using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCoreServer;
using Newtonsoft.Json;
using Steamworks;
using TinyHalflifeServer.Json;
using static TinyHalflifeServer.Program;

namespace TinyHalflifeServer
{
    internal class Program
    {
        public static Config Config;
        static void Main()
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

            var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<HostedService>();
            })
            .Build();
            host.Run();
        }

        public class HostedService(IHostApplicationLifetime appLifetime) : IHostedService
        {
            private readonly IHostApplicationLifetime _appLifetime = appLifetime;
            private readonly Server m_Server = new();
            public Task StartAsync(CancellationToken cancellationToken)
            {
                _appLifetime.ApplicationStopping.Register(OnStopping);
                SteamAPI.Init();
                m_Server.StartRun();
                return Task.CompletedTask;
            }
            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
            private void OnStopping()
            {
                m_Server.Stop();
                SteamAPI.Shutdown();
                Logger.Log("Goodbye world...");
            }
        }
    }
}
