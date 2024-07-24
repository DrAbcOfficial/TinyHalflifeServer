namespace TinyHalflifeServer.Json;
public class Config
{
    public string Product { get; set; }
    public int AppId { get; set; }
    public string Version { get; set; }
    public string GSLT { get; set; }
    public int Port { get; set; }
    public bool GoldSrc { get; set; }
    public bool Debug { get; set; }
    public JsonText Text { get; set; }
    public JsonMirror RDIP { get; set; }
    public JsonServerInfo ServerInfo { get; set; }
}
