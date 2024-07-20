using NetCoreServer;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TinyHalflifeServer.A2S;

namespace TinyHalflifeServer.UDP
{
    internal class GameUDPServer(int port, ServerInfo info) : UdpServer(IPAddress.Any, port)
    {
        private enum A2S_Type
        {
            Info,
            Player,
            Rules,
            Ping,
            GetChallenge,
            Challenge
        }
        private readonly ServerInfo _serverInfo = info;

        private byte[] BuildRDIPRespond()
        {
            using MemoryStream ms = new();
            using BinaryWriter bw = new(ms);
            //Non sense prefix
            bw.Write(0xFFFFFFFF);
            bw.Write(Encoding.UTF8.GetBytes($"L{Program.Config.RDIP.Destination}"));
            return [.. ms.ToArray()];
        }

        private byte[] BuildRetryRespond() 
        {
            using MemoryStream ms = new();
            using BinaryWriter bw = new(ms);
            //Non sense prefix
            bw.Write(0xFFFFFFFF);
            //svc_stufftext
            bw.Write((byte)0x09);
            //cmd
            bw.Write(Encoding.UTF8.GetBytes($"retry\0\x7\x3B"));
            return [.. ms.ToArray()];
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
                A2S_Type.Challenge => BuildRDIPRespond(),
                _ => throw new Exception("Error A2S respond type!"),
            };
            Logger.Debug("Data responding: {0}", BitConverter.ToString(respond));
            SendAsync(endpoint, respond, 0, respond.Length);
            if(type == A2S_Type.Challenge)
            {
                respond = BuildRetryRespond();
                SendAsync(endpoint, respond, 0, respond.Length);
            }
            Logger.Debug("Responded!");
        }
        private void S2ARequest(EndPoint endpoint, byte[] buffer)
        {
            using MemoryStream ms = new(buffer);
            using BinaryReader br = new(ms);
            int prefix = br.ReadInt32();
            byte header = br.ReadByte();
            Logger.Debug("S2A prefix:{0} header:{1}", prefix, header);
            switch (header)
            {
                //T A2S_INFO
                case 0x54:
                    {
                        Logger.Debug("S2A_INFO");
                        Logger.Debug("Data before reading string: {0}", BitConverter.ToString(buffer));
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
                        Logger.Debug("Data before reading string: {0}", BitConverter.ToString(buffer));
                        //int challenge = br.ReadInt32();
                        A2SRespond(endpoint, A2S_Type.Player);
                        break;
                    }
                //V A2S_RULES
                case 0x56:
                    {
                        Logger.Debug("S2A_RULES");
                        Logger.Debug("Data before reading string: {0}", BitConverter.ToString(buffer));
                        //int challenge = br.ReadInt32();
                        A2SRespond(endpoint, A2S_Type.Rules);
                        break;
                    }
                //i A2A_PING
                case 0x69:
                    {
                        Logger.Debug("A2A_PING");
                        Logger.Debug("Data before reading string: {0}", BitConverter.ToString(buffer));
                        A2SRespond(endpoint, A2S_Type.Ping);
                        break;
                    }
                //W A2S_SERVERQUERY_GETCHALLENGE
                case 0x57:
                    {
                        Logger.Debug("S2A_SERVERQUERY_GETCHALLENGE");
                        Logger.Debug("Data before reading string: {0}", BitConverter.ToString(buffer));
                        A2SRespond(endpoint, A2S_Type.GetChallenge);
                        break;
                    }
                //g getchallenge steam
                case 0x67:
                    {
                        Logger.Debug("getchallenge");
                        Logger.Debug("Data before reading string: {0}", BitConverter.ToString(buffer));
                        if (Program.Config.RDIP.Enable)
                            A2SRespond(endpoint, A2S_Type.Challenge);
                        break;
                    }
                default:
                    {
                        Logger.Warn("Unknow S2A type, header: {0}, data: {1}", header, BitConverter.ToString(buffer));
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
            if (data[0] == 0xFF && data[1] == 0xFF && data[2] == 0xFF && data[3] == 0xFF)
            {
                S2ARequest(endpoint, data);
            }
            else
            {
                Logger.Log("[" + endpoint.ToString() + "]: Recive" + Encoding.UTF8.GetString(data));
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
}
