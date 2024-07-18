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
        static bool booleanFilterBylocation;
        static bool seperateOutputFolderForEachLocation;
        static string[] includedLocationCode;
        static string schoolUUID;
        static string clientId;
        static string clientSecret;
        static string outputFolder;
        static bool enableGuardianSync;
        static SomEnvironmentConfig somOmgeving;

        public static EventLogHelper eh = new EventLogHelper();
        public static OpenAPIHelper oh;
        public static FileHelper fh = new FileHelper();


        #region sluiten van app door user
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        static bool InitializeConfiguration()
        {
            bool isValid = true;

            if (!bool.TryParse(ConfigurationManager.AppSettings["BooleanFilterBylocation"], out booleanFilterBylocation))
            {
                eh.WriteLog("Fout: BooleanFilterBylocation is ongeldig of ontbreekt in App.Config. ", EventLogEntryType.Error, 400);
                isValid = false;
            }

            if (!bool.TryParse(ConfigurationManager.AppSettings["SeperateOutputFolderForEachLocation"], out seperateOutputFolderForEachLocation))
            {
                eh.WriteLog("Fout: SeperateOutputFolderForEachLocation is ongeldig of ontbreekt in App.Config.", EventLogEntryType.Error, 400);
                isValid = false;
            }

            includedLocationCode = ConfigurationManager.AppSettings["IncludedLocationCode"]?.Split(';');
            if (includedLocationCode == null || includedLocationCode.Length == 0)
            {
                eh.WriteLog("Fout: IncludedLocationCode is ongeldig of ontbreek in App.Config.", EventLogEntryType.Error, 400);
                isValid = false;
            }

            schoolUUID = ConfigurationManager.AppSettings["SchoolUUID"];
            if (string.IsNullOrWhiteSpace(schoolUUID))
            {
                eh.WriteLog("Fout: SchoolUUID is ongeldig of ontbreekt in App.Config.", EventLogEntryType.Error, 400);
                isValid = false;
            }

            clientId = ConfigurationManager.AppSettings["ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                eh.WriteLog("Fout: ClientId is ongeldig of ontbreekt in App.Config.", EventLogEntryType.Error, 400);
                isValid = false;
            }

            clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                eh.WriteLog("Fout: ClientSecret is ongeldig of ontbreekt in App.Config.", EventLogEntryType.Error, 400);
                isValid = false;
            }

            outputFolder = ConfigurationManager.AppSettings["OutputFolder"];
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                eh.WriteLog("Fout: OutputFolder is ongeldig of ontbreekt in App.Config.", EventLogEntryType.Error, 400);
                isValid = false;
            }
            else
            {
                outputFolder = outputFolder.EndsWith("\\") ? outputFolder : outputFolder + "\\";
            }

            if (!bool.TryParse(ConfigurationManager.AppSettings["EnableGuardianSync"], out enableGuardianSync))
            {
                eh.WriteLog("Fout: EnableGuardianSync is ongeldig of ontbreekt in App.Config.", EventLogEntryType.Error, 400);
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["SomOmgeving"]))
            {
                eh.WriteLog("Fout: SomOmgeving is ongeldig of ontbreekt in App.Config.", EventLogEntryType.Error, 400);
                isValid = false;
            }
            else
            {
                string somOmgevingstring = ConfigurationManager.AppSettings["SomOmgeving"];
                switch (somOmgevingstring[0].ToString().ToLower()) //eerste letter is voldoende
                {
                    case "a":
                        somOmgeving = SomEnvironmentConfig.Acceptatie;
                        break;
                    case "n":
                        somOmgeving = SomEnvironmentConfig.Nightly;
                        break;
                    case "t":
                        somOmgeving = SomEnvironmentConfig.Test;
                        break;
                    case "p":
                    default:
                        somOmgeving = SomEnvironmentConfig.Prod;
                        break;
                }
            }
            return isValid;
        }

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
        static Program()
        {
            if (!InitializeConfiguration())
            {
                eh.WriteLog("De configuratie bevat fouten. Controleer alstublieft het config bestand.", EventLogEntryType.Error, 400);
                Environment.Exit(1);
            }
        }


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

                oh = new OpenAPIHelper(clientId, clientSecret, schoolUUID, somOmgeving);
                int i = 1;
                while (!oh.IsConnected && i < 20)
                {
                    Console.WriteLine("try again..." + i);
                    oh = new OpenAPIHelper(clientId, clientSecret, schoolUUID, somOmgeving);
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
                else
                {
                    eh.WriteLog("Geen verbinding met Somtoday", EventLogEntryType.Error, 100);
                }

                Console.WriteLine("======================================");
                eh.WriteLog("Sync voltooid", EventLogEntryType.Information, 100);

            }
            Thread.Sleep(10000);
        }


    }
}

