namespace TinyHalflifeServer.Json
{
    public class JsonMirror
    {
        public bool Enable { get; set; }
        public string Destination { get; set; }
    }
    public class JsonModInfo
    {
        public string Url { get; set; }
        public string DownloadUrl { get; set; }
        public int Version { get; set; }
        public int Size { get; set; }
        public int Type { get; set; }
        public int DLL { get; set; }
    }
    public class JsonPlayerInfo
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public float Duration { get; set; }
    }
    public class JsonRulesInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public class JsonServerInfo
    {
        public string Name { get; set; }
        public string Map { get; set; }
        public string GameFolder { get; set; }
        public string Description { get; set; }
        public int MaxClients { get; set; }
        public int Protocol { get; set; }
        public int Type { get; set; }
        public int OS { get; set; }
        public bool Passworded { get; set; }
        public bool Moded { get; set; }
        public bool VAC { get; set; }
        public int FakeClients { get; set; }
        public JsonModInfo ModInfo = new();
        public List<JsonPlayerInfo> Players { get; set; }
        public List<JsonRulesInfo> Rules { get; set; }
    }
    public class Config
    {
        public string Product { get; set; }
        public int AppId { get; set; }
        public string Version { get; set; }
        public string GSLT { get; set; }
        public int Port { get; set; }
        public bool GoldSrc { get; set; }
        public bool Debug { get; set; }
        public JsonMirror RDIP { get; set; }
        public JsonServerInfo ServerInfo { get; set; }
    }
}
