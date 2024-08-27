using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Models
{
    class SDScsvV1
    {
        // https://learn.microsoft.com/en-us/schooldatasync/sds-v1-csv-file-format
        public List<School> Schools { get; set; }
        public List<Section> Sections { get; set; }
        public List<Student> Students { get; set; }
        public List<Teacher> Teachers { get; set; }
        public List<StudentEnrollment> StudentEnrollments { get; set; }
        public List<TeacherRoster> TeacherRosters { get; set; }
        public List<Guardian> User { get; set; }  // ouders/guardians
        public List<GuardianRelationship> Guardianrelationship { get; set; }

    }
    public class GuardianRelationship
    {
        [DisplayName("SIS ID")]
        public string SISid { get; set; }
        [DisplayName("Email")]
        public string Email { get; set; }
        [DisplayName("Role")]
        public string Role { get; } = "guardian";  // Read-only property, initialized to "guardian"
    }
    public sealed class GuardianRelationshipCSVMap : ClassMap<GuardianRelationship>
    {
        public GuardianRelationshipCSVMap()
        {
            //AutoMap();
            Map(m => m.SISid).Name("SIS ID");
            Map(m => m.Email).Name("Email");
            Map(m => m.Role).Name("Role");
        }
    }


    public class Guardian
    {
        [DisplayName("Email")]
        public string Email { get; set; }
        [DisplayName("First Name")]
        public string FirstName { get; set; }
        [DisplayName("Last Name")]
        public string LastName { get; set; }
        [DisplayName("Phone")]
        public string Phone { get; set; }
        [DisplayName("SIS ID")]
        public string SISid { get; set; }
    }
    public sealed class GuardianCSVMap : ClassMap<Guardian>
    {
        public GuardianCSVMap()
        {
            //AutoMap();
            Map(m => m.Email).Name("Email");
            Map(m => m.FirstName).Name("First Name");
            Map(m => m.LastName).Name("Last Name");
            Map(m => m.Phone).Name("Phone");
            Map(m => m.SISid).Name("SIS ID");
        }
    }


    public class TeacherRoster
    {
        [DisplayName("Section SIS ID")]
        public string SISSectionid { get; set; }
        [DisplayName("SIS ID")]
        public string SISTeacherid { get; set; }
    }
    public sealed class TeacherRosterCSVMap : ClassMap<TeacherRoster>
    {
        public TeacherRosterCSVMap()
        {
            //AutoMap();
            Map(m => m.SISSectionid).Name("Section SIS ID");
            Map(m => m.SISTeacherid).Name("SIS ID");
        }
    }


    public class StudentEnrollment
    {
        public string SISSectionid { get; set; }
        public string SISStudentid { get; set; }
    }
    public sealed class StudentEnrollmentCSVMap : ClassMap<StudentEnrollment>
    {
        public StudentEnrollmentCSVMap()
        {
            //AutoMap();
            Map(m => m.SISSectionid).Name("Section SIS ID");
            Map(m => m.SISStudentid).Name("SIS ID");
        }
    }


    public class Teacher
    {
        public string SISid { get; set; }
        public string SISSchoolid { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }
    public sealed class TeacherCSVMap : ClassMap<Teacher>
    {
        public TeacherCSVMap()
        {
            //AutoMap();
            Map(m => m.SISid).Name("SIS ID");
            Map(m => m.SISSchoolid).Name("School SIS ID");
            Map(m => m.Username).Name("Username");
        }
    }


    public class Student
    {
        public string SISid { get; set; }
        public string SISSchoolid { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }
    public sealed class StudentCSVMap : ClassMap<Student>
    {
        public StudentCSVMap()
        {
            //AutoMap();
            Map(m => m.SISid).Name("SIS ID");
            Map(m => m.SISSchoolid).Name("School SIS ID");
            Map(m => m.Username).Name("Username");
        }
    }


    public class Section
    {
        public string SISid { get; set; }
        public string SISSchoolid { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public string CourseName { get; set; }
        public string CourseDescription { get; set; }

    }
    public sealed class SectionCSVMap : ClassMap<Section>
    {
        public SectionCSVMap()
        {
            //AutoMap();
            Map(m => m.SISid).Name("SIS ID");
            Map(m => m.SISSchoolid).Name("School SIS ID");
            Map(m => m.Name).Name("Section Name");
            Map(m => m.Number).Name("Section Number");
            Map(m => m.CourseName).Name("Course Name");
            Map(m => m.CourseDescription).Name("Course Description");
        }
    }
    public class School
    {
        public string SISid { get; set; }
        public string Name { get; set; }
    }
    public sealed class SchoolCSVMap : ClassMap<School>
    {
        public SchoolCSVMap()
        {
            //AutoMap();
            Map(m => m.SISid).Name("SIS ID");
            Map(m => m.Name).Name("Name");
        }
    }
}
