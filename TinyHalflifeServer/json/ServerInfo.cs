namespace TinyHalflifeServer.Json;
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
    public List<JsonRuleInfo> Rules { get; set; }
}