using NetCoreServer;
using Steamworks;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TinyHalflifeServer.A2S;

namespace TinyHalflifeServer.UDP;

internal class GameUDPServer(int port, ServerInfo info) : UdpServer(IPAddress.Any, port)
{
    private enum A2S_Type
    {
        Info,
        Player,
        Rules,
        Ping,
        GetChallenge,
        Challenge,
        Connect
    }
    private readonly ServerInfo _serverInfo = info;

    private byte[] RenderGetChallengeRespond()
    {
        using MemoryStream ms = new();
        using BinaryWriter bw = new(ms);
        if (!Program.Config!.RDIP!.Enable || _serverInfo.GetServerPassworded())
        {
            bw.Write(-1);
            //message
            bw.Write((byte)0x6C);
            bw.Write(Encoding.UTF8.GetBytes($"{Program.Config!.Text!.Kicked}\0"));
        }
        else
        {
            //S2C_CHALLENGE
            //none sense prefix
            bw.Write(0xFFFFFFFF);
            //generate fake challenge
            Random random = new();
            static uint NextUInt(Random random, uint minValue, uint maxValue)
            {
                ulong range = (ulong)maxValue - minValue + 1;
                ulong randomValue = (ulong)(random.NextDouble() * range) + minValue;
                return (uint)randomValue;
            };
            uint challenge = (NextUInt(random, 0, 0x7fff) << 16) | (NextUInt(random, 0, 0xffff));
            bw.Write(Encoding.UTF8.GetBytes(string.Format("A00000000 {0} 3 {1} {2}\n\0", challenge, SteamGameServer.GetSteamID().m_SteamID.ToString(), SteamGameServer.BSecure() ? 1 : 0)));
        }
        return [.. ms.ToArray()];
    }
    private static byte[] RenderConnectRespond()
    {
        /*
         * https://oxikkk.github.io/articles/goldsrc_connection_process.html
         *  B 165 "158.XXX.50.XX:27005" 0 8156
         *  ^ ^   ^                     ^ ^
         *  | |   |                     | server build number
         *  | |   |                     always zero (unknown)
         *  | |   "true" ip address
         *  | user id
         *  message id
        */
        using MemoryStream ms = new();
        using BinaryWriter bw = new(ms);
        //none sense prefix
        bw.Write(0xFFFFFFFF);
        bw.Write(Encoding.UTF8.GetBytes($"B {233}\"{Program.Config!.RDIP!.IP}:{Program.Config.RDIP.Port}\" 0 {1234}"));
        return [.. ms.ToArray()];
    }
    private void SendRDIPMessage(EndPoint endpoint)
    {
        using MemoryStream ms = new();
        using BinaryWriter bw = new(ms);
        //msg channel 1, no fragment
        //little eddin
        bw.Write(0x80000001);
        //no ongoing outer
        bw.Write(0x00000000);
        //svc_stufftext
        bw.Write((byte)9);
        bw.Write(Encoding.UTF8.GetBytes(string.Format("connect {0}:{1}\n\0", Program.Config!.RDIP!.IP, Program.Config.RDIP.Port)));
        byte[] respond = ms.ToArray();
        SendAsync(endpoint, respond, 0, respond.Length);
        Logger.Debug("[" + endpoint.ToString() + "]: RDIP responded data " + BitConverter.ToString(respond));
    }
    private void A2SRespond(EndPoint endpoint, A2S_Type type)
    {
        Logger.Debug("Responding ip:{0}, type:{1}", endpoint.ToString(), type);
        byte[] respond = type switch
        {
            A2S_Type.Info => _serverInfo.RenderA2SInfoRespond(),
            A2S_Type.Player => _serverInfo.RenderA2SPlayerRespond(),
            A2S_Type.Rules => _serverInfo.RenderA2SRulesRespond(),
            A2S_Type.Ping => _serverInfo.RenderA2APingRespond(),
            A2S_Type.GetChallenge => _serverInfo.RenderA2SServerQueryGetChallengeRespond(),
            A2S_Type.Challenge => RenderGetChallengeRespond(),
            A2S_Type.Connect => RenderConnectRespond(),
            _ => throw new Exception("Error A2S respond type!"),
        };
        Logger.Debug("Data responding: {0}", BitConverter.ToString(respond));
        SendAsync(endpoint, respond, 0, respond.Length);
        Logger.Debug("Responded!");
        if (type == A2S_Type.Connect)
        {
            SendRDIPMessage(endpoint);
        }
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
                    //int challenge = br.ReadInt32();
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
            //i A2A_PING
            case 0x69:
                {
                    Logger.Debug("A2A_PING");
                    A2SRespond(endpoint, A2S_Type.Ping);
                    break;
                }
            //W A2S_SERVERQUERY_GETCHALLENGE
            case 0x57:
                {
                    Logger.Debug("S2A_SERVERQUERY_GETCHALLENGE");
                    A2SRespond(endpoint, A2S_Type.GetChallenge);
                    break;
                }
            //g getchallenge steam
            case 0x67:
                {
                    A2SRespond(endpoint, A2S_Type.Challenge);
                    break;
                }
            //c connect
            case 0x63:
                {
                    Logger.Debug("connect");
                    if (Program.Config!.RDIP!.Enable)
                        A2SRespond(endpoint, A2S_Type.Connect);
                    else
                        ReceiveAsync();
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
        //S2A request
        if (data.Length > 4 && data[0] == 0xFF && data[1] == 0xFF && data[2] == 0xFF && data[3] == 0xFF)
        {
            S2ARequest(endpoint, data);
        }
        else
        {
            Logger.Debug("[" + endpoint.ToString() + "]: Recive Unknown data " + BitConverter.ToString(data));
            ReceiveAsync();
        }
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
