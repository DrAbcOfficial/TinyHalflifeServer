using System.Text.Json;
using Steamworks;
using TinyHalflifeServer.Json;

namespace TinyHalflifeServer;

internal class Program
{
    public static Config? Config;
    public static Server? m_Server;
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
            Config = new()
            {
                Product = "svencoop",
                AppId = 225840,
                Version = "5.0.1.7",
                GSLT = "",
                Port = 27015,
                GoldSrc = false,
                Debug = false,
                RDIP = new()
                {
                    Enable = false,
                    IP = "127.0.0.1",
                    Port = 27105
                },
                ServerInfo = new()
                {
                    Name = "Tiny Sven Server",
                    Map = "Fanta Sea",
                    GameFolder = "svencoop",
                    Description = "Tiny Sven Co-op",
                    MaxClients = 32,
                    Protocol = 17,
                    Type = 0,
                    OS = 0,
                    Passworded = true,
                    Moded =false,
                    VAC = true,
                    FakeClients = 0,
                    ModInfo = new()
                    {
                        Url = "github.com",
                        DownloadUrl = "github.com",
                        Version = 0,
                        Size = 0,
                        Type = 0,
                        DLL = 0
                    },
                    Players = [new() { Name = "Tiny", Score = 114, Duration = 1919810.0f}],
                    Rules = [new(){Name="coop", Value="fuckno"}]
                },
                Text = new()
                {
                    Kicked = "You have been banned from this server."
                }
            };
            string json = JsonSerializer.Serialize(Config);
            File.WriteAllText(configPath, json);
            Logger.Warn("Can not find config file, generated into {0}", Path.GetFullPath(configPath));
        }

        m_Server = new();

        SteamAPI.Init();
        m_Server.StartRun();

        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        Logger.Log("Application is running. Press Ctrl+C to exit.");
        Console.ReadLine();
    }

    static void OnProcessExit(object? sender, EventArgs e)
    {
        m_Server?.Stop();
        SteamAPI.Shutdown();
        Logger.Log("Goodbye world...");
    }
}
