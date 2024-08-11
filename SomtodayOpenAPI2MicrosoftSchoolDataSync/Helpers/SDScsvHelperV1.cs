using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using SomtodayOpenAPI2MicrosoftSchoolDataSyncV2.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    internal class SDScsvHelperV1
    {
        SettingsHelper sh = new SettingsHelper();

        private VestigingModel vestigingModel;

        public SDScsvHelperV1(VestigingModel info)
        {
            this.vestigingModel = info;
        }

        internal SDScsvV1 ConvertToSDSCSV()
        {
            SDScsvV1 result = new SDScsvV1();

            result.Schools = GetSchools();

            var classesInfo = GetClassesAndEnrollments();

            result.Sections = classesInfo.Sections;
            result.Teachers = classesInfo.Teachers;
            result.Students = classesInfo.Students;
            result.TeacherRosters = classesInfo.TeacherRoster;
            result.StudentEnrollments = classesInfo.StudentEnrollments;

            var guardianInfo = GetGuardiansAndRelationships();

            result.User = guardianInfo.Guardians;
            result.Guardianrelationship = guardianInfo.Guardianrelationships;

            return result;
        }

        private (List<Guardian> Guardians, List<GuardianRelationship> Guardianrelationships) GetGuardiansAndRelationships()
        {
            List<Guardian> guardians = new List<Guardian>();
            List<GuardianRelationship> guardianrelationships = new List<GuardianRelationship>();

            foreach (OuderVerzorger ouder in vestigingModel.OuderVerzorgers)
            {
                if (ouder.Leerlingen_van_vestiging?.Count > 0)
                {
                    Guardian guardian = new Guardian();

                    guardian.SISid = ouder.Uuid.ToString();
                    guardian.Email = ouder.Emailadres;
                    guardian.FirstName = string.IsNullOrEmpty(ouder.Voorvoegsel) ? ouder.Voorletters : string.Format($"{ouder.Voorvoegsel} {ouder.Achternaam}");
                    guardian.Phone = ouder.Telefoonnummer;
                    guardian.LastName = ouder.Achternaam;
                    guardians.Add(guardian);

                    foreach (Guid leerling in ouder.Leerlingen_van_vestiging)
                    {
                        GuardianRelationship gr = new GuardianRelationship();
                        gr.SISid = leerling.ToString();
                        gr.Email = ouder.Emailadres;
                        guardianrelationships.Add(gr);
                    }
                }
            }
            return (guardians, guardianrelationships);
        }




        private (List<Section> Sections, List<Teacher> Teachers, List<Student> Students, List<TeacherRoster> TeacherRoster, List<StudentEnrollment> StudentEnrollments) GetClassesAndEnrollments()
        {
            string currentSchoolyear = DateTime.Now.Month < 8 ? (DateTime.Now.Year - 1) + "-" + DateTime.Now.Year : DateTime.Now.Year + "-" + (DateTime.Now.Year + 1);
            List<Section> sections = new List<Section>();
            List<Teacher> teachers = new List<Teacher>();
            List<Student> students = new List<Student>();
            List<TeacherRoster> teacherRoster = new List<TeacherRoster>();
            List<StudentEnrollment> studentEnrollments = new List<StudentEnrollment>();

            string vestigingsAfkorting = vestigingModel.Vestiging.Afkorting;
            foreach (Lesgroep lesgroep in vestigingModel.Lesgroepen)
            {
                if (!string.IsNullOrEmpty(lesgroep.Naam))
                {
                    if (lesgroep.Docenten.Count > 0 && lesgroep.Leerlingen.Count > 0)
                    {
                        string sectieNaam = GetFilteredName(lesgroep.Naam);
                        Section lg = new Section();
                        lg.SISSchoolid = vestigingModel.Vestiging.Uuid.ToString();
                        lg.SISid = (lesgroep.Naam.ToLower().StartsWith(vestigingsAfkorting.ToLower()) ? sectieNaam : vestigingsAfkorting.ToLower() + sectieNaam) + currentSchoolyear;
                        lg.Name = sectieNaam;
                        lg.Number = lesgroep.Uuid.ToString();
                        lg.CourseName = lesgroep.Vaknaam;
                        lg.CourseDescription = lesgroep.Onderwijssoort;
                        sections.Add(lg);


                        foreach (var mw in lesgroep.Docenten)
                        {
                            TeacherRoster er = new TeacherRoster();
                            er.SISTeacherid = mw.ToString();
                            er.SISSectionid = lg.SISid;
                            teacherRoster.Add(er);

                            Medewerker currentTeacher = vestigingModel.Medewerkers.Where(m => m.Uuid == mw).FirstOrDefault();
                            if (currentTeacher != null)
                            {
                                Teacher teacher = new Teacher();
                                teacher.SISid = mw.ToString();
                                teacher.SISSchoolid = vestigingModel.Vestiging.Uuid.ToString();
                                teacher.Username = sh.ReplaceTeacherProperty(SettingsHelper.OutputFormatUsernameTeacher, currentTeacher);
                                teachers.Add(teacher);
                            }
                        }

                        foreach (var ll in lesgroep.Leerlingen)
                        {
                            StudentEnrollment er = new StudentEnrollment();
                            er.SISStudentid = ll.Uuid.ToString();
                            er.SISSectionid = lg.SISid;
                            studentEnrollments.Add(er);

                            Leerling currentStudent = vestigingModel.Leerlingen.Where(s => s.Uuid == ll.Uuid).FirstOrDefault();
                            if (currentStudent != null)
                            {
                                Student student = new Student();
                                student.SISid = ll.Uuid.ToString();
                                student.SISSchoolid = vestigingModel.Vestiging.Uuid.ToString();
                                student.Username = sh.ReplaceStudentProperty(SettingsHelper.OutputFormatUsernameStudent, currentStudent);
                                students.Add(student);
                            }
                        }
                    }
                }
            }
            return (sections, teachers, students, teacherRoster, studentEnrollments);
        }



        private List<School> GetSchools()
        {
            List<School> result = new List<School>();
            School _school = new School();
            _school.SISid = vestigingModel.Vestiging.Uuid.ToString();
            _school.Name = vestigingModel.Vestiging.Naam;
            result.Add(_school);
            return result;
        }


        private string GetFilteredName(string input)
        {
            //Alles met een spatie of verboden teken voor OneDrive wordt omgezet naar _
            string _temp = Regex.Replace(input, @"[^\S]|[\~\""\#\%\&\*\:\<\>\?\/\\{\|}\.\[\]]", "_");
            return RemoveDiacritics(_temp);
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
