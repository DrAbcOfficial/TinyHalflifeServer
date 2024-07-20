using SimpleUdp;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TinyHalflifeServer.A2S;

namespace TinyHalflifeServer.UDP
{
    internal class GameUDPServer
    {
        private enum A2S_Type
        {
            Info,
            Player,
            Rules,
            Ping,
            GetChallenge
        }
        private UdpEndpoint _server;
        private int _port;
        private ServerInfo _serverInfo;
        public GameUDPServer(int port, ServerInfo info)
        {
            _port = port;
            _serverInfo = info;
        }
        public void Start()
        {
            _server = new("0.0.0.0", _port);
            _server.EndpointDetected += EndpointDetected;
            _server.DatagramReceived += DatagramReceived;
        }

        private void A2SRespond(Datagram dg, A2S_Type type)
        {
            Logger.Debug("Responding ip:{0}:{1}, type:{2}", dg.Ip, dg.Port, type);
            UdpClient client = new(new IPEndPoint(IPAddress.Parse(dg.Ip), dg.Port));
            byte[] buffer = type switch
            {
                A2S_Type.Info => _serverInfo.RenderA2SInfoRespond(),
                A2S_Type.Player => _serverInfo.RenderA2SPlayerRespond(),
                A2S_Type.Rules => _serverInfo.RenderA2SRulesRespond(),
                A2S_Type.Ping => _serverInfo.RenderA2APingRespond(),
                A2S_Type.GetChallenge => _serverInfo.RenderA2SServerQueryGetChallengeRespond(),
            };
            client.Send(buffer, buffer.Length);
            Logger.Debug("Responded!");
        }

        private void S2A(Datagram dg)
        {
            using MemoryStream ms = new(dg.Data);
            using BinaryReader br = new(ms);
            int prefix = br.ReadInt32();
            byte header = br.ReadByte();
            Logger.Log("S2A prefix:{0} header:{1}", prefix, header);
            switch (header)
            {
                //T A2S_INFO
                case 0x54:
                    {
                        Logger.Debug("S2A_INFO");
                        string paylod = br.ReadString();
                        int challenge = br.ReadInt32();
                        Logger.Debug("S2A_INFO paylod:{0} challenge:{1}", paylod, challenge);
                        A2SRespond(dg, A2S_Type.Info);
                        break;
                    }
                //U A2S_PLAYER
                case 0x55:
                    {
                        Logger.Debug("S2A_PLAYER");
                        int challenge = br.ReadInt32();
                        A2SRespond(dg, A2S_Type.Player);
                        break;
                    }
                //V A2S_RULES
                case 0x56:
                    {
                        Logger.Debug("S2A_RULES");
                        int challenge = br.ReadInt32();
                        A2SRespond(dg, A2S_Type.Rules);
                        break;
                    }
                //i A2A_PING
                case 0x69:
                    {
                        Logger.Debug("A2A_PING");
                        A2SRespond(dg, A2S_Type.Ping);
                        break;
                    }
                //W A2S_SERVERQUERY_GETCHALLENGE
                case 0x57:
                    {
                        Logger.Debug("S2A_SERVERQUERY_GETCHALLENGE");
                        A2SRespond(dg, A2S_Type.GetChallenge);
                        break;
                    }
                default: Logger.Warn("Unknow S2A type, header: {0}, data: {1}", header, BitConverter.ToString(dg.Data)); break;
            }
        }

        private void EndpointDetected(object sender, EndpointMetadata md)
        {
            Logger.Log("Endpoint detected: " + md.Ip + ":" + md.Port);
        }

        private void DatagramReceived(object sender, Datagram dg)
        {
            //S2A request
            if (dg.Data[0] == 0xFF && dg.Data[1] == 0xFF && dg.Data[2] == 0xFF && dg.Data[3] == 0xFF)
            {
                S2A(dg);
            }
            else
                Logger.Log("[" + dg.Ip + ":" + dg.Port + "]: " + Encoding.UTF8.GetString(dg.Data));
        }
    }
}
