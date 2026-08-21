using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using System;
using System.Collections.Generic;
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
        CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            Encoding = Encoding.UTF8
        };

        internal void SaveJsonToDisk(List<VestigingModel> allInfo, string targetFolder)
        {
            //save json to disk. use system.text.json
            string json = System.Text.Json.JsonSerializer.Serialize(allInfo);
            string filePath = Path.Combine(targetFolder, "allInfo.json");

            File.WriteAllText(filePath, json);
        }

        internal List<VestigingModel> LoadJsonFromDisk(string targetFolder)
        {
            List<VestigingModel> allInfo = new List<VestigingModel>();
            //load json from disk. use system.text.json
            string json = File.ReadAllText(targetFolder + "allInfo.json");
            allInfo = System.Text.Json.JsonSerializer.Deserialize<List<VestigingModel>>(json);
            return allInfo;
        }

        internal void SaveV1ToDisk(List<SDScsvV1> sdsCsv, string actualOutputFolder)
        {
            SDScsvV1 completeList = new SDScsvV1()
            {
                Schools = sdsCsv.SelectMany(o => o.Schools).ToList(),
                Sections = sdsCsv.SelectMany(o => o.Sections).ToList(),
                Teachers = sdsCsv.SelectMany(o => o.Teachers).ToList(),
                Students = sdsCsv.SelectMany(o => o.Students).ToList(),
                TeacherRosters = sdsCsv.SelectMany(o => o.TeacherRosters)
                    .GroupBy(roster => new { roster.SISSectionid, roster.SISTeacherid }).Select(group => group.First()).ToList(),
                StudentEnrollments = sdsCsv.SelectMany(o => o.StudentEnrollments)
                    .GroupBy(enrollment => new { enrollment.SISSectionid, enrollment.SISStudentid }).Select(group => group.First()).ToList(),
                User = sdsCsv.SelectMany(o => o.User).GroupBy(guardian => guardian.SISid).Select(group => group.First()).ToList(),
                Guardianrelationship = sdsCsv.SelectMany(o => o.Guardianrelationship)
                    .GroupBy(relationship => new { relationship.SISid, relationship.Email, relationship.Role }).Select(group => group.First()).ToList()
            };
            SaveV1ToDisk(completeList, actualOutputFolder);
        }
        internal void SaveV1ToDisk(SDScsvV1 sdsCsv, string actualOutputFolder)
        {
            CreateOutputFolderIfNeeded(actualOutputFolder);

            using (TextWriter writer = new StreamWriter(actualOutputFolder + @"School.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<SchoolCSVMap>();
                csv.WriteRecords(sdsCsv.Schools);
            }

            using (TextWriter writer = new StreamWriter(actualOutputFolder + @"Section.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<SectionCSVMap>();
                csv.WriteRecords(sdsCsv.Sections);
            }

            using (TextWriter writer = new StreamWriter(actualOutputFolder + @"Teacher.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<TeacherCSVMap>();
                csv.WriteRecords(sdsCsv.Teachers);
            }

            using (TextWriter writer = new StreamWriter(actualOutputFolder + @"Student.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<StudentCSVMap>();
                csv.WriteRecords(sdsCsv.Students);
            }

            using (TextWriter writer = new StreamWriter(actualOutputFolder + @"TeacherRoster.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<TeacherRosterCSVMap>();
                csv.WriteRecords(sdsCsv.TeacherRosters);
            }

            using (TextWriter writer = new StreamWriter(actualOutputFolder + @"StudentEnrollment.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<StudentEnrollmentCSVMap>();
                csv.WriteRecords(sdsCsv.StudentEnrollments);
            }

            if (sdsCsv.User.Count > 0)
            {
                using (TextWriter writer = new StreamWriter(actualOutputFolder + @"User.csv"))
                {
                    var csv = new CsvWriter(writer, config);
                    csv.Context.RegisterClassMap<GuardianCSVMap>();
                    csv.WriteRecords(sdsCsv.User);
                }

                using (TextWriter writer = new StreamWriter(actualOutputFolder + @"Guardianrelationship.csv"))
                {
                    var csv = new CsvWriter(writer, config);
                    csv.Context.RegisterClassMap<GuardianRelationshipCSVMap>();
                    csv.WriteRecords(sdsCsv.Guardianrelationship);
                }
            }
        }

        internal void SaveV2ToDisk(List<SDScsvV2> sdsCsvList, string outputFolder)
        {
            SDScsvV2 completelist = new SDScsvV2()
            {
                orgs = sdsCsvList.SelectMany(o => o.orgs).ToList(),
                classes = sdsCsvList.SelectMany(c => c.classes).ToList(),
                enrollments = sdsCsvList.SelectMany(e => e.enrollments)
                    .GroupBy(enrollment => new { enrollment.classSourcedId, enrollment.userSourcedId, enrollment.role }).Select(group => group.First()).ToList(),
                relationships = sdsCsvList.SelectMany(r => r.relationships)
                    .GroupBy(relationship => new { relationship.userSourcedId, relationship.relationshipUserSourcedId, relationship.relationshipRole }).Select(group => group.First()).ToList(),
                roles = sdsCsvList.SelectMany(r => r.roles)
                    .GroupBy(role => new { role.userSourcedId, role.orgSourcedId, role.role }).Select(group => group.First()).ToList(),
                users = sdsCsvList.SelectMany(u => u.users).GroupBy(user => user.sourcedId).Select(group => group.First()).ToList(),
            };
            SaveV2ToDisk(completelist, outputFolder);
        }
        internal void SaveV2ToDisk(SDScsvV2 sdsCsv, string outputFolder)
        {
            CreateOutputFolderIfNeeded(outputFolder);

            using (TextWriter writer = new StreamWriter(outputFolder + @"orgs.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<SdsOrganizationMap>();
                csv.WriteRecords(sdsCsv.orgs);
            }

            using (TextWriter writer = new StreamWriter(outputFolder + @"users.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<SdsUserMap>();
                csv.WriteRecords(sdsCsv.users);
            }

            using (TextWriter writer = new StreamWriter(outputFolder + @"roles.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<SdsRoleMap>();
                csv.WriteRecords(sdsCsv.roles);
            }

            using (TextWriter writer = new StreamWriter(outputFolder + @"classes.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<SdsClassMap>();
                csv.WriteRecords(sdsCsv.classes);
            }

            using (TextWriter writer = new StreamWriter(outputFolder + @"enrollments.csv"))
            {
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<SdsEnrollmentMap>();
                csv.WriteRecords(sdsCsv.enrollments);
            }

            if (sdsCsv.relationships.Count > 0)
            {
                using (TextWriter writer = new StreamWriter(outputFolder + @"relationships.csv"))
                {
                    var csv = new CsvWriter(writer, config);
                    csv.Context.RegisterClassMap<SdsRelationshipMap>();
                    csv.WriteRecords(sdsCsv.relationships);
                }
            }
        }

        private void CreateOutputFolderIfNeeded(string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
            {
                eh.WriteLog(String.Format("Output directory bestaat niet, maar wordt nu aangemaakt: {0} ", outputFolder), LogLevel.Information, 100);
                Directory.CreateDirectory(outputFolder);
            }
        }

        internal void ClearCsvFiles(string outputFolder, bool seperateOutputFolderForEachLocation)
        {
            if (seperateOutputFolderForEachLocation)
            {
                string[] folders = Directory.GetDirectories(outputFolder);
                foreach (string folder in folders)
                {
                    EmptyCsvFiles(folder);
                }
            }
            else
            {
                EmptyCsvFiles(outputFolder);
            }
        }

        private void EmptyCsvFiles(string folder)
        {
            // Get all CSV files in the folder
            string[] csvFiles = Directory.GetFiles(folder, "*.csv");
            foreach (string csvFile in csvFiles)
            {
                // Read the first line to keep the header
                string firstLine = File.ReadLines(csvFile).FirstOrDefault();
                // Clear the file content
                File.WriteAllText(csvFile, firstLine + Environment.NewLine);
            }
        }
    }
}
