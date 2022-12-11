using SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers;
using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using SomtodayOpenAPI2MicrosoftSchoolDataSyncV2.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync
{
    internal class Program
    {
        static readonly bool booleanFilterBylocation = bool.Parse(ConfigurationManager.AppSettings["BooleanFilterBylocation"]);
        static readonly bool seperateOutputFolderForEachLocation = bool.Parse(ConfigurationManager.AppSettings["SeperateOutputFolderForEachLocation"]);
        static readonly string[] includedLocationCode = ConfigurationManager.AppSettings["IncludedLocationCode"].Split(';');
        static readonly string schoolUUID = ConfigurationManager.AppSettings["SchoolUUID"];
        static readonly string clientId = ConfigurationManager.AppSettings["ClientId"];
        static readonly string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
        static readonly string outputFolder = ConfigurationManager.AppSettings["OutputFolder"].EndsWith("\\") ? ConfigurationManager.AppSettings["OutputFolder"] : ConfigurationManager.AppSettings["OutputFolder"] + "\\";
        static readonly bool enableGuardianSync = bool.Parse(ConfigurationManager.AppSettings["EnableGuardianSync"]);

        public static EventLogHelper eh = new EventLogHelper();
        public static OpenAPIHelper oh;
        public static FileHelper fh = new FileHelper();


        #region sluiten van app door user
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                default:
                    eh.WriteLog("Applicatie afgebroken", EventLogEntryType.Warning, 400);
                    return false;
            }
        }
        #endregion



        static void Main(string[] args)
        {
            DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;

            eh.CheckEventLog();
            //sluiten van app door user
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
            //sluiten van app door user


            SettingsHelper settingshelper = new SettingsHelper();
            if (settingshelper.ValidateUsernameFormat())
            {
                eh.WriteLog("Sync gestart met applicatieversie: " + buildDate.ToString("o").Split('T')[0], EventLogEntryType.Information, 100);

                oh = new OpenAPIHelper(clientId, clientSecret, schoolUUID);
                int i = 1;
                while (!oh.IsConnected)
                {
                    Console.WriteLine("try again..." + i);
                    oh = new OpenAPIHelper(clientId, clientSecret, schoolUUID);
                    i++;
                }
                if (oh.IsConnected)
                {
                    List<VestigingModel> allInfo = oh.DownloadAllInfo(booleanFilterBylocation, includedLocationCode, enableGuardianSync);
                    List<SDScsv> sdsCsvList = new List<SDScsv>();
                    foreach (VestigingModel info in allInfo)
                    {
                        eh.WriteLog($"VestigingsInfo: {info.Vestiging.Naam}, {info.Lesgroepen.Count} Lesgroepen, {info.Medewerkers.Count} Docenten, {info.Leerlingen.Count} Leerlingen, {info.OuderVerzorgers.Count} Ouders");
                        SDScsvHelper sh = new SDScsvHelper(info);
                        SDScsv sdsCsv = sh.ConvertToSDSCSV();
                        if (seperateOutputFolderForEachLocation)
                        {
                            string actualOutputFolder = outputFolder + info.Vestiging.Afkorting + "\\";
                            eh.WriteLog($"Schrijven naar: {actualOutputFolder}");
                            fh.SaveToDisk(sdsCsv, actualOutputFolder);
                        }
                        else
                        {
                            sdsCsvList.Add(sdsCsv);
                        }
                    }
                    if (sdsCsvList.Count > 0 && !seperateOutputFolderForEachLocation)
                    {
                        eh.WriteLog($"Alles schrijven naar: {outputFolder}");
                        fh.SaveToDisk(sdsCsvList, outputFolder);
                    }
                }

                Console.WriteLine("======================================");
                eh.WriteLog("Sync voltooid", EventLogEntryType.Information, 100);
                
            }
            Thread.Sleep(10000);
        }
    }
}

