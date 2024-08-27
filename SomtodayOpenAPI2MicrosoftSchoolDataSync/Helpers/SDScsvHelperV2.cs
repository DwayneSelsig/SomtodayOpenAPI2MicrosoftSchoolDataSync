using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using SomtodayOpenAPI2MicrosoftSchoolDataSyncV2.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Instrumentation;
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


            Tuple<List<classes>, List<enrollments>> classesInfo = GetClassesAndEnrolements();
            result.classes = classesInfo.Item1;
            result.enrollments = classesInfo.Item2;

            result.relationships = GetRelationships();


            return result;
        }

        private List<relationships> GetRelationships()
        {
            List<relationships> relationships = new List<relationships>();

            foreach (OuderVerzorger ouder in vestigingModel.OuderVerzorgers)
            {
                foreach (Guid leerling in ouder.Leerlingen_van_vestiging)
                {
                    //Heeft deze ouder een gekoppelde leerling?
                    var leerlingModel = vestigingModel.Leerlingen.Where(s => s.Uuid == leerling).FirstOrDefault();

                    if (leerlingModel != null && !string.IsNullOrEmpty(ouder.Emailadres))
                    {
                        relationships rel = new relationships();
                        rel.userSourcedId = leerling.ToString();
                        rel.relationshipUserSourcedId = ouder.Uuid.ToString();
                        rel.relationshipRole = "guardian"; // https://learn.microsoft.com/en-us/schooldatasync/default-list-of-values#contact-relationship-roles
                        relationships.Add(rel);
                    }
                }
            }
            return relationships;
        }

        private Tuple<List<classes>, List<enrollments>> GetClassesAndEnrolements()
        {
            List<classes> classes = new List<classes>();
            List<enrollments> enrollments = new List<enrollments>();

            string currentSchoolyear = DateTime.Now.Month < 8 ? (DateTime.Now.Year - 1) + "-" + DateTime.Now.Year : DateTime.Now.Year + "-" + (DateTime.Now.Year + 1);

            foreach (Lesgroep lesgroep in vestigingModel.Lesgroepen)
            {
                if (lesgroep.Docenten.Count > 0 && lesgroep.Leerlingen.Count > 0)
                {
                    classes lg = new classes();
                    string sectieNaam = GetFilteredName(lesgroep.Naam);

                    lg.title = sectieNaam;
                    lg.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                    lg.sourcedId = (sectieNaam.ToLower().StartsWith(vestigingModel.Vestiging.Afkorting.ToLower()) ? sectieNaam : vestigingModel.Vestiging.Afkorting.ToLower() + sectieNaam) + currentSchoolyear;

                    classes.Add(lg);
                    foreach (var mw in lesgroep.Docenten)
                    {
                        enrollments er = new enrollments();
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
                        enrollments er = new enrollments();
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
            return Tuple.Create<List<classes>, List<enrollments>>(classes, enrollments);
        }


        private List<roles> GetRoles()
        {
            List<roles> result = new List<roles>();
            foreach (Medewerker mw in vestigingModel.Medewerkers)
            {
                roles role = new roles();
                role.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                role.userSourcedId = mw.Uuid.ToString();
                role.role = "staff";
                result.Add(role);
            }

            foreach (Leerling ll in vestigingModel.Leerlingen)
            {
                roles role = new roles();
                role.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                role.userSourcedId = ll.Uuid.ToString();
                role.role = "student";
                result.Add(role);
            }

            foreach (OuderVerzorger ov in vestigingModel.OuderVerzorgers)
            {
                if (!string.IsNullOrEmpty(ov.Emailadres))
                {
                    roles role = new roles();
                    role.orgSourcedId = vestigingModel.Vestiging.Uuid.ToString();
                    role.userSourcedId = ov.Uuid.ToString();
                    role.role = "other";
                    result.Add(role);
                }
            }
            return result;
        }

        private List<users> GetUsers()
        {
            List<users> result = new List<users>();
            foreach (Medewerker mw in vestigingModel.Medewerkers)
            {
                users user = new users();
                user.username = sh.ReplaceTeacherProperty(SettingsHelper.OutputFormatUsernameTeacher, mw);
                user.sourcedId = mw.Uuid.ToString();
                result.Add(user);

            }

            foreach (Leerling ll in vestigingModel.Leerlingen)
            {
                users user = new users();
                user.username = sh.ReplaceStudentProperty(SettingsHelper.OutputFormatUsernameStudent, ll);
                user.sourcedId = ll.Uuid.ToString();
                result.Add(user);
            }

            foreach (OuderVerzorger ov in vestigingModel.OuderVerzorgers)
            {
                if (!string.IsNullOrEmpty(ov.Emailadres))
                {
                    users user = new users();
                    user.username = ov.Emailadres;
                    user.sourcedId = ov.Uuid.ToString();
                    user.phone = BusinessLogicHelper.NormaliseerTelefoonnummerNaarE164(ov.Telefoonnummer);
                    result.Add(user);
                }
            }
            return result;
        }

        private List<orgs> GetOrgs()
        {
            List<orgs> result = new List<orgs>();
            orgs _org = new orgs();
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
