using LiteNetLib;
using System.Net;

namespace TinyHalflifeServer.UDP
{
    internal class GameServer : INetEventListener
    {
        private NetManager _server;

        public GameServer(int port)
        {
            _server = new NetManager(this);
            _server.Start(port);
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("SomeConnectionKey");
        }
        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Console.WriteLine($"Network error: {socketError}");
        }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte data, DeliveryMethod deliveryMethod)
        {
            string message = reader.GetString();
            Console.WriteLine($"Received: {message}");
            reader.Recycle();
        }
        public void OnNetworkReceiveUnconnected(IPEndPoint endPoint, NetPacketReader reader, UnconnectedMessageType unconnectedMessageType)
        {

        }
        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"Client connected: {peer.Id}");
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"Client disconnected: {peer.Id}");
        }
    }
}
