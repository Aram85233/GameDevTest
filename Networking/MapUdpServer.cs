using LiteNetLib;
using MemoryPack;
using System.Net;
using TileMap.Contracts.Dto;
using TileMap.Contracts.Events;
using TileMap.Contracts.Requests;
using TileMap.Contracts.Responses;

namespace TileMap.Networking
{
    public class MapUdpServer : INetEventListener, IDisposable
    {
        private readonly EventBasedNetListener listener;
        private readonly NetManager server;
        private readonly object peersLock = new();
        private readonly List<NetPeer> peers = new();

        // Слои (интерфейсы/классы)
        private readonly IMapQueryProvider mapQueryProvider; // интерфейс для доступа к слоям (см. ниже)
        private readonly ReaderWriterLockSlim rwLock = new();

        public MapUdpServer(IMapQueryProvider provider, int port = 9050)
        {
            mapQueryProvider = provider ?? throw new ArgumentNullException(nameof(provider));

            listener = new EventBasedNetListener();
            server = new NetManager(this) { AutoRecycle = true };
            server.Start(port);

            // LiteNetLib events handled via INetEventListener methods below
        }

        public void PollEvents() => server.PollEvents();

        public void Stop()
        {
            server.Stop();
        }

        public void Dispose()
        {
            Stop();
            rwLock?.Dispose();
        }

        // INetEventListener impl
        public void OnPeerConnected(NetPeer peer)
        {
            lock (peersLock) { peers.Add(peer); }
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            lock (peersLock) { peers.Remove(peer); }
        }
        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { /* логгировать при необходимости */ }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept(); // простая логика: все принимаем
        }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                // Первый байт — тип сообщения
                var len = reader.AvailableBytes;
                if (len <= 0) return;
                var buffer = reader.GetRemainingBytes(); // копирование
                var type = (NetMessageType)buffer[0];
                var payload = buffer.AsSpan(1).ToArray();

                switch (type)
                {
                    case NetMessageType.GetObjectsInAreaRequest:
                        HandleGetObjectsRequest(peer, payload);
                        break;
                    case NetMessageType.GetRegionsInAreaRequest:
                        HandleGetRegionsRequest(peer, payload);
                        break;
                    default:
                        // игнорируем/логгируем
                        break;
                }
            }
            finally
            {
                reader.Recycle();
            }
        }

        private void HandleGetObjectsRequest(NetPeer peer, byte[] payload)
        {
            var req = MemoryPackSerializer.Deserialize<GetObjectsInAreaRequest>(payload);
            // Читаем слои под read lock
            rwLock.EnterReadLock();
            try
            {
                var objects = mapQueryProvider.GetObjectsInArea(req.X1, req.Y1, req.X2, req.Y2);
                var dto = new GetObjectsInAreaResponse
                {
                    Objects = objects.Select(o => new GameObjectDto
                    {
                        Id = o.Id,
                        X = o.X,
                        Y = o.Y,
                        Width = o.Width,
                        Height = o.Height
                    }).ToList()
                };

                var body = MemoryPackSerializer.Serialize(dto);
                var send = new byte[1 + body.Length];
                send[0] = (byte)NetMessageType.GetObjectsInAreaResponse;
                Array.Copy(body, 0, send, 1, body.Length);
                peer.Send(send, DeliveryMethod.ReliableOrdered);
            }
            finally { rwLock.ExitReadLock(); }
        }

        private void HandleGetRegionsRequest(NetPeer peer, byte[] payload)
        {
            var req = MemoryPackSerializer.Deserialize<GetRegionsInAreaRequest>(payload);
            rwLock.EnterReadLock();
            try
            {
                var regions = mapQueryProvider.GetRegionsInArea(req.X1, req.Y1, req.X2, req.Y2);
                var dto = new GetRegionsInAreaResponse { Regions = regions.Select(r => new RegionDto { Id = r.Id, Name = r.Name }).ToList() };

                var body = MemoryPackSerializer.Serialize(dto);
                var send = new byte[1 + body.Length];
                send[0] = (byte)NetMessageType.GetRegionsInAreaResponse;
                Array.Copy(body, 0, send, 1, body.Length);
                peer.Send(send, DeliveryMethod.ReliableOrdered);
            }
            finally { rwLock.ExitReadLock(); }
        }

        // Внешний вызов — при обновлениях объектов (подписка)
        public void BroadcastObjectEvent(NetMessageType type, ObjectEventMessage ev)
        {
            if (type != NetMessageType.ObjectAdded && type != NetMessageType.ObjectUpdated && type != NetMessageType.ObjectDeleted)
                throw new ArgumentException("Invalid event type");

            var body = MemoryPackSerializer.Serialize(ev);
            var toSend = new byte[1 + body.Length];
            toSend[0] = (byte)type;
            Array.Copy(body, 0, toSend, 1, body.Length);

            lock (peersLock)
            {
                foreach (var p in peers.ToList())
                {
                    if (p.ConnectionState == ConnectionState.Connected)
                        p.Send(toSend, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        // utils для цикла сервера
        public void RunPollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                PollEvents();
                Thread.Sleep(5);
            }
        }

        // Эти методы можно вызвать, когда нужно читать/писать слоями в безопасном режиме
        public IDisposable EnterReadScope() { rwLock.EnterReadLock(); return new DisposableAction(() => rwLock.ExitReadLock()); }
        public IDisposable EnterWriteScope() { rwLock.EnterWriteLock(); return new DisposableAction(() => rwLock.ExitWriteLock()); }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.Broadcast)
            {
                var data = reader.GetRemainingBytes();
                Console.WriteLine($"📢 Broadcast от {remoteEndPoint}, длина {data.Length}");
                // Можно ответить клиенту
                var reply = System.Text.Encoding.UTF8.GetBytes("Server received broadcast");
                server.SendUnconnectedMessage(reply, remoteEndPoint);
            }
            reader.Recycle();
        }

        private class DisposableAction : IDisposable
        {
            private readonly Action action;
            public DisposableAction(Action action) { this.action = action; }
            public void Dispose() => action();
        }
    }
}
