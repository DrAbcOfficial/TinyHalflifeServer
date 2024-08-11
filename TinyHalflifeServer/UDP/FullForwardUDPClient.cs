using System.Net;
using System.Net.Sockets;
using UdpClient = NetCoreServer.UdpClient;

namespace TinyHalflifeServer.UDP
{
    internal class FullForwardUDPClient(string address, int port, FullForwardUDPServer server, EndPoint client) : UdpClient(address, port)
    {
        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            server.SendAsync(client, buffer);
            Logger.Debug($"[Full Forward] Recive full forward package from {address}:{port}");
        }
        protected override void OnSent(EndPoint endpoint, long sent)
        {
            ReceiveAsync();
            Logger.Debug($"[Full Forward] Sent full forward package to {address}:{port}");
        }
        protected override void OnError(SocketError error)
        {
            Logger.Error($"Error: {error}");
        }
    }
}
