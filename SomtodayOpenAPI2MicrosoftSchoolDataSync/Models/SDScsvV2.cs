using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Models
{
    internal class SDScsvV2
    {

        // https://learn.microsoft.com/en-us/schooldatasync/sds-v2.1-csv-file-format
        public List<SdsClass> classes { get; set; }          //optional/required
        public List<SdsEnrollment> enrollments { get; set; }  //optional/required
        public List<SdsOrganization> orgs { get; set; }    //required
        public List<SdsRelationship> relationships { get; set; }  //optional/required when syncing guardians
        public List<SdsRole> roles { get; set; }  //required
        public List<SdsUser> users { get; set; }  //required

//public List<courses> courses { get; set; }          //optional (If your classes.csv data contains links to courses, the corresponding data should be provided to avoid error messages when processing data.) 

    }

    /*
    public class academicSessions
    {
        public string sourcedId { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public int schoolYear { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
    }

    public class academicSessionsClassMap : ClassMap<academicSessions>
    {
        public academicSessionsClassMap()
        {
            Map(m => m.sourcedId).Name("sourcedId");
            Map(m => m.title).Name("title");
            Map(m => m.type).Name("type");
            Map(m => m.schoolYear).Name("schoolYear");
            Map(m => m.startDate).Name("startDate");
            Map(m => m.endDate).Name("endDate");
        }
    }
    */

    public class SdsClass
    {
        public string sourcedId { get; set; }
        public string orgSourcedId { get; set; }
        public string title { get; set; }
        public string sessionSourcedIds { get; set; }
        public string courseSourcedId { get; set; }
    }

    public class SdsClassMap : ClassMap<SdsClass>
    {
        public SdsClassMap()
        {
            Map(m => m.sourcedId).Name("sourcedId");
            Map(m => m.orgSourcedId).Name("orgSourcedId");
            Map(m => m.title).Name("title");
            Map(m => m.sessionSourcedIds).Name("sessionSourcedIds");
            Map(m => m.courseSourcedId).Name("courseSourcedId");
        }
    }

    public class SdsCourse
    {
        public string sourcedId { get; set; }
        public int orgSourcedId { get; set; }
        public string title { get; set; }
        public string schoolYearSourcedId { get; set; }
        public string code { get; set; }
        public int? subject { get; set; }
        public string grade { get; set; }
    }

    public class SdsCourseMap : ClassMap<SdsCourse>
    {
        public SdsCourseMap()
        {
            Map(m => m.sourcedId).Name("sourcedId");
            Map(m => m.orgSourcedId).Name("orgSourcedId");
            Map(m => m.title).Name("title");
            Map(m => m.schoolYearSourcedId).Name("schoolYearSourcedId");
            Map(m => m.code).Name("code");
            Map(m => m.subject).Name("subject");
            Map(m => m.grade).Name("grade");
        }
    }

    /*
    public class demographics
    {
        public int userSourcedId { get; set; }
        public string sex { get; set; }
        public DateTime birthDate { get; set; }
        public string birthCity { get; set; }
        public string birthState { get; set; }
        public string birthCountry { get; set; }
        public string ethnicityCodes { get; set; }
        public string raceCodes { get; set; }
    }

    public class demographicsClassMap : ClassMap<demographics>
    {
        public demographicsClassMap()
        {
            Map(m => m.userSourcedId).Name("userSourcedId");
            Map(m => m.sex).Name("sex");
            Map(m => m.birthDate).Name("birthDate");
            Map(m => m.birthCity).Name("birthCity");
            Map(m => m.birthState).Name("birthState");
            Map(m => m.birthCountry).Name("birthCountry");
            Map(m => m.ethnicityCodes).Name("ethnicityCodes");
            Map(m => m.raceCodes).Name("raceCodes");
        }
    }
    */

    public class SdsEnrollment
    {
        public string classSourcedId { get; set; }
        public string userSourcedId { get; set; }
        public string role { get; set; }
    }

    public class SdsEnrollmentMap : ClassMap<SdsEnrollment>
    {
        public SdsEnrollmentMap()
        {
            Map(m => m.classSourcedId).Name("classSourcedId");
            Map(m => m.userSourcedId).Name("userSourcedId");
            Map(m => m.role).Name("role");
        }
    }

    public class SdsOrganization
    {
        public string sourcedId { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public int? parentSourcedId { get; set; }

        /*public enum type
        {
            school,
            department,
            district,
            local,
            state,
            national,
            departmentOfEducation,
            ministryOfEducation,
            university,
            college,
            campus,
            adultEducation,
            municipality,
            academicTrust,
            localAuthority,
            region,
            division,
            province,
            researchCenter,
            program
        }*/
    }

    public class SdsOrganizationMap : ClassMap<SdsOrganization>
    {
        public SdsOrganizationMap()
        {
            Map(m => m.sourcedId).Name("sourcedId");
            Map(m => m.name).Name("name");
            Map(m => m.type).Name("type");
            Map(m => m.parentSourcedId).Name("parentSourcedId");
        }
    }

    public class SdsRelationship
    {
        public string userSourcedId { get; set; }
        public string relationshipUserSourcedId { get; set; }
        public string relationshipRole { get; set; }
    }

    public class SdsRelationshipMap : ClassMap<SdsRelationship>
    {
        public SdsRelationshipMap()
        {
            Map(m => m.userSourcedId).Name("userSourcedId");
            Map(m => m.relationshipUserSourcedId).Name("relationshipUserSourcedId");
            Map(m => m.relationshipRole).Name("relationshipRole");
        }
    }

    public class SdsRole
    {
        public string userSourcedId { get; set; }
        public string orgSourcedId { get; set; }
        public string role { get; set; }
        //public string sessionSourcedId { get; set; }
        //public string grade { get; set; }
        //public bool isPrimary { get; set; }
        //public DateTime roleStartDate { get; set; }
        //public DateTime roleEndDate { get; set; }
    }

    public class SdsRoleMap : ClassMap<SdsRole>
    {
        public SdsRoleMap()
        {
            Map(m => m.userSourcedId).Name("userSourcedId");
            Map(m => m.orgSourcedId).Name("orgSourcedId");
            Map(m => m.role).Name("role");
            //Map(m => m.sessionSourcedId).Name("sessionSourcedId");
            //Map(m => m.grade).Name("grade");
            //Map(m => m.isPrimary).Name("isPrimary");
            //Map(m => m.roleStartDate).Name("roleStartDate");
            //Map(m => m.roleEndDate).Name("roleEndDate");
        }
    }

    /*
    public class userFlags
    {
        public int userSourcedId { get; set; }
        public string flag { get; set; }
    }

    public class userFlagsClassMap : ClassMap<userFlags>
    {
        public userFlagsClassMap()
        {
            Map(m => m.userSourcedId).Name("userSourcedId");
            Map(m => m.flag).Name("flag");
        }
    }
    */

    public class SdsUser
    {
        public string sourcedId { get; set; }
        public string username { get; set; }
        public string givenName { get; set; }
        public string familyName { get; set; }
        public string password { get; set; }
        public string activeDirectoryMatchId { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string sms { get; set; }
    }

    public class SdsUserMap : ClassMap<SdsUser>
    {
        public SdsUserMap()
        {
            Map(m => m.sourcedId).Name("sourcedId");
            Map(m => m.username).Name("username");
            Map(m => m.givenName).Name("givenName");
            Map(m => m.familyName).Name("familyName");
            Map(m => m.password).Name("password");
            Map(m => m.activeDirectoryMatchId).Name("activeDirectoryMatchId");
            Map(m => m.email).Name("email");
            Map(m => m.phone).Name("phone");
            Map(m => m.sms).Name("sms");
        }
    }


}
