using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using SomtodayOpenAPI2MicrosoftSchoolDataSyncV2.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
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
            ResolvedPopulation population = ResolvedPopulation.Create(vestigingModel);
            SDScsvV1 result = new SDScsvV1
            {
                Schools = population.Classes.Count > 0 ? GetSchools() : new List<School>()
            };

            var classesInfo = GetClassesAndEnrollments(population);

            result.Sections = classesInfo.Sections;
            result.Teachers = classesInfo.Teachers;
            result.Students = classesInfo.Students;
            result.TeacherRosters = classesInfo.TeacherRoster;
            result.StudentEnrollments = classesInfo.StudentEnrollments;

            var guardianInfo = GetGuardiansAndRelationships(population.StudentIds);

            result.User = guardianInfo.Guardians;
            result.Guardianrelationship = guardianInfo.Guardianrelationships;

            return result;
        }

        private (List<Guardian> Guardians, List<GuardianRelationship> Guardianrelationships) GetGuardiansAndRelationships(HashSet<Guid> includedStudentIds)
        {
            List<Guardian> guardians = new List<Guardian>();
            List<GuardianRelationship> guardianrelationships = new List<GuardianRelationship>();

            foreach (OuderVerzorger ouder in vestigingModel.OuderVerzorgers ?? new List<OuderVerzorger>())
            {
                if (ouder.Leerlingen_van_vestiging?.Count > 0)
                {
                    bool guardianFound = false;

                    foreach (Guid leerling in ouder.Leerlingen_van_vestiging)
                    {
                        if (includedStudentIds.Contains(leerling) && !string.IsNullOrEmpty(ouder.Emailadres))
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




        private (List<Section> Sections, List<Teacher> Teachers, List<Student> Students, List<TeacherRoster> TeacherRoster, List<StudentEnrollment> StudentEnrollments) GetClassesAndEnrollments(ResolvedPopulation population)
        {
            string currentSchoolyear = DateTime.Now.Month < 8 ? (DateTime.Now.Year - 1) + "-" + DateTime.Now.Year : DateTime.Now.Year + "-" + (DateTime.Now.Year + 1);
            List<Section> sections = new List<Section>();
            List<Teacher> teachers = new List<Teacher>();
            List<Student> students = new List<Student>();
            List<TeacherRoster> teacherRoster = new List<TeacherRoster>();
            List<StudentEnrollment> studentEnrollments = new List<StudentEnrollment>();

            string vestigingsAfkorting = vestigingModel.Vestiging.Afkorting;
            foreach (ResolvedClass resolvedClass in population.Classes)
            {
                        Lesgroep lesgroep = resolvedClass.Group;
                        string sectieNaam = resolvedClass.FilteredName;
                        Section lg = new Section();
                        lg.SISSchoolid = vestigingModel.Vestiging.Uuid.ToString();
                        lg.SISid = (lesgroep.Naam.ToLower().StartsWith(vestigingsAfkorting.ToLower()) ? sectieNaam : vestigingsAfkorting.ToLower() + sectieNaam) + currentSchoolyear;
                        lg.Name = sectieNaam;
                        lg.Number = lesgroep.Uuid.ToString();
                        lg.CourseName = lesgroep.Vaknaam;
                        lg.CourseDescription = lesgroep.Onderwijssoort;
                        sections.Add(lg);


                        foreach (Medewerker currentTeacher in resolvedClass.Teachers)
                        {
                                TeacherRoster er = new TeacherRoster();
                                er.SISTeacherid = currentTeacher.Uuid.ToString();
                                er.SISSectionid = lg.SISid;
                                teacherRoster.Add(er);

                                Teacher teacher = new Teacher();
                                teacher.SISid = currentTeacher.Uuid.ToString();
                                teacher.SISSchoolid = vestigingModel.Vestiging.Uuid.ToString();
                                teacher.Username = sh.ReplaceTeacherProperty(SettingsHelper.OutputFormatUsernameTeacher, currentTeacher);
                                teachers.Add(teacher);
                        }

                        foreach (Leerling currentStudent in resolvedClass.Students)
                        {
                                StudentEnrollment er = new StudentEnrollment();
                                er.SISStudentid = currentStudent.Uuid.ToString();
                                er.SISSectionid = lg.SISid;
                                studentEnrollments.Add(er);

                                Student student = new Student();
                                student.SISid = currentStudent.Uuid.ToString();
                                student.SISSchoolid = vestigingModel.Vestiging.Uuid.ToString();
                                student.Username = sh.ReplaceStudentProperty(SettingsHelper.OutputFormatUsernameStudent, currentStudent);
                                students.Add(student);
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
    }
}
