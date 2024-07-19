using Steamworks;
using System.Net;

namespace TinyHalflifeServer.Steam
{
    public struct SteamAuthInfo(UInt64 steamId, EAuthSessionResponse code)
    {
        public UInt64 m_SteamID = steamId;
        public EAuthSessionResponse m_AuthCode = code;
    }
    public class AuthHolder
    {
        private List<SteamAuthInfo> m_AuthList = [];
        public void SetAuth(UInt64 steamid, EAuthSessionResponse code)
        {
            bool found = false;
            for (int i = 0; i < m_AuthList.Count; i++)
            {
                SteamAuthInfo auth = m_AuthList[i];
                if (auth.m_SteamID == steamid)
                {
                    found = true;
                    auth.m_AuthCode = code;
                }
            }
            if (found)
            {
                return;
            }
            m_AuthList.Add(new SteamAuthInfo(steamid, code));
        }

        static AuthHolder g_AuthHolder = new AuthHolder();
        public static AuthHolder GetAuthHolder()
        {
            return g_AuthHolder;
        }
    }

    public class SteamServer
    {
        #region Callback
        protected Callback<SteamServersConnected_t> m_CallbackLogonSuccess;
        protected Callback<SteamServerConnectFailure_t> m_CallbackLogonFailure;
        protected Callback<SteamServersDisconnected_t> m_CallbackLoggedOff;
        // game server callbacks
        protected Callback<GSPolicyResponse_t> m_CallbackGSPolicyResponse;
        protected Callback<ValidateAuthTicketResponse_t> m_CallbackValidateAuthTicketResponse;
        #endregion
        #region Varible
        protected EServerMode m_eServerMode;

        protected bool m_bMasterServerUpdaterSharingGameSocket;
        protected bool m_bLogOnFinished;
        protected bool m_bLoggedOn;
        protected bool m_bLogOnResult;      // if true, show logon result
        protected bool m_bHasActivePlayers;  // player stats updates are only sent if active players are available
        protected CSteamID m_SteamIDGS;
        protected bool m_bInitialized;


        // The port that we are listening for queries on.
        protected UInt32 m_unIP;
        protected UInt16 m_usPort;
        protected UInt16 m_QueryPort;
        protected string m_sAccountToken;
        #endregion
        #region Const
        const ushort STEAMGAMESERVER_QUERY_PORT_SHARED = 0xffff;
        #endregion

        public SteamServer()
        {
            m_CallbackLogonSuccess = Callback<SteamServersConnected_t>.CreateGameServer(OnLogonSuccess);
            m_CallbackLogonFailure = Callback<SteamServerConnectFailure_t>.CreateGameServer(OnLogonFailure);
            m_CallbackLoggedOff = Callback<SteamServersDisconnected_t>.CreateGameServer(OnLoggedOff);

            m_CallbackGSPolicyResponse = Callback<GSPolicyResponse_t>.CreateGameServer(OnGsPolicyResponse);
            m_CallbackValidateAuthTicketResponse = Callback<ValidateAuthTicketResponse_t>.CreateGameServer(OnValidateAuthTicketResponse);
        }
        public bool BSecure()
        {
            return SteamGameServer.BSecure();
        }
        public bool BIsActive()
        {
            return m_eServerMode >= EServerMode.eServerModeNoAuthentication;
        }
        public bool BLanOnly()
        {
            return m_eServerMode == EServerMode.eServerModeNoAuthentication;
        }
        public bool BWantsSecure()
        {
            return m_eServerMode == EServerMode.eServerModeAuthenticationAndSecure;
        }
        public bool BLoggedOn()
        {
            return SteamGameServer.BLoggedOn();
        }
        public CSteamID GetGSSteamID()
        {
            return m_SteamIDGS;
        }
        public bool BHasLogonResult()
        {
            return m_bLogOnResult;
        }
        public void LogOnAnonymous()
        {
            SteamGameServer.LogOnAnonymous();
        }
        public UInt16 GetQueryPort()
        {
            return m_QueryPort;
        }
        public long GetPublicIP()
        {
            return SteamGameServer.GetPublicIP().ToIPAddress().Address;
        }
        public string GetAccountToken()
        {
            return m_sAccountToken;
        }

