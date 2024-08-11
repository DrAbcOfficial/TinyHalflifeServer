namespace TinyHalflifeServer.Json;
public class JsonMirror
{
    public bool Enable { get; set; }
    public string? IP { get; set; }
    public int Port { get; set; }
    //0 = stufftext
    //1 = reconnect
    public int Method { get; set; }
}