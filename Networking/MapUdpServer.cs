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
        private readonly NetManager server;
        private readonly object peersLock = new();
        private readonly List<NetPeer> peers = new();

        private readonly IMapQueryProvider mapQueryProvider;
        private readonly ReaderWriterLockSlim rwLock = new();

        public MapUdpServer(IMapQueryProvider provider, int port = 9050)
        {
            mapQueryProvider = provider ?? throw new ArgumentNullException(nameof(provider));

            server = new NetManager(this) { AutoRecycle = true };
            server.Start(port);
            Console.WriteLine($"UDP сервер слушает порт {port}");
        }

        public void RunPollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                PollEvents();
                Thread.Sleep(5);
            }
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

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"Новый клиент подключился: {peer.Address}");
            lock (peersLock) { peers.Add(peer); }
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            lock (peersLock) { peers.Remove(peer); }
        }
        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) 
        {
            // логгировать при необходимости
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            try
            {
                var len = reader.AvailableBytes;
                if (len <= 0) return;

                var buffer = reader.GetRemainingBytes();
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
                        Console.WriteLine($"Неизвестный тип сообщения: {type}");
                        break;
                }
            }
            finally
            {
                reader.Recycle();
            }
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

        private void HandleGetObjectsRequest(NetPeer peer, byte[] payload)
        {
            var req = MemoryPackSerializer.Deserialize<GetObjectsInAreaRequest>(payload);
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
    }
}
