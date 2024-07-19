using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyHalflifeServer
{
    internal class Logger
    {
        private static ILogger logger = LoggerFactory.Create(static builder =>
        {
            builder.AddConsole();
        }).CreateLogger<Logger>();

        public static void Log(string message, params object?[] arg)
        {
            logger.LogInformation(message, arg);
        }
        public static void Warn(string message, params object?[] arg)
        {
            logger.LogWarning(message, arg);
        }
        public static void Error(string message, params object?[] arg)
        {
            logger.LogError(message, arg);
        }
        public static void Crit(string message, params object?[] arg)
        {
            logger.LogCritical(message, arg);
        }
    }
}
