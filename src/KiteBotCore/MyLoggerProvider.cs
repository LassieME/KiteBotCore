using System;
using Microsoft.Extensions.Logging;

namespace KiteBotCore
{
    public partial class Program
    {
        public class MyLoggerProvider : ILoggerProvider
        {
            public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
            {
                return new MyLogger();
            }

            public void Dispose()
            { }

            public class MyLogger : Microsoft.Extensions.Logging.ILogger
            {
                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    //Console.WriteLine($"------------\n{formatter(state, exception)}\n------------");
                    switch (logLevel)
                    {
                        case LogLevel.Critical:
                            Serilog.Log.Fatal($"------------\n{formatter(state, exception)}\n------------");
                            break;
                        case LogLevel.Error:
                            Serilog.Log.Error($"------------\n{formatter(state, exception)}\n------------");
                            break;
                        case LogLevel.Warning:
                            Serilog.Log.Warning($"------------\n{formatter(state, exception)}\n------------");
                            break;
                        case LogLevel.Information:
                            Serilog.Log.Information($"------------\n{formatter(state, exception)}\n------------");
                            break;
                        case LogLevel.Debug:
                            //Serilog.Log.Debug($"------------\n{formatter(state, exception)}\n------------");
                            break;
                        case LogLevel.Trace:
                            //Serilog.Log.Verbose($"------------\n{formatter(state, exception)}\n------------");
                            break;
                    }
                }

                public IDisposable BeginScope<TState>(TState state)
                {
                    return null;
                }
            }
        }
    }
}