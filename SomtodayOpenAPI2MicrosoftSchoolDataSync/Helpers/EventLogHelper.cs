using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    class EventLogHelper
    {
        private EventLog appLog;
        bool boolLogCreating = true;


        public EventLogHelper()
        {
            appLog = new EventLog
            {
                Log = "Application",
                Source = GetType().ToString().Split('.')[0]
            };

        }

        public void WriteLog(string Message, EventLogEntryType eventType = EventLogEntryType.Information, int eventId = 0)
        {
            try
            {
                WriteLogUnsafe(Message, eventType, eventId);
            }
            catch
            {

            }
        }

        public void WriteLogUnsafe(string Message, EventLogEntryType eventType = EventLogEntryType.Information, int eventId = 0)
        {
            Console.ResetColor();
            switch (eventType)
            {
                case EventLogEntryType.Error:
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(Message);
                    break;
                case EventLogEntryType.Warning:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(Message);
                    break;
                case EventLogEntryType.Information:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(Message);
                    break;
                case EventLogEntryType.SuccessAudit:
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(Message);
                    break;
                case EventLogEntryType.FailureAudit:
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(Message);
                    break;
                default:
                    Console.WriteLine(Message);
                    break;
            }
            Console.ResetColor();
            appLog.WriteEntry(Message, eventType, eventId);
        }

        internal void CheckEventLog()
        {
            bool createEntry = false;
            createEntry = !LogExists();
            if (createEntry)
            {
                CreateLog();
            }
        }

        internal void CreateLog()
        {
            Console.Write("Druk op een toets om een Windows Event Log aan te maken... ");
            var task = Task.Run(() => Console.ReadKey(true));
            bool read = task.Wait(10000);
            if (read)
            {
                try
                {
                    System.Diagnostics.EventLog.CreateEventSource(source: appLog.Source, logName: appLog.Log);
                }
                catch
                {
                    //waarschijnlijk draait SDSsync niet als Admin.
                    var proc = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = @"powershell.exe",
                        Arguments = "-command New-EventLog -Source \"" + appLog.Source + "\" -LogName \"" + appLog.Log + "\"",
                        Verb = "runas"
                    };

                    try
                    {
                        Process powerShellCreateEntry = Process.Start(proc);
                        powerShellCreateEntry.EnableRaisingEvents = true;
                        powerShellCreateEntry.Exited += powerShellCreateEntry_Exited;
                        Console.WriteLine("Eventlog wordt aangemaakt.");
                    }
                    catch (Exception powerShellCreate)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(String.Format("Eventlog aanmaken mislukt! ", powerShellCreate.Message));
                        Console.ResetColor();
                        boolLogCreating = false;
                    }
                }
                int timeout = 0;
                while (boolLogCreating)
                {
                    timeout++;
                    Thread.Sleep(1000);
                    if (timeout > 30)
                    {
                        Console.WriteLine("Timeout!");
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Sync gestart zonder logs te schrijven.");
            }
        }


        private void powerShellCreateEntry_Exited(object sender, EventArgs e)
        {
            Process proc = sender as Process;
            if (proc.ExitCode == 0)
            {
                try
                {
                    WriteLogUnsafe("Log succesvol aangemaakt!");
                    boolLogCreating = false;
                }
                catch
                {
                    //CheckEventLog();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Eventlog aanmaken mislukt!");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Eventlog aanmaken mislukt!");
                Console.ResetColor();
            }
        }


        internal void DeleteLog()
        {
            var proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = @"powershell.exe",
                Arguments = "-command Remove-EventLog -Source \"" + appLog.Source + "\"",
                Verb = "runas"
            };

            try
            {
                // Remove-EventLog  -Source "UMService2LANschoolCSV"
                Process powerShellCreateEntry = Process.Start(proc);
                Console.WriteLine("Eventlog wordt verwijderd.");
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Eventlog verwijderen mislukt!");
                Console.ResetColor();
            }
            Thread.Sleep(2000);
        }

        internal bool LogExists()
        {
            bool exists = true;
            try
            {
                if (!EventLog.SourceExists(appLog.Source))
                {
                    exists = false;
                }
            }
            catch
            {
                exists = false;
            }

            return exists;
        }
    }
}
