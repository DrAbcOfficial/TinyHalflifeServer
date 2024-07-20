namespace TinyHalflifeServer.Json
{
    public class JsonMirror
    {
        public bool Enable = false;
        public string Destination = "127.0.0.1:27105";
    }
    public class JsonModInfo
    {
        public string Url = "github.com";
        public string DownloadUrl = "notevenexists.com";
        public int Version = 0;
        public int Size = 0;
        public int Type = 0;
        public int DLL = 1;
    }
    public class JsonPlayerInfo
    {
        public string Name;
        public int Score;
        public float Duration;
    }
    public class JsonRulesInfo
    {
        public string Name;
        public string Value;
    }
    public class JsonServerInfo
    {
        public string Name = "Tiny Sven coop Server";
        public string Map = "Not even exists";
        public string GameFolder = "svencoop";
        public string Description = "Tiny Sven Co-op";
        public int MaxClients = 32;
        public int Protocol = 43;
        public int Type = 0;
        public int OS = 0;
        public bool Passworded = true;
        public bool Moded = false;
        public bool VAC = true;
        public int FakeClients = 0;
        public JsonModInfo ModInfo = new();
        public List<JsonPlayerInfo> Players = [];
        public List<JsonRulesInfo> Rules = [];
    }
    public class Config
    {
        public string AppId = "svencoop";
        public string Version = "5.0.1.7";
        public string GSLT = "";
        public int Port = 27015;
        public JsonMirror RDIP = new();
        public JsonServerInfo ServerInfo = new();
    }
}
