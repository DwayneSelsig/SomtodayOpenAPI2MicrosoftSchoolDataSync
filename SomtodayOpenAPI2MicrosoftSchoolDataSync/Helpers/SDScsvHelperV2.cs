using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using SomtodayOpenAPI2MicrosoftSchoolDataSyncV2.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    internal class SDScsvHelperV2
    {
        SettingsHelper sh = new SettingsHelper();

        private VestigingModel vestigingModel;

        public SDScsvHelperV2(VestigingModel info)
        {
            this.vestigingModel = info;
        }

        internal SDScsvV2 ConvertToSDSCSV()
        {
            ResolvedPopulation population = ResolvedPopulation.Create(vestigingModel);
            SDScsvV2 result = new SDScsvV2();

            result.orgs = population.Classes.Count > 0 ? GetOrgs() : new List<SdsOrganization>();
            result.users = GetUsers(population);
            result.roles = GetRoles(population);


            Tuple<List<SdsClass>, List<SdsEnrollment>> classesInfo = GetClassesAndEnrolements(population);
            result.classes = classesInfo.Item1;
            result.enrollments = classesInfo.Item2;

            result.relationships = GetRelationships(population);


            return result;
        }

        private List<SdsRelationship> GetRelationships(ResolvedPopulation population)
        {
            List<SdsRelationship> relationships = new List<SdsRelationship>();

            foreach (OuderVerzorger ouder in vestigingModel.OuderVerzorgers ?? new List<OuderVerzorger>())
            {
                foreach (Guid leerling in ouder.Leerlingen_van_vestiging ?? Array.Empty<Guid>())
                {
                    if (population.StudentIds.Contains(leerling) && !string.IsNullOrEmpty(ouder.Emailadres))
                    {
                        SdsRelationship rel = new SdsRelationship();
                        rel.userSourcedId = leerling.ToString();
                        rel.relationshipUserSourcedId = ouder.Uuid.ToString();
                        rel.relationshipRole = "guardian"; // https://learn.microsoft.com/en-us/schooldatasync/default-list-of-values#contact-relationship-roles
                        relationships.Add(rel);
                    }
                }
            }
            return relationships
                .GroupBy(relationship => new { relationship.userSourcedId, relationship.relationshipUserSourcedId, relationship.relationshipRole })
                .Select(group => group.First())
                .ToList();
        }

        private Tuple<List<SdsClass>, List<SdsEnrollment>> GetClassesAndEnrolements(ResolvedPopulation population)
        {
            List<SdsClass> classes = new List<SdsClass>();
            List<SdsEnrollment> enrollments = new List<SdsEnrollment>();

            string currentSchoolyear = DateTime.Now.Month < 8 ? (DateTime.Now.Year - 1) + "-" + DateTime.Now.Year : DateTime.Now.Year + "-" + (DateTime.Now.Year + 1);

            foreach (ResolvedClass resolvedClass in population.Classes)
            {
                    Lesgroep lesgroep = resolvedClass.Group;
                    SdsClass lg = new SdsClass();
                    string sectieNaam = resolvedClass.FilteredName;

                    lg.title = sectieNaam;
                    lg.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                    lg.sourcedId = (sectieNaam.ToLower().StartsWith(vestigingModel.Vestiging.Afkorting.ToLower()) ? sectieNaam : vestigingModel.Vestiging.Afkorting.ToLower() + sectieNaam) + currentSchoolyear;

                    classes.Add(lg);
                    foreach (Medewerker teacher in resolvedClass.Teachers)
                    {
                        SdsEnrollment er = new SdsEnrollment();
                        er.classSourcedId = lg.sourcedId;
                        er.userSourcedId = teacher.Uuid.ToString();
                        er.role = "teacher";  // https://learn.microsoft.com/en-us/schooldatasync/default-list-of-values#enrollment-roles
                        enrollments.Add(er);
                    }
                    foreach (Leerling student in resolvedClass.Students)
                    {
                        SdsEnrollment er = new SdsEnrollment();
                        er.classSourcedId = lg.sourcedId;
                        er.userSourcedId = student.Uuid.ToString();
                        er.role = "student"; // https://learn.microsoft.com/en-us/schooldatasync/default-list-of-values#enrollment-roles
                        enrollments.Add(er);
                    }
            }
            return Tuple.Create<List<SdsClass>, List<SdsEnrollment>>(classes, enrollments
                .GroupBy(enrollment => new { enrollment.classSourcedId, enrollment.userSourcedId, enrollment.role })
                .Select(group => group.First())
                .ToList());
        }


        private List<SdsRole> GetRoles(ResolvedPopulation population)
        {
            List<SdsRole> result = new List<SdsRole>();
            foreach (Medewerker mw in (vestigingModel.Medewerkers ?? new List<Medewerker>()).Where(medewerker => population.TeacherIds.Contains(medewerker.Uuid)))
            {
                SdsRole role = new SdsRole();
                role.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                role.userSourcedId = mw.Uuid.ToString();
                role.role = "staff";
                result.Add(role);
            }

            foreach (Leerling ll in (vestigingModel.Leerlingen ?? new List<Leerling>()).Where(leerling => population.StudentIds.Contains(leerling.Uuid)))
            {
                SdsRole role = new SdsRole();
                role.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                role.userSourcedId = ll.Uuid.ToString();
                role.role = "student";
                result.Add(role);
            }

            foreach (OuderVerzorger ov in GetIncludedGuardians(population))
            {
                if (!string.IsNullOrEmpty(ov.Emailadres))
                {
                    SdsRole role = new SdsRole();
                    role.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                    role.userSourcedId = ov.Uuid.ToString();
                    role.role = "other";
                    result.Add(role);
                }
            }
            return result
                .GroupBy(role => new { role.userSourcedId, role.orgSourcedId, role.role })
                .Select(group => group.First())
                .ToList();
        }

        private List<SdsUser> GetUsers(ResolvedPopulation population)
        {
            List<SdsUser> result = new List<SdsUser>();
            foreach (Medewerker mw in (vestigingModel.Medewerkers ?? new List<Medewerker>()).Where(medewerker => population.TeacherIds.Contains(medewerker.Uuid)))
            {
                SdsUser user = new SdsUser();
                user.username = sh.ReplaceTeacherProperty(SettingsHelper.OutputFormatUsernameTeacher, mw);
                user.sourcedId = mw.Uuid.ToString();
                result.Add(user);

            }

            foreach (Leerling ll in (vestigingModel.Leerlingen ?? new List<Leerling>()).Where(leerling => population.StudentIds.Contains(leerling.Uuid)))
            {
                SdsUser user = new SdsUser();
                user.username = sh.ReplaceStudentProperty(SettingsHelper.OutputFormatUsernameStudent, ll);
                user.sourcedId = ll.Uuid.ToString();
                result.Add(user);
            }

            foreach (OuderVerzorger ov in GetIncludedGuardians(population))
            {
                    SdsUser user = new SdsUser();
                    user.username = ov.Emailadres;
                    user.sourcedId = ov.Uuid.ToString();
                    user.phone = BusinessLogicHelper.NormaliseerTelefoonnummerNaarE164(ov.Telefoonnummer);
                    result.Add(user);
            }
            return result.GroupBy(user => user.sourcedId).Select(group => group.First()).ToList();
        }

        private IEnumerable<OuderVerzorger> GetIncludedGuardians(ResolvedPopulation population)
        {
            return (vestigingModel.OuderVerzorgers ?? new List<OuderVerzorger>())
                .Where(guardian => !string.IsNullOrEmpty(guardian.Emailadres))
                .Where(guardian => (guardian.Leerlingen_van_vestiging ?? Array.Empty<Guid>()).Any(population.StudentIds.Contains));
        }

        private List<SdsOrganization> GetOrgs()
        {
            List<SdsOrganization> result = new List<SdsOrganization>();
            SdsOrganization _org = new SdsOrganization();
            _org.sourcedId = vestigingModel.Vestiging.Uuid.ToString();
            _org.name = vestigingModel.Vestiging.Naam;
            _org.type = "school";
            result.Add(_org);
            return result;
        }

        private string GetVestigingsIds()
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in vestigingModel.Vestiging.Afkorting)
            {
                int x = c;
                result.Append(x.ToString("000"));
            }
            return result.ToString().TrimStart('0');
        }
    }
}
