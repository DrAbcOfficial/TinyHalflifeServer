using System.Text.Json;
using Steamworks;
using TinyHalflifeServer.Json;

namespace TinyHalflifeServer
{
    internal class Program
    {
        public static Config Config;
        public static Server m_Server;
        static void Main()
        {
            Logger.Log("Hello, World!");
            const string configPath = "./config.json";
            if (Path.Exists(configPath))
            {
                Config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
            }
            if(Config == null)
            {
                Config = new();
                string json = JsonSerializer.Serialize(Config);
                File.WriteAllText(configPath, json);
            }

            m_Server = new();

            SteamAPI.Init();
            m_Server.StartRun();

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            Console.WriteLine("Application is running. Press Ctrl+C to exit.");
            Console.ReadLine();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            m_Server.Stop();
            SteamAPI.Shutdown();
            Logger.Log("Goodbye world...");
        }
    }
}
