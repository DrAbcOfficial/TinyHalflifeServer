using TinyHalflifeServer.Steam;

namespace TinyHalflifeServer
{
    public class Server
    {
        private SteamServer m_SteamServerInfo = new();
        public void Initialize()
        {
            m_SteamServerInfo.InitServer((ushort)Program.Config.Port, Program.Config.AppId, Program.Config.Version, Program.Config.ServerInfo.VAC);
            m_SteamServerInfo.SetAccountToken(Program.Config.GSLT);
            m_SteamServerInfo.LogOn();

            if (m_SteamServerInfo.BLoggedOn())
            {
                /*
                    asio::co_spawn(g_IoContext, PrepareListenServer(), asio::detached);
			        asio::co_spawn(g_IoContext, RunFrame(), asio::detached);
			        asio::co_spawn(g_IoContext, PrintAuthedCount(), asio::detached);
                 */
            }
        }
    }
}
