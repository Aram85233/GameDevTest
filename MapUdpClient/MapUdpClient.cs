using LiteNetLib;
using MemoryPack;
using TileMap.Contracts.Events;
using TileMap.Contracts.Requests;
using TileMap.Contracts.Responses;

namespace TileMap.MapUdpClient
{
    public class MapUdpClient
    {
        private readonly NetManager client;
        private readonly EventBasedNetListener listener;
        private NetPeer? serverPeer;

        public MapUdpClient()
        {
            listener = new EventBasedNetListener();
            client = new NetManager(listener);
            client.Start();

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("✅ Подключились к серверу");
                serverPeer = peer;

                // Запросим объекты
                var req = new GetObjectsInAreaRequest { X1 = 0, Y1 = 0, X2 = 50, Y2 = 50 };
                SendRequest(NetMessageType.GetObjectsInAreaRequest, req);

                // Запросим регионы
                var req2 = new GetRegionsInAreaRequest { X1 = 0, Y1 = 0, X2 = 50, Y2 = 50 };
                SendRequest(NetMessageType.GetRegionsInAreaRequest, req2);
            };

            listener.NetworkReceiveEvent += (peer, reader, channelNumber, deliveryMethod) =>
            {
                var data = reader.GetRemainingBytes();
                try
                {
                    if (data == null || data.Length == 0) return;
                    var type = (NetMessageType)data[0];
                    var payload = data.Skip(1).ToArray();

                    switch (type)
                    {
                        case NetMessageType.GetObjectsInAreaResponse:
                            var respObj = MemoryPackSerializer.Deserialize<GetObjectsInAreaResponse>(payload);
                            Console.WriteLine($"📦 Объекты: {string.Join(", ", respObj.Objects.Select(o => o.Id))}");
                            break;
                        case NetMessageType.GetRegionsInAreaResponse:
                            var respReg = MemoryPackSerializer.Deserialize<GetRegionsInAreaResponse>(payload);
                            Console.WriteLine($"📦 Регионы: {string.Join(", ", respReg.Regions.Select(o => o.Id))}");
                            break;
                        case NetMessageType.ObjectAdded:
                            var added = MemoryPackSerializer.Deserialize<ObjectEventMessage>(payload);
                            Console.WriteLine($"📢 Object Added: {added.Id} @({added.X},{added.Y})");
                            break;
                        case NetMessageType.ObjectUpdated:
                            var updated = MemoryPackSerializer.Deserialize<ObjectEventMessage>(payload);
                            Console.WriteLine($"📢 Object Updated: {updated.Id} @({updated.X},{updated.Y})");
                            break;
                        case NetMessageType.ObjectDeleted:
                            var deleted = MemoryPackSerializer.Deserialize<ObjectEventMessage>(payload);
                            Console.WriteLine($"📢 Object Deleted: {deleted.Id}");
                            break;
                    }
                }
                finally
                {
                    reader.Recycle(); // возвращает объект в пул
                }
            };

            listener.PeerDisconnectedEvent += (peer, info) => Console.WriteLine("❌ Отключились");
        }

        public void Connect(string host = "127.0.0.1", int port = 9050)
        {
            client.Connect(host, port, "");
            Console.WriteLine($"🔌 Подключаемся к {host}:{port}");
        }

        public void PollEvents()
        {
            client.PollEvents();
        }

        private void SendRequest<T>(NetMessageType type, T req)
        {
            if (serverPeer == null) return;
            var body = MemoryPackSerializer.Serialize(req);
            var send = new byte[1 + body.Length];
            send[0] = (byte)type;
            Array.Copy(body, 0, send, 1, body.Length);
            serverPeer.Send(send, DeliveryMethod.ReliableOrdered);
        }
    }
}
