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

            var guardianInfo = GetGuardiansAndRelationships(classesInfo.Students);

            result.User = guardianInfo.Guardians;
            result.Guardianrelationship = guardianInfo.Guardianrelationships;

            return result;
        }

        private (List<Guardian> Guardians, List<GuardianRelationship> Guardianrelationships) GetGuardiansAndRelationships(List<Student> students)
        {
            List<Guardian> guardians = new List<Guardian>();
            List<GuardianRelationship> guardianrelationships = new List<GuardianRelationship>();

            foreach (OuderVerzorger ouder in vestigingModel.OuderVerzorgers)
            {
                if (ouder.Leerlingen_van_vestiging?.Count > 0)
                {
                    bool guardianFound = false;

                    foreach (Guid leerling in ouder.Leerlingen_van_vestiging)
                    {
                        var leerlingModel = students.Where(s => s.SISid == leerling.ToString()).FirstOrDefault();
                        if (leerlingModel != null && !string.IsNullOrEmpty(ouder.Emailadres))
                        {
                            guardianFound = true;
                            GuardianRelationship gr = new GuardianRelationship();
                            gr.SISid = leerling.ToString();
                            gr.Email = ouder.Emailadres;
                            guardianrelationships.Add(gr);
                        }
                    }

                    if (guardianFound)
                    {
                        Guardian guardian = new Guardian();
                        guardian.SISid = ouder.Uuid.ToString();
                        guardian.Email = ouder.Emailadres;
                        guardian.FirstName = string.IsNullOrEmpty(ouder.Voorvoegsel) ? (!string.IsNullOrEmpty(ouder.Voorletters) ? ouder.Voorletters : ".") : string.Format($"{ouder.Voorvoegsel} {ouder.Achternaam}");
                        guardian.Phone = string.IsNullOrEmpty(ouder.Telefoonnummer) ? "" : BusinessLogicHelper.NormaliseerTelefoonnummerNaarE164(ouder.Telefoonnummer);
                        guardian.LastName = ouder.Achternaam;
                        guardians.Add(guardian);
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
                    if (lesgroep.Docenten?.Count > 0 && lesgroep.Leerlingen?.Count > 0)
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
                            Medewerker currentTeacher = vestigingModel.Medewerkers.Where(m => m.Uuid == mw).FirstOrDefault();
                            if (currentTeacher != null)
                            {
                                TeacherRoster er = new TeacherRoster();
                                er.SISTeacherid = mw.ToString();
                                er.SISSectionid = lg.SISid;
                                teacherRoster.Add(er);

                                Teacher teacher = new Teacher();
                                teacher.SISid = mw.ToString();
                                teacher.SISSchoolid = vestigingModel.Vestiging.Uuid.ToString();
                                teacher.Username = sh.ReplaceTeacherProperty(SettingsHelper.OutputFormatUsernameTeacher, currentTeacher);
                                teachers.Add(teacher);
                            }
                        }

                        foreach (var ll in lesgroep.Leerlingen)
                        {
                            Leerling currentStudent = vestigingModel.Leerlingen.Where(s => s.Uuid == ll.Uuid).FirstOrDefault();
                            if (currentStudent != null)
                            {
                                StudentEnrollment er = new StudentEnrollment();
                                er.SISStudentid = ll.Uuid.ToString();
                                er.SISSectionid = lg.SISid;
                                studentEnrollments.Add(er);

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
            if (teachers.Count() > 0)
            {
                teachers = teachers.GroupBy(t => t.SISid).Select(t => t.First()).ToList(); //only keep unique objects
            }

            if (students.Count() > 0)
            {
                students = students.GroupBy(s => s.SISid).Select(s => s.First()).ToList(); //only keep unique objects
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
