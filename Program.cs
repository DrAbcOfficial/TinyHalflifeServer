using Newtonsoft.Json;
using TinyHalflifeServer.Json;

namespace TinyHalflifeServer
{
    internal class Program
    {
        public static Config? Config = null;
        static void Main(string[] args)
        {
            Logger.Log("Hello, World!");
            const string configPath = "./config.json";
            if (Path.Exists(configPath))
            {
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            }
            if(Config == null)
            {
                Config = new();
                string json = JsonConvert.SerializeObject(Config);
                File.WriteAllText(configPath, json);
            }
        }
    }
}