        public void InitServer(UInt16 port, string appid, string version, bool enablevac)
        {
            if (enablevac)
                m_eServerMode = EServerMode.eServerModeAuthenticationAndSecure;
            else
                m_eServerMode = EServerMode.eServerModeAuthentication;

            if (!GameServer.Init(0, port, STEAMGAMESERVER_QUERY_PORT_SHARED, m_eServerMode, version))
                Logger.Error("[SteamGameServer] Unable to initialize Steam Game Server.");
            else
                Logger.Log("[SteamGameServer] Initialize Steam Game Server success.");

            //goldsrc game dont need heartbeat
            //if (!Init())
            //    Logger.Error("[CSteamGameServerAPIContext] initialize failed!");

            SteamGameServer.SetProduct(appid);
            SteamGameServer.SetDedicatedServer(true);

            m_bInitialized = true;
        }
        public void LogOn()
        {
            switch (m_eServerMode)
            {
                case EServerMode.eServerModeNoAuthentication:
                    Logger.Log("Initializing Steam libraries for LAN server");
                    break;
                case EServerMode.eServerModeAuthentication:
                    Logger.Log("Initializing Steam libraries for INSECURE Internet server.  Authentication and VAC not requested.");
                    break;
                case EServerMode.eServerModeAuthenticationAndSecure:
                    Logger.Log("Initializing Steam libraries for secure Internet server");
                    break;
                default:
                    Logger.Log("Bogus eServermode {mode}!", m_eServerMode);
                    Logger.Log("Bogus server mode?!");
                    break;
            }

            if (string.IsNullOrWhiteSpace(m_sAccountToken))
            {
                Logger.Warn("****************************************************\n");
                Logger.Warn("*                                                  *\n");
                Logger.Warn("*  No Steam account token was specified.           *\n");
                Logger.Warn("*  Logging into anonymous game server account.     *\n");
                Logger.Warn("*  Connections will be restricted to LAN only.     *\n");
                Logger.Warn("*                                                  *\n");
                Logger.Warn("*  To create a game server account go to           *\n");
                Logger.Warn("*  http://steamcommunity.com/dev/managegameservers *\n");
                Logger.Warn("*                                                  *\n");
                Logger.Warn("****************************************************\n");
                SteamGameServer.LogOnAnonymous();
            }
            else
            {
                Logger.Log("Logging into Steam gameserver account with logon token {token}\n", m_sAccountToken);
                SteamGameServer.LogOn(m_sAccountToken);
            }

            Logger.Log("Waiting for logon result response, this may take a while...\n");

            uint retry = 0;
            while (!m_bLogOnResult)
            {
                if (retry++ > 60)
                {
                    Logger.Log("Can't logon to steam game server after 60 retries!\n");
                    break;
                }
                Thread.Sleep(1000);
                GameServer.RunCallbacks();
            }
        }
        public void SetAccountToken(string token)
        {
            m_sAccountToken = token;
        }

