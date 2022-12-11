using CsvHelper;
using CsvHelper.Configuration;
using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    internal class FileHelper
    {

        EventLogHelper eh = Program.eh;
        internal void SaveToDisk(List<SDScsv> sdsCsvList, string outputFolder)
        {
            SDScsv completelist = new SDScsv()
            {
                orgs = sdsCsvList.SelectMany(o => o.orgs).ToList(),
                classes = sdsCsvList.SelectMany(c => c.classes).ToList(),
                enrollments = sdsCsvList.SelectMany(e => e.enrollments).ToList(),
                relationships = sdsCsvList.SelectMany(r => r.relationships).ToList(),
                roles = sdsCsvList.SelectMany(r => r.roles).ToList(),
                users = sdsCsvList.SelectMany(u => u.users).ToList(),
            };
            SaveToDisk(completelist, outputFolder);
        }
        internal void SaveToDisk(SDScsv sdsCsv, string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
            {
                eh.WriteLog(String.Format("Output directory bestaat niet, maar wordt nu aangemaakt: {0} ", outputFolder), EventLogEntryType.Information, 100);
                Directory.CreateDirectory(outputFolder);
            }


            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                Encoding = Encoding.UTF8
            };


            using (TextWriter writer = new StreamWriter(outputFolder + @"orgs.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<orgsClassMap>();
                csv.WriteRecords(sdsCsv.orgs);
            }

            using (TextWriter writer = new StreamWriter(outputFolder + @"users.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<usersClassMap>();
                csv.WriteRecords(sdsCsv.users);
            }

            using (TextWriter writer = new StreamWriter(outputFolder + @"roles.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<rolesClassMap>();
                csv.WriteRecords(sdsCsv.roles);
            }

            using (TextWriter writer = new StreamWriter(outputFolder + @"classes.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<classesClassMap>();
                csv.WriteRecords(sdsCsv.classes);
            }

            using (TextWriter writer = new StreamWriter(outputFolder + @"enrollments.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<enrollmentsClassMap>();
                csv.WriteRecords(sdsCsv.enrollments);
            }

            if (sdsCsv.relationships.Count > 0)
            {
                using (TextWriter writer = new StreamWriter(outputFolder + @"relationships.csv"))
                {
                    var csv = new CsvWriter(writer, config);
                    csv.Context.RegisterClassMap<relationshipsClassMap>();
                    csv.WriteRecords(sdsCsv.relationships);
                }
            }
        }
    }
}
