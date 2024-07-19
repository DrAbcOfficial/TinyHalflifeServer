using Microsoft.Extensions.Logging;
using Steamworks;
using System;
using System.Net;

namespace TinyHalflifeServer
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

    class SteamServer
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

        SteamServer()
        {
            m_CallbackLogonSuccess = Callback<SteamServersConnected_t>.CreateGameServer(OnLogonSuccess);
            m_CallbackLogonFailure = Callback<SteamServerConnectFailure_t>.CreateGameServer(OnLogonFailure);
            m_CallbackLoggedOff = Callback<SteamServersDisconnected_t>.CreateGameServer(OnLoggedOff);

            m_CallbackGSPolicyResponse = Callback<GSPolicyResponse_t>.CreateGameServer(OnGsPolicyResponse);
            m_CallbackValidateAuthTicketResponse = Callback<ValidateAuthTicketResponse_t>.CreateGameServer(OnValidateAuthTicketResponse);
        }
        bool BSecure()
        {
            return SteamGameServer.BSecure();
        }
        bool BIsActive()
        {
            return m_eServerMode >= EServerMode.eServerModeNoAuthentication;
        }
        bool BLanOnly()
        {
            return m_eServerMode == EServerMode.eServerModeNoAuthentication;
        }
        bool BWantsSecure()
        {
            return m_eServerMode == EServerMode.eServerModeAuthenticationAndSecure;
        }
        bool BLoggedOn()
        {
            return SteamGameServer.BLoggedOn();
        }
        CSteamID GetGSSteamID()
        {
            return m_SteamIDGS;
        }
        bool BHasLogonResult()
        {
            return m_bLogOnResult;
        }
        void LogOnAnonymous()
        {
            SteamGameServer.LogOnAnonymous();
        }
        UInt16 GetQueryPort()
        {
            return m_QueryPort;
        }
        long GetPublicIP()
        {
            return SteamGameServer.GetPublicIP().ToIPAddress().Address;
        }
        string GetAccountToken()
        {
            return m_sAccountToken;
        }

        void InitServer(UInt16 port, string appid, string version, bool enablevac)
        {
            if (enablevac)
                m_eServerMode = EServerMode.eServerModeAuthenticationAndSecure;
            else
                m_eServerMode = EServerMode.eServerModeAuthentication;

            if (!GameServer.Init(0, port, STEAMGAMESERVER_QUERY_PORT_SHARED, m_eServerMode, version))
                Program.Logger().LogError("[SteamGameServer] Unable to initialize Steam Game Server.");
            else
                Program.Logger().LogInformation("[SteamGameServer] Initialize Steam Game Server success.");

            //goldsrc game dont need heartbeat
            //if (!Init())
            //    Program.Logger().LogError("[CSteamGameServerAPIContext] initialize failed!");

            SteamGameServer.SetProduct(appid);
            SteamGameServer.SetDedicatedServer(true);

            m_bInitialized = true;
        }

        void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t pValidateAuthTicketResponse)
        {
            Program.Logger().LogInformation("GC response the result of validation of the ticket [SteamID: {steamid}]", pValidateAuthTicketResponse.m_SteamID.GetUnAccountInstance());
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
            Program.Logger().LogInformation("Auth response: {response}({reason})", pValidateAuthTicketResponse.m_eAuthSessionResponse, reason);
        }
        void OnGsPolicyResponse(GSPolicyResponse_t pPolicyResponse)
        {
            if (!BIsActive())
                return;
            Program.Logger().LogInformation("VAC secure mode is {status}", SteamGameServer.BSecure() ? "activated" : "disabled");
        }
        void OnLogonSuccess(SteamServersConnected_t pLogonSuccess)
        {
            if(!BIsActive())
                return ;
            if(!m_bLogOnResult)
            {
                m_bLogOnResult = true;
            }
            Program.Logger().LogInformation("Connection to Steam server successful");
            IPAddress ip = SteamGameServer.GetPublicIP().ToIPAddress();
            Program.Logger().LogInformation("   Public IP is {status}.", ip.ToString());

            m_SteamIDGS = SteamGameServer.GetSteamID();

            if (m_SteamIDGS.BAnonGameServerAccount())
            {
                Program.Logger().LogInformation("Assigned anonymous gameserver Steam ID {steamid}.", m_SteamIDGS.ToString());
            }
            else if(m_SteamIDGS.BPersistentGameServerAccount())
            {
                Program.Logger().LogInformation("Assigned persistent gameserver Steam ID {steamid}.", m_SteamIDGS.ToString());
            }
            else
            {
                Program.Logger().LogWarning("Assigned Steam ID {steamid}, which is of an unexpected type!", m_SteamIDGS.ToString());
                Program.Logger().LogWarning("Unexpected steam ID type!");
            }
            Program.Logger().LogInformation("Gameserver logged on to Steam, assigned identity steamid:{steamid}", m_SteamIDGS.GetUnAccountInstance());
        }
        void OnLogonFailure(SteamServerConnectFailure_t pLogonFailure)
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
                            Program.Logger().LogInformation("Connection to Steam servers successful (SU).");
                        }
                        break;
                    default:
                        if (!BLanOnly())
                        {
                            Program.Logger().LogInformation("Could not establish connection to Steam servers.");
                        }
                        break;
                }

                Program.Logger().LogError("Could not establish connection to Steam servers, error code : {0}", pLogonFailure.m_eResult);
                if (!string.IsNullOrEmpty(szFatalError))
                {
                    Program.Logger().LogCritical("****************************************************");
                    Program.Logger().LogCritical("*                FATAL ERROR                       *");
                    Program.Logger().LogCritical("{szFatalError}", szFatalError);
                    Program.Logger().LogCritical("*  Double-check your sv_setsteamaccount setting.   *");
                    Program.Logger().LogCritical("*                                                  *");
                    Program.Logger().LogCritical("*  To create a game server account go to           *");
                    Program.Logger().LogCritical("*  http://steamcommunity.com/dev/managegameservers *");
                    Program.Logger().LogCritical("*                                                  *");
                    Program.Logger().LogCritical("****************************************************");
                }
            }
            m_bLogOnResult = true;
        }
        void OnLoggedOff(SteamServersDisconnected_t pLoggedOff)
        {
            if (!BLanOnly())
            {
                Program.Logger().LogWarning("Connection to Steam servers lost.  (Result = {result})", pLoggedOff.m_eResult);
            }

            if (GetGSSteamID().BPersistentGameServerAccount())
            {
                switch (pLoggedOff.m_eResult)
                {
                    case EResult.k_EResultLoggedInElsewhere:
                    case EResult.k_EResultLogonSessionReplaced:
                        Program.Logger().LogCritical("****************************************************");
                        Program.Logger().LogCritical("*                                                  *");
                        Program.Logger().LogCritical("*  Steam account token was reused elsewhere.       *");
                        Program.Logger().LogCritical("*  Make sure you are using a separate account      *");
                        Program.Logger().LogCritical("*  token for each game server that you operate.    *");
                        Program.Logger().LogCritical("*                                                  *");
                        Program.Logger().LogCritical("*  To create additional game server accounts go to *");
                        Program.Logger().LogCritical("*  http://steamcommunity.com/dev/managegameservers *");
                        Program.Logger().LogCritical("*                                                  *");
                        Program.Logger().LogCritical("*  This game server instance will now shut down!   *");
                        Program.Logger().LogCritical("*                                                  *");
                        Program.Logger().LogCritical("****************************************************");
                        return;
                }
            }
        }
    }
}
