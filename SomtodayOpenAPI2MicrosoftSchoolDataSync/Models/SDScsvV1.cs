using CsvHelper;
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
    [CsvRecord]
    public partial class GuardianRelationship
    {
        [DisplayName("SIS ID")]
        [CsvColumn("SIS ID", Index = 0)]
        public string SISid { get; set; }
        [DisplayName("Email")]
        [CsvColumn("Email", Index = 1)]
        public string Email { get; set; }
        [DisplayName("Role")]
        [CsvColumn("Role", Index = 2)]
        public string Role { get; private set; } = "guardian";  // Read-only property, initialized to "guardian"
    }

    [CsvRecord]
    public partial class Guardian
    {
        [DisplayName("Email")]
        [CsvColumn("Email", Index = 0)]
        public string Email { get; set; }
        [DisplayName("First Name")]
        [CsvColumn("First Name", Index = 1)]
        public string FirstName { get; set; }
        [DisplayName("Last Name")]
        [CsvColumn("Last Name", Index = 2)]
        public string LastName { get; set; }
        [DisplayName("Phone")]
        [CsvColumn("Phone", Index = 3)]
        public string Phone { get; set; }
        [DisplayName("SIS ID")]
        [CsvColumn("SIS ID", Index = 4)]
        public string SISid { get; set; }
    }

    [CsvRecord]
    public partial class TeacherRoster
    {
        [DisplayName("Section SIS ID")]
        [CsvColumn("Section SIS ID", Index = 0)]
        public string SISSectionid { get; set; }
        [DisplayName("SIS ID")]
        [CsvColumn("SIS ID", Index = 1)]
        public string SISTeacherid { get; set; }
    }

    [CsvRecord]
    public partial class StudentEnrollment
    {
        [CsvColumn("Section SIS ID", Index = 0)]
        public string SISSectionid { get; set; }
        [CsvColumn("SIS ID", Index = 1)]
        public string SISStudentid { get; set; }
    }

    [CsvRecord]
    public partial class Teacher
    {
        [CsvColumn("SIS ID", Index = 0)]
        public string SISid { get; set; }
        [CsvColumn("School SIS ID", Index = 1)]
        public string SISSchoolid { get; set; }
        [CsvColumn("Username", Index = 2)]
        public string Username { get; set; }
        [CsvIgnore]
        public string Firstname { get; set; }
        [CsvIgnore]
        public string Lastname { get; set; }
    }

    [CsvRecord]
    public partial class Student
    {
        [CsvColumn("SIS ID", Index = 0)]
        public string SISid { get; set; }
        [CsvColumn("School SIS ID", Index = 1)]
        public string SISSchoolid { get; set; }
        [CsvColumn("Username", Index = 2)]
        public string Username { get; set; }
        [CsvIgnore]
        public string Firstname { get; set; }
        [CsvIgnore]
        public string Lastname { get; set; }
    }

    [CsvRecord]
    public partial class Section
    {
        [CsvColumn("SIS ID", Index = 0)]
        public string SISid { get; set; }
        [CsvColumn("School SIS ID", Index = 1)]
        public string SISSchoolid { get; set; }
        [CsvColumn("Section Name", Index = 2)]
        public string Name { get; set; }
        [CsvColumn("Section Number", Index = 3)]
        public string Number { get; set; }
        [CsvColumn("Course Name", Index = 4)]
        public string CourseName { get; set; }
        [CsvColumn("Course Description", Index = 5)]
        public string CourseDescription { get; set; }

    }

    [CsvRecord]
    public partial class School
    {
        [CsvColumn("SIS ID", Index = 0)]
        public string SISid { get; set; }
        [CsvColumn("Name", Index = 1)]
        public string Name { get; set; }
    }
}
