using Microsoft.Extensions.Logging;
namespace TinyHalflifeServer
{
    internal class Program
    {
        private static ILogger logger = LoggerFactory.Create(static builder =>
        {
            builder.AddConsole();
        }).CreateLogger<Program>();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }

        public static ILogger Logger()
        {
            return logger;
        }
    }
}