        public void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t pValidateAuthTicketResponse)
        {
            Logger.Log("GC response the result of validation of the ticket [SteamID: {steamid}]", pValidateAuthTicketResponse.m_SteamID.GetUnAccountInstance());
            AuthHolder.GetAuthHolder().SetAuth(pValidateAuthTicketResponse.m_SteamID.GetUnAccountInstance(), pValidateAuthTicketResponse.m_eAuthSessionResponse);
            string reason = pValidateAuthTicketResponse.m_eAuthSessionResponse switch
            {
                EAuthSessionResponse.k_EAuthSessionResponseOK => "Success",
                EAuthSessionResponse.k_EAuthSessionResponseUserNotConnectedToSteam => "The user in question is not connected to steam",
                EAuthSessionResponse.k_EAuthSessionResponseNoLicenseOrExpired => "The license has expired",
                EAuthSessionResponse.k_EAuthSessionResponseVACBanned => "The user is VAC banned for this game",
                EAuthSessionResponse.k_EAuthSessionResponseLoggedInElseWhere => "The user account has logged in elsewhere and the session containing the game instance has been disconnected",
                EAuthSessionResponse.k_EAuthSessionResponseVACCheckTimedOut => "VAC has been unable to perform anti-cheat checks on this user",
                EAuthSessionResponse.k_EAuthSessionResponseAuthTicketCanceled => "The ticket has been canceled by the issuer",
                EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalidAlreadyUsed => "This ticket has already been used, it is not valid.",
                EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalid => "This ticket is not from a user instance currently connected to steam.",
                EAuthSessionResponse.k_EAuthSessionResponsePublisherIssuedBan => "The user is banned for this game. The ban came via the web api and not VAC",
                EAuthSessionResponse.k_EAuthSessionResponseAuthTicketNetworkIdentityFailure => "The network identity failed",
                _ => "Unknown response",
            };
            Logger.Log("Auth response: {response}({reason})", pValidateAuthTicketResponse.m_eAuthSessionResponse, reason);
        }
        public void OnGsPolicyResponse(GSPolicyResponse_t pPolicyResponse)
        {
            if (!BIsActive())
                return;
            Logger.Log("VAC secure mode is {status}", SteamGameServer.BSecure() ? "activated" : "disabled");
        }
        public void OnLogonSuccess(SteamServersConnected_t pLogonSuccess)
        {
            if(!BIsActive())
                return ;
            if(!m_bLogOnResult)
            {
                m_bLogOnResult = true;
            }
            Logger.Log("Connection to Steam server successful");
            IPAddress ip = SteamGameServer.GetPublicIP().ToIPAddress();
            Logger.Log("   Public IP is {status}.", ip.ToString());

            m_SteamIDGS = SteamGameServer.GetSteamID();

            if (m_SteamIDGS.BAnonGameServerAccount())
            {
                Logger.Log("Assigned anonymous gameserver Steam ID {steamid}.", m_SteamIDGS.ToString());
            }
            else if(m_SteamIDGS.BPersistentGameServerAccount())
            {
                Logger.Log("Assigned persistent gameserver Steam ID {steamid}.", m_SteamIDGS.ToString());
            }
            else
            {
                Logger.Warn("Assigned Steam ID {steamid}, which is of an unexpected type!", m_SteamIDGS.ToString());
                Logger.Warn("Unexpected steam ID type!");
            }
            Logger.Log("Gameserver logged on to Steam, assigned identity steamid:{steamid}", m_SteamIDGS.GetUnAccountInstance());
        }
        public void OnLogonFailure(SteamServerConnectFailure_t pLogonFailure)
        {
            if (!BIsActive())
                return;

            if (!m_bLogOnResult)
            {
                string szFatalError = "";
                switch (pLogonFailure.m_eResult)
                {
                    case EResult.k_EResultAccountNotFound:  // account not found
                        szFatalError = "*  Invalid Steam account token was specified.      *";
                        break;
                    case EResult.k_EResultGSLTDenied:       // a game server login token owned by this token's owner has been banned
                        szFatalError = "*  Steam account token specified was revoked.      *";
                        break;
                    case EResult.k_EResultGSOwnerDenied:    // game server owner is denied for other reason (account lock, community ban, vac ban, missing phone)
                        szFatalError = "*  Steam account token owner no longer eligible.   *";
                        break;
                    case EResult.k_EResultGSLTExpired:      // this game server login token was disused for a long time and was marked expired
                        szFatalError = "*  Steam account token has expired from inactivity.*";
                        break;
                    case EResult.k_EResultServiceUnavailable:
                        if (!BLanOnly())
                        {
                            Logger.Log("Connection to Steam servers successful (SU).");
                        }
                        break;
                    default:
                        if (!BLanOnly())
                        {
                            Logger.Log("Could not establish connection to Steam servers.");
                        }
                        break;
                }

                Logger.Error("Could not establish connection to Steam servers, error code : {0}", pLogonFailure.m_eResult);
                if (!string.IsNullOrEmpty(szFatalError))
                {
                    Logger.Crit("****************************************************");
                    Logger.Crit("*                FATAL ERROR                       *");
                    Logger.Crit("{szFatalError}", szFatalError);
                    Logger.Crit("*  Double-check your sv_setsteamaccount setting.   *");
                    Logger.Crit("*                                                  *");
                    Logger.Crit("*  To create a game server account go to           *");
                    Logger.Crit("*  http://steamcommunity.com/dev/managegameservers *");
                    Logger.Crit("*                                                  *");
                    Logger.Crit("****************************************************");
                }
            }
            m_bLogOnResult = true;
        }
        public void OnLoggedOff(SteamServersDisconnected_t pLoggedOff)
        {
            if (!BLanOnly())
            {
                Logger.Warn("Connection to Steam servers lost.  (Result = {result})", pLoggedOff.m_eResult);
            }

            if (GetGSSteamID().BPersistentGameServerAccount())
            {
                switch (pLoggedOff.m_eResult)
                {
                    case EResult.k_EResultLoggedInElsewhere:
                    case EResult.k_EResultLogonSessionReplaced:
                        Logger.Crit("****************************************************");
                        Logger.Crit("*                                                  *");
                        Logger.Crit("*  Steam account token was reused elsewhere.       *");
                        Logger.Crit("*  Make sure you are using a separate account      *");
                        Logger.Crit("*  token for each game server that you operate.    *");
                        Logger.Crit("*                                                  *");
                        Logger.Crit("*  To create additional game server accounts go to *");
                        Logger.Crit("*  http://steamcommunity.com/dev/managegameservers *");
                        Logger.Crit("*                                                  *");
                        Logger.Crit("*  This game server instance will now shut down!   *");
                        Logger.Crit("*                                                  *");
                        Logger.Crit("****************************************************");
                        return;
                }
            }
        }
    }
}
