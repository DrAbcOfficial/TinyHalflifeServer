namespace TinyHalflifeServer.A2S
{
    //Fake A2S builder
    internal class ModInfo
    {
        public enum ModType
        {
            SingleAndMultiPlay,
            MultiPlayOnly,
            Unknown
        }
        public enum ModDLL
        {
            HalfLife,
            Other
        }

        public string m_ModUrl = "";
        public string m_ModDownloadUrl = "";
        public long m_ModVersion = 0;
        public long m_ModSize = 0;
        public ModType m_ModType = ModType.Unknown;
        public ModDLL m_ModDLL = ModDLL.Other;
    }
    internal class PlayerInfo
    {
        public string m_Name = "";
        public long m_Score = 0;
        public float m_Duration = 0.0f;
    }
    internal class RulesInfo
    {
        public string m_Name = "";
        public string m_Value = "";
    }
    internal class ServerInfo
    {
        #region A2S_INFO
        #region GetterSetter
        public string GetServerAddress() { return m_ServerAddress; }
        public void SetServerAddress(string ip_port) { m_ServerAddress = ip_port; }
        public string GetServerName() { return m_ServerName; }
        public void SetServerName(string serverName) { m_ServerName = serverName; }
        public string GetServerMap() { return m_ServerMap; }
        public void SetServerMap(string serverMap) { m_ServerMap = serverMap; }
        public string GetServerGameFolder() { return m_ServerGameFolder; }
        public void SetServerGameFolder(string serverGameFolder) { m_ServerGameFolder = serverGameFolder; }
        public string GetServerDescription() { return m_ServerDescription; }
        public void SetServerDescription(string serverDescription) { m_ServerDescription = serverDescription; }
        public uint GetServerMaxNumClients() { return m_ServerMaxClients; }
        public void SetServerMaxNumClients(uint clients) { m_ServerMaxClients = clients; }
        public uint GetServerProtocol() { return m_ServerProtocol; }
        public void SetServerProtocol(uint protocol) { m_ServerProtocol = protocol; }
        public ServerType GetServerType() { return m_ServerType; }
        public void SetServerType(ServerType type) { m_ServerType = type; }
        public ServerOS GetServerOS() { return m_ServerOS; }
        public void SetServerOS(ServerOS os) { m_ServerOS = os; }
        public bool GetServerPassworded() { return m_ServerPassworded; }
        public void SetServerPassworded(bool passwded) { m_ServerPassworded = passwded; }
        public bool GetServerModed() { return m_ServerModed; }
        public void SetServerModed(bool moded) { m_ServerModed = moded; }
        public ModInfo GetServerModInfo() { return m_ServerModInfo; }
        public bool GetServerVACStatus() { return m_ServerVacStatus; }
        public void SetServerVACStatus(bool status) { m_ServerVacStatus = status; }
        public uint GetServerNumFakeClients() { return m_ServerNumFakeClients; }
        public void SetServerNumFakeClients(uint num) { m_ServerNumFakeClients = num; }
        #endregion
        #region Enum
        public enum ServerType
        {
            Dedicated,
            Listen,
            HLTV,
            Other
        }
        public enum ServerOS
        {
            Linux,
            Windows,
            Other
        }
        #endregion
        #region Varible
        string m_ServerAddress = "";
        string m_ServerName = "";
        string m_ServerMap = "";
        string m_ServerGameFolder = "";
        string m_ServerDescription = "";
        uint m_ServerMaxClients = 0;
        uint m_ServerProtocol = 0;
        ServerType m_ServerType = ServerType.Other;
        ServerOS m_ServerOS = ServerOS.Other;
        bool m_ServerPassworded = false;
        bool m_ServerModed = false;
        ModInfo m_ServerModInfo = new();
        bool m_ServerVacStatus = false;
        uint m_ServerNumFakeClients = 0;
        #endregion
        public byte[] RenderA2SInfoRespond()
        {
            List<byte> buffer = [];
            using (MemoryStream ms = new())
            {
                using (BinaryWriter bw = new(ms))
                {
                    // Nonsense Prefix
                    bw.Write((long)0xFFFFFFFF);
                    // Header	byte	Always equal to 'm' (0x6D)
                    bw.Write((byte)0x6D);
                    //Address	string	IP address and port of the server.
                    bw.Write(m_ServerAddress);
                    //Name	string	Name of the server.
                    bw.Write(m_ServerName);
                    //Map	string	Map the server has currently loaded.
                    bw.Write(m_ServerMap);
                    //Folder	string	Name of the folder containing the game files.
                    bw.Write(m_ServerGameFolder);
                    //Game	string	Full name of the game.
                    bw.Write(m_ServerDescription);
                    //Players	byte	Number of players on the server.
                    bw.Write((byte)m_ServerPlayers.Count);
                    //Max. Players	byte	Maximum number of players the server reports it can hold.
                    bw.Write((byte)m_ServerMaxClients);
                    //Protocol	byte	Protocol version used by the server.
                    bw.Write((byte)m_ServerProtocol);
                    /* Server type	byte	Indicates the type of server:
                       'D' for dedicated server
                       'L' for non-dedicated server
                       'P' for a HLTV server */
                    switch (m_ServerType)
                    {
                        case ServerType.Dedicated: bw.Write((byte)0x44); break;//D
                        case ServerType.Listen: bw.Write((byte)0x4C); break;//L
                        case ServerType.HLTV: bw.Write((byte)0x50); break;//P
                        default: bw.Write((byte)0x00); break;
                    }
                    /* Environment	byte	Indicates the operating system of the server:
                       'L' for Linux
                       'W' for Windows*/
                    switch (m_ServerOS)
                    {
                        case ServerOS.Linux: bw.Write((byte)0x4C); break;//L
                        case ServerOS.Windows: bw.Write((byte)0x57); break;//W
                        default: bw.Write((byte)0x00); break;
                    }
                    /*Visibility	byte	Indicates whether the server requires a password:
                        0 for public
                        1 for private*/
                    bw.Write((byte)(m_ServerPassworded ? 0x01 : 0x00));
                    /*Mod	byte	Indicates whether the game is a mod:
                        0 for Half-Life
                        1 for Half-Life mod*/
                    if (m_ServerModed)
                    {
                        bw.Write((byte)0x01);
                        //These fields are only present in the response if "Mod" is 1:
                        //Link	string	URL to mod website.
                        bw.Write(m_ServerModInfo.m_ModUrl);
                        //Download Link	string	URL to download the mod.
                        bw.Write(m_ServerModInfo.m_ModDownloadUrl);
                        //NULL	byte	NULL byte (0x00)
                        bw.Write((byte)0x00);
                        //Version	long	Version of mod installed on server.
                        bw.Write(m_ServerModInfo.m_ModVersion);
                        //Size	long	Space (in bytes) the mod takes up.
                        bw.Write(m_ServerModInfo.m_ModSize);
                        /*Type	byte	Indicates the type of mod:
                            0 for single and multiplayer mod
                            1 for multiplayer only mod*/
                        bw.Write((byte)(m_ServerModInfo.m_ModType == ModInfo.ModType.SingleAndMultiPlay ? 0x00 : 0x01));
                        /*DLL	byte	Indicates whether mod uses its own DLL:
                            0 if it uses the Half-Life DLL
                            1 if it uses its own DLL*/
                        bw.Write((byte)(m_ServerModInfo.m_ModDLL == ModInfo.ModDLL.HalfLife ? 0x00 : 0x01));
                    }
                    else
                        bw.Write((byte)0x00);
                    /*VAC	byte	Specifies whether the server uses VAC:
                        0 for unsecured
                        1 for secured*/
                    bw.Write((byte)(m_ServerVacStatus ? 0x01 : 0x00));
                    //Bots	byte	Number of bots on the server.
                    bw.Write((byte)m_ServerNumFakeClients);
                }
                ms.ToArray().CopyTo(buffer.ToArray(), 0);
            }
            return [.. buffer];
        }
        #endregion
        #region A2S_PLAYER 
        List<PlayerInfo> m_ServerPlayers = [];
        public PlayerInfo GetPlayerInfo(int index) { return m_ServerPlayers[index]; }
        public List<PlayerInfo> GetPlayerInfos() { return m_ServerPlayers; }
        public byte[] RenderA2SPlayerRespond()
        {
            List<byte> buffer = [];
            using (MemoryStream ms = new())
            {
                using (BinaryWriter bw = new(ms))
                {
                    // Nonsense Prefix
                    bw.Write((long)0xFFFFFFFF);
                    //Header	byte	Always equal to 'D' (0x44)
                    bw.Write((byte)0x44);
                    //Players	byte	Number of players whose information was gathered.
                    bw.Write((byte)m_ServerPlayers.Count);
                    //For every player in "Players" there is this chunk in the response:
                    uint index = 0;
                    foreach (PlayerInfo info in m_ServerPlayers)
                    {
                        //Index	byte	Index of player chunk starting from 0.
                        bw.Write((byte)index);
                        //Name	string	Name of the player.
                        bw.Write(info.m_Name);
                        //Score	long	Player's score (usually "frags" or "kills".)
                        bw.Write(info.m_Score);
                        //Duration	float	Time (in seconds) player has been connected to the server.
                        bw.Write(info.m_Duration);
                        index++;
                    }
                }
                ms.ToArray().CopyTo(buffer.ToArray(), 0);
            }
            return [.. buffer];
        }
        #endregion
        #region A2S_RULES
        List<RulesInfo> m_RulesInfos = [];
        public List<RulesInfo> GetRulesInfos() { return m_RulesInfos; }
        public byte[] RenderA2SRulesRespond()
        {
            List<byte> buffer = [];
            using (MemoryStream ms = new())
            {
                using (BinaryWriter bw = new(ms))
                {
                    // Nonsense Prefix
                    bw.Write((long)0xFFFFFFFF);
                    //Header	byte	Always equal to 'E' (0x45)
                    bw.Write((byte)0x45);
                    //Rules	short	Number of rules in the response.
                    bw.Write((short)m_RulesInfos.Count);
                    //For every rule in "Rules" there is this chunk in the response:
                    foreach (RulesInfo info in m_RulesInfos)
                    {
                        //Name	string	Name of the rule.
                        bw.Write(info.m_Name);
                        //Value	string	Value of the rule.
                        bw.Write(info.m_Value);
                    }
                }
                ms.ToArray().CopyTo(buffer.ToArray(), 0);
            }
            return [.. buffer];
        }
        #endregion
        #region A2A_PING
        public byte[] RenderA2APingRespond()
        {
            List<byte> buffer = [];
            using (MemoryStream ms = new())
            {
                using (BinaryWriter bw = new(ms))
                {
                    // Nonsense Prefix
                    bw.Write((long)0xFFFFFFFF);
                    //Header	byte	'j' (0x6A)
                    bw.Write((byte)0x6A);
                    //Payload	string	Null
                    bw.Write("");
                }
                ms.ToArray().CopyTo(buffer.ToArray(), 0);
            }
            return [.. buffer];
        }
        #endregion
        #region A2S_SERVERQUERY_GETCHALLENGE
        public byte[] RenderA2SServerQueryGetChallengeRespond()
        {
            List<byte> buffer = [];
            using (MemoryStream ms = new())
            {
                using (BinaryWriter bw = new(ms))
                {
                    // Nonsense Prefix
                    bw.Write((long)0xFFFFFFFF);
                    //Header	byte	Should be equal to 'A' (0x41).
                    bw.Write((byte)0x41);
                    //Challenge	long	The challenge number to use.
                    bw.Write((long)0xFFFFFFFF);
                }
                ms.ToArray().CopyTo(buffer.ToArray(), 0);
            }
            return [.. buffer];
        }
        #endregion
    }
}
