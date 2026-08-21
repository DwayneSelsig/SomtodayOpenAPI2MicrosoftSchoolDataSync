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
            SDScsvV2 result = new SDScsvV2();

            result.orgs = GetOrgs();
            result.users = GetUsers();
            result.roles = GetRoles();


            Tuple<List<SdsClass>, List<SdsEnrollment>> classesInfo = GetClassesAndEnrolements();
            result.classes = classesInfo.Item1;
            result.enrollments = classesInfo.Item2;

            result.relationships = GetRelationships();


            return result;
        }

        private List<SdsRelationship> GetRelationships()
        {
            List<SdsRelationship> relationships = new List<SdsRelationship>();

            foreach (OuderVerzorger ouder in vestigingModel.OuderVerzorgers)
            {
                foreach (Guid leerling in ouder.Leerlingen_van_vestiging)
                {
                    //Heeft deze ouder een gekoppelde leerling?
                    var leerlingModel = vestigingModel.Leerlingen.Where(s => s.Uuid == leerling).FirstOrDefault();

                    if (leerlingModel != null && !string.IsNullOrEmpty(ouder.Emailadres))
                    {
                        SdsRelationship rel = new SdsRelationship();
                        rel.userSourcedId = leerling.ToString();
                        rel.relationshipUserSourcedId = ouder.Uuid.ToString();
                        rel.relationshipRole = "guardian"; // https://learn.microsoft.com/en-us/schooldatasync/default-list-of-values#contact-relationship-roles
                        relationships.Add(rel);
                    }
                }
            }
            return relationships;
        }

        private Tuple<List<SdsClass>, List<SdsEnrollment>> GetClassesAndEnrolements()
        {
            List<SdsClass> classes = new List<SdsClass>();
            List<SdsEnrollment> enrollments = new List<SdsEnrollment>();

            string currentSchoolyear = DateTime.Now.Month < 8 ? (DateTime.Now.Year - 1) + "-" + DateTime.Now.Year : DateTime.Now.Year + "-" + (DateTime.Now.Year + 1);

            foreach (Lesgroep lesgroep in vestigingModel.Lesgroepen)
            {
                if (lesgroep.Docenten.Count > 0 && lesgroep.Leerlingen.Count > 0)
                {
                    SdsClass lg = new SdsClass();
                    string sectieNaam = BusinessLogicHelper.GetFilteredName(lesgroep.Naam);

                    lg.title = sectieNaam;
                    lg.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                    lg.sourcedId = (sectieNaam.ToLower().StartsWith(vestigingModel.Vestiging.Afkorting.ToLower()) ? sectieNaam : vestigingModel.Vestiging.Afkorting.ToLower() + sectieNaam) + currentSchoolyear;

                    classes.Add(lg);
                    foreach (var mw in lesgroep.Docenten)
                    {
                        SdsEnrollment er = new SdsEnrollment();
                        er.classSourcedId = lg.sourcedId;
                        er.userSourcedId = mw.ToString();
                        er.role = "teacher";  // https://learn.microsoft.com/en-us/schooldatasync/default-list-of-values#enrollment-roles
                        if (vestigingModel.Medewerkers.Where(m => m.Uuid == mw).FirstOrDefault() != null) //als de docent voorkomt in de medewerkerlijst.
                        {
                            enrollments.Add(er);
                        }
                    }
                    foreach (var ll in lesgroep.Leerlingen)
                    {
                        SdsEnrollment er = new SdsEnrollment();
                        er.classSourcedId = lg.sourcedId;
                        er.userSourcedId = ll.Uuid.ToString();
                        er.role = "student"; // https://learn.microsoft.com/en-us/schooldatasync/default-list-of-values#enrollment-roles
                        if (vestigingModel.Leerlingen.Where(s => s.Uuid == ll.Uuid).FirstOrDefault() != null) //als de leerling voorkomt in de leerlinglijst.
                        {
                            enrollments.Add(er);
                        }
                    }
                }
            }
            return Tuple.Create<List<SdsClass>, List<SdsEnrollment>>(classes, enrollments);
        }


        private List<SdsRole> GetRoles()
        {
            List<SdsRole> result = new List<SdsRole>();
            foreach (Medewerker mw in vestigingModel.Medewerkers)
            {
                SdsRole role = new SdsRole();
                role.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                role.userSourcedId = mw.Uuid.ToString();
                role.role = "staff";
                result.Add(role);
            }

            foreach (Leerling ll in vestigingModel.Leerlingen)
            {
                SdsRole role = new SdsRole();
                role.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                role.userSourcedId = ll.Uuid.ToString();
                role.role = "student";
                result.Add(role);
            }

            foreach (OuderVerzorger ov in vestigingModel.OuderVerzorgers)
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
            return result;
        }

        private List<SdsUser> GetUsers()
        {
            List<SdsUser> result = new List<SdsUser>();
            foreach (Medewerker mw in vestigingModel.Medewerkers)
            {
                SdsUser user = new SdsUser();
                user.username = sh.ReplaceTeacherProperty(SettingsHelper.OutputFormatUsernameTeacher, mw);
                user.sourcedId = mw.Uuid.ToString();
                result.Add(user);

            }

            foreach (Leerling ll in vestigingModel.Leerlingen)
            {
                SdsUser user = new SdsUser();
                user.username = sh.ReplaceStudentProperty(SettingsHelper.OutputFormatUsernameStudent, ll);
                user.sourcedId = ll.Uuid.ToString();
                result.Add(user);
            }

            foreach (OuderVerzorger ov in vestigingModel.OuderVerzorgers)
            {
                if (!string.IsNullOrEmpty(ov.Emailadres))
                {
                    SdsUser user = new SdsUser();
                    user.username = ov.Emailadres;
                    user.sourcedId = ov.Uuid.ToString();
                    user.phone = BusinessLogicHelper.NormaliseerTelefoonnummerNaarE164(ov.Telefoonnummer);
                    result.Add(user);
                }
            }
            return result;
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
