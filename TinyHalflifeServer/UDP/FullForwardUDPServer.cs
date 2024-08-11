using NetCoreServer;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TinyHalflifeServer.A2S;

namespace TinyHalflifeServer.UDP;
internal class FullForwardUDPServer(int port, ServerInfo info) : UdpServer(IPAddress.Any, port)
{
    private enum A2S_Type
    {
        Info,
        Player,
        Rules,
        GetChallenge,
        Ping
    }
    private readonly ServerInfo _serverInfo = info;

    private void A2SRespond(EndPoint endpoint, A2S_Type type)
    {
        Logger.Debug("Responding ip:{0}, type:{1}", endpoint.ToString(), type);
        byte[] respond = type switch
        {
            A2S_Type.Info => _serverInfo.RenderA2SInfoRespond(),
            A2S_Type.Player => _serverInfo.RenderA2SPlayerRespond(),
            A2S_Type.Rules => _serverInfo.RenderA2SRulesRespond(),
            A2S_Type.GetChallenge => _serverInfo.RenderA2SServerQueryGetChallengeRespond(),
            A2S_Type.Ping => _serverInfo.RenderA2APingRespond(),
            _ => throw new Exception("Error A2S respond type!"),
        };
        Logger.Debug("Data responding: {0}", BitConverter.ToString(respond));
        SendAsync(endpoint, respond, 0, respond.Length);
        Logger.Debug("Responded!");
    }
    private void S2ARequest(EndPoint endpoint, byte[] buffer)
    {
        using MemoryStream ms = new(buffer);
        using BinaryReader br = new(ms);
        int prefix = br.ReadInt32();
        byte header = br.ReadByte();
        Logger.Debug("S2A prefix:{0} header:{1}", prefix, header);
        Logger.Debug("Data before reading string: {0}", BitConverter.ToString(buffer));
        switch (header)
        {
            //T A2S_INFO
            case 0x54:
                {
                    Logger.Debug("S2A_INFO");
                    string paylod = Encoding.UTF8.GetString(br.ReadBytes(20));
                    Logger.Debug("S2A_INFO paylod: {0}", paylod);
                    A2SRespond(endpoint, A2S_Type.Info);
                    break;
                }
            //U A2S_PLAYER
            case 0x55:
                {
                    Logger.Debug("S2A_PLAYER");
                    A2SRespond(endpoint, A2S_Type.Player);
                    break;
                }
            //V A2S_RULES
            case 0x56:
                {
                    Logger.Debug("S2A_RULES");
                    A2SRespond(endpoint, A2S_Type.Rules);
                    break;
                }
            //W A2S_SERVERQUERY_GETCHALLENGE
            case 0x57:
                {
                    Logger.Debug("S2A_SERVERQUERY_GETCHALLENGE");
                    A2SRespond(endpoint, A2S_Type.GetChallenge);
                    break;
                }
            //i A2A_PING
            case 0x69:
                {
                    Logger.Debug("A2A_PING");
                    A2SRespond(endpoint, A2S_Type.Ping);
                    break;
                }
            default:
                {
                    Logger.Warn("Unknow S2A type, header: 0x{0}", BitConverter.ToString([header]));
                    ReceiveAsync();
                    break;
                }

        }
    }
    protected override void OnStarted()
    {
        // Start receive datagrams
        ReceiveAsync();
    }
    protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
    {
        byte[] data = new byte[size];
        Array.Copy(buffer, offset, data, 0, size);
        if (data.Length > 4)
        {
            void FullForward()
            {
                //Full forward
                FullForwardUDPClient udpClient = new(Program.Config!.RDIP!.IP!, Program.Config!.RDIP!.Port, this, endpoint);
                udpClient.Connect();
                udpClient.SendAsync(data);
                ReceiveAsync();
            }
            //S2A request
            if (data[0] == 0xFF && data[1] == 0xFF && data[2] == 0xFF && data[3] == 0xFF)
            {
                byte magic = data[4];
                if ((magic >= 0x54 && magic <= 0x57) || magic == 0x69)
                    S2ARequest(endpoint, data);
                else
                    FullForward();
            }
            else
                FullForward();
        }
        else
            ReceiveAsync();

    }
    protected override void OnSent(EndPoint endpoint, long sent)
    {
        // Continue receive datagrams
        ReceiveAsync();
    }
    protected override void OnError(SocketError error)
    {
        Logger.Error($"Echo UDP server caught an error with code {error}");
    }
}
