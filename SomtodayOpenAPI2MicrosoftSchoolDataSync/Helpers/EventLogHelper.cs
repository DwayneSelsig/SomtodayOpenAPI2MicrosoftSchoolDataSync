using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

#pragma warning disable CA1416 // This project targets Windows and this file configures Windows Event Log.

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    internal sealed class EventLogHelper : IDisposable
    {
        private const string LogName = "Application";
        private const string SourceName = "SomtodayOpenAPI2MicrosoftSchoolDataSync";

        private ILoggerFactory loggerFactory;
        private ILogger logger;

        public EventLogHelper()
        {
            ConfigureLogger(includeEventLog: false);
        }

        public void WriteLog(string message, LogLevel logLevel = LogLevel.Information, int eventId = 0)
        {
            try
            {
                logger.Log(logLevel, new EventId(eventId), "{Message}", message);
            }
            catch
            {
                Console.WriteLine(message);
            }
        }

        internal void CheckEventLog()
        {
            if (LogExists())
            {
                ConfigureLogger(includeEventLog: true);
                return;
            }

            CreateLog();
        }

        internal void CreateLog()
        {
            Console.Write("Druk op een toets om een Windows Event Log aan te maken... ");

            if (Console.IsInputRedirected)
            {
                Console.WriteLine();
                Console.WriteLine("Sync gestart zonder Windows Event Log te schrijven.");
                return;
            }

            var keyPress = Task.Run(() => Console.ReadKey(intercept: true));
            if (!keyPress.Wait(10000))
            {
                Console.WriteLine();
                Console.WriteLine("Sync gestart zonder Windows Event Log te schrijven.");
                return;
            }

            try
            {
                EventLog.CreateEventSource(SourceName, LogName);
                ConfigureLogger(includeEventLog: true);
                WriteLog("Log succesvol aangemaakt!");
            }
            catch
            {
                CreateLogWithElevation();
            }
        }

        private void CreateLogWithElevation()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command New-EventLog -Source \"" + SourceName + "\" -LogName \"" + LogName + "\"",
                Verb = "runas"
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (!process.WaitForExit(30000) || process.ExitCode != 0 || !LogExists())
                    {
                        Console.WriteLine("Eventlog aanmaken mislukt!");
                        return;
                    }
                }

                ConfigureLogger(includeEventLog: true);
                WriteLog("Log succesvol aangemaakt!");
            }
            catch
            {
                Console.WriteLine("Eventlog aanmaken mislukt!");
            }
        }

        internal void DeleteLog()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command Remove-EventLog -Source \"" + SourceName + "\"",
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                Console.WriteLine("Eventlog wordt verwijderd.");
            }
            catch
            {
                Console.WriteLine("Eventlog verwijderen mislukt!");
            }
        }

        internal bool LogExists()
        {
            try
            {
                return EventLog.SourceExists(SourceName);
            }
            catch
            {
                return false;
            }
        }

        private void ConfigureLogger(bool includeEventLog)
        {
            loggerFactory?.Dispose();
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddSimpleConsole(options =>
                {
                    options.ColorBehavior = LoggerColorBehavior.Enabled;
                    options.SingleLine = true;
                });

                if (includeEventLog)
                {
                    builder.AddEventLog(settings =>
                    {
                        settings.LogName = LogName;
                        settings.SourceName = SourceName;
                    });
                    builder.AddFilter<EventLogLoggerProvider>((_, level) => level >= LogLevel.Information);
                }
            });
            logger = loggerFactory.CreateLogger("SomtodayOpenAPI2MicrosoftSchoolDataSync");
        }

        public void Dispose()
        {
            loggerFactory?.Dispose();
        }
    }
}

#pragma warning restore CA1416
