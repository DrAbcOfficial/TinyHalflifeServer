using Steamworks;
using TinyHalflifeServer.A2S;
using TinyHalflifeServer.Steam;
using TinyHalflifeServer.UDP;

namespace TinyHalflifeServer
{
    public class Server
    {
        private SteamServer m_SteamServerInfo = new();
        private ServerInfo m_ServerInfo = new();
        private GameUDPServer m_UdpServer;
        private Task m_TaskRunFrame;
        public void Initialize()
        {
            m_SteamServerInfo.InitServer((ushort)Program.Config.Port, Program.Config.AppId, Program.Config.Version, Program.Config.ServerInfo.VAC);
            m_SteamServerInfo.SetAccountToken(Program.Config.GSLT);
            m_SteamServerInfo.LogOn();

            //A2S Info
            m_ServerInfo.SetServerAddress($"{SteamGameServer.GetPublicIP().ToIPAddress()}:{Program.Config.Port}");
            m_ServerInfo.SetServerName(Program.Config.ServerInfo.Name);
            m_ServerInfo.SetServerMap(Program.Config.ServerInfo.Map);
            m_ServerInfo.SetServerGameFolder(Program.Config.ServerInfo.GameFolder);
            m_ServerInfo.SetServerDescription(Program.Config.ServerInfo.Description);
            m_ServerInfo.SetServerMaxNumClients((uint)Program.Config.ServerInfo.MaxClients);
            m_ServerInfo.SetServerProtocol((uint)Program.Config.ServerInfo.Protocol);
            m_ServerInfo.SetServerType((ServerInfo.ServerType)Program.Config.ServerInfo.Type);
            m_ServerInfo.SetServerOS((ServerInfo.ServerOS)Program.Config.ServerInfo.OS);
            m_ServerInfo.SetServerPassworded(Program.Config.ServerInfo.Passworded);
            m_ServerInfo.SetServerModed(Program.Config.ServerInfo.Moded);
            m_ServerInfo.SetServerVACStatus(Program.Config.ServerInfo.VAC);
            m_ServerInfo.SetServerNumFakeClients((uint)Program.Config.ServerInfo.FakeClients);

            ModInfo mod = m_ServerInfo.GetServerModInfo();
            mod.m_ModUrl = Program.Config.ServerInfo.ModInfo.Url;
            mod.m_ModDownloadUrl = Program.Config.ServerInfo.ModInfo.DownloadUrl;
            mod.m_ModVersion = Program.Config.ServerInfo.ModInfo.Version;
            mod.m_ModSize = Program.Config.ServerInfo.ModInfo.Size;
            mod.m_ModType = (ModInfo.ModType)Program.Config.ServerInfo.ModInfo.Type;
            mod.m_ModDLL = (ModInfo.ModDLL)Program.Config.ServerInfo.ModInfo.DLL;
            List<PlayerInfo> list = m_ServerInfo.GetPlayerInfos();
            foreach (var pi in Program.Config.ServerInfo.Players)
            {
                PlayerInfo info = new()
                {
                    m_Name = pi.Name,
                    m_Duration = pi.Duration,
                    m_Score = pi.Score
                };
                list.Add(info);
            }
            List<RulesInfo> rules = m_ServerInfo.GetRulesInfos();
            foreach(var ri in Program.Config.ServerInfo.Rules)
            {
                RulesInfo info = new()
                {
                    m_Name = ri.Name,
                    m_Value = ri.Value
                };
                rules.Add(info);
            }

            if (m_SteamServerInfo.BLoggedOn())
            {
                /*
                    asio::co_spawn(g_IoContext, PrepareListenServer(), asio::detached);
			        asio::co_spawn(g_IoContext, RunFrame(), asio::detached);
			        asio::co_spawn(g_IoContext, PrintAuthedCount(), asio::detached);
                 */
                m_UdpServer = new(Program.Config.Port, m_ServerInfo);
                m_TaskRunFrame = new(() =>
                {
                    while (true)
                    {
                        GameServer.RunCallbacks();
                        SendUpdatedServerDetails();
                        SteamGameServer.SetAdvertiseServerActive(true);
                        Thread.Sleep(500);
                    }
                });
            }
        }

        public void StartRun()
        {
            m_TaskRunFrame.Start();
            m_UdpServer.Start();
        }

        public void Frame()
        {
            Thread.Sleep(1000);
        }

        private void SendUpdatedServerDetails()
        {
            SteamGameServer.SetProduct(Program.Config.AppId);
            SteamGameServer.SetModDir(Program.Config.ServerInfo.GameFolder);
            SteamGameServer.SetServerName(Program.Config.ServerInfo.Name);
            SteamGameServer.SetGameDescription(Program.Config.ServerInfo.Description);
            SteamGameServer.SetMapName(Program.Config.ServerInfo.Map);
            SteamGameServer.SetPasswordProtected(Program.Config.ServerInfo.Passworded);
            SteamGameServer.SetMaxPlayerCount(Program.Config.ServerInfo.MaxClients);
            SteamGameServer.SetBotPlayerCount(Program.Config.ServerInfo.FakeClients);
            SteamGameServer.SetSpectatorPort(0);
        }
    }
}
