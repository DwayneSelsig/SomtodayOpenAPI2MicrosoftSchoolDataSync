using SomtodayOpenAPI2MicrosoftSchoolDataSync;
using SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers;
using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Tests;

public class PopulationExportTests
{
    [Fact]
    public async Task Authentication_request_encodes_credentials_as_form_data()
    {
        using var content = OpenAPIHelper.CreateAuthenticationContent("client id", "secret&value");

        string body = await content.ReadAsStringAsync();

        Assert.Equal("grant_type=client_credentials&client_id=client+id&client_secret=secret%26value", body);
    }

    [Fact]
    public void Unresolved_source_members_do_not_create_a_class_or_location()
    {
        VestigingModel location = CreateLocation(new Lesgroep
        {
            Uuid = Guid.NewGuid(), Naam = "LOC-wiskunde",
            Docenten = new[] { Guid.NewGuid() },
            Leerlingen = new[] { new LeerlingVestiging { Uuid = Guid.NewGuid() } }
        });

        SDScsvV1 v1 = new SDScsvHelperV1(location).ConvertToSDSCSV();
        SDScsvV2 v2 = new SDScsvHelperV2(location).ConvertToSDSCSV();

        Assert.Empty(v1.Schools); Assert.Empty(v1.Sections); Assert.Empty(v1.Teachers); Assert.Empty(v1.Students);
        Assert.Empty(v2.orgs); Assert.Empty(v2.classes); Assert.Empty(v2.users); Assert.Empty(v2.roles);
    }

    [Fact]
    public void Only_resolved_members_of_exportable_groups_are_emitted_in_both_versions()
    {
        Guid teacherId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid excludedTeacherId = Guid.NewGuid();
        Guid excludedStudentId = Guid.NewGuid();
        VestigingModel location = CreateLocation(
            new Lesgroep
            {
                Uuid = Guid.NewGuid(), Naam = "LOC-wiskunde",
                Docenten = new[] { teacherId, Guid.NewGuid() },
                Leerlingen = new[] { new LeerlingVestiging { Uuid = studentId }, new LeerlingVestiging { Uuid = Guid.NewGuid() } }
            },
            new Lesgroep
            {
                Uuid = Guid.NewGuid(), Naam = "alleen-bronleden",
                Docenten = new[] { excludedTeacherId },
                Leerlingen = new[] { new LeerlingVestiging { Uuid = Guid.NewGuid() } }
            },
            new Medewerker { Uuid = teacherId, Emailadres = "teacher@example.test" },
            new Medewerker { Uuid = excludedTeacherId, Emailadres = "excluded-teacher@example.test" },
            new Leerling { Uuid = studentId, Emailadres = "student@example.test" },
            new Leerling { Uuid = excludedStudentId, Emailadres = "excluded-student@example.test" });

        SDScsvV1 v1 = new SDScsvHelperV1(location).ConvertToSDSCSV();
        SDScsvV2 v2 = new SDScsvHelperV2(location).ConvertToSDSCSV();

        Assert.Single(v1.Sections); Assert.Single(v1.Teachers); Assert.Single(v1.Students);
        Assert.Equal(teacherId.ToString(), v1.Teachers[0].SISid); Assert.Equal(studentId.ToString(), v1.Students[0].SISid);
        Assert.Single(v2.classes); Assert.Single(v2.users, user => user.sourcedId == teacherId.ToString());
        Assert.Single(v2.users, user => user.sourcedId == studentId.ToString());
        Assert.DoesNotContain(v2.users, user => user.sourcedId == excludedTeacherId.ToString() || user.sourcedId == excludedStudentId.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Invalid_class_names_are_ignored_without_an_exception(string name)
    {
        Guid teacherId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        VestigingModel location = CreateLocation(
            new Lesgroep { Uuid = Guid.NewGuid(), Naam = name, Docenten = new[] { teacherId }, Leerlingen = new[] { new LeerlingVestiging { Uuid = studentId } } },
            new Medewerker { Uuid = teacherId, Emailadres = "teacher@example.test" },
            new Leerling { Uuid = studentId, Emailadres = "student@example.test" });

        Assert.Empty(new SDScsvHelperV1(location).ConvertToSDSCSV().Sections);
        Assert.Empty(new SDScsvHelperV2(location).ConvertToSDSCSV().classes);
    }

    [Fact]
    public void Guardians_are_limited_to_included_students()
    {
        Guid teacherId = Guid.NewGuid();
        Guid includedStudentId = Guid.NewGuid();
        Guid excludedStudentId = Guid.NewGuid();
        Guid guardianId = Guid.NewGuid();
        VestigingModel location = CreateLocation(
            new[] { new Lesgroep { Uuid = Guid.NewGuid(), Naam = "LOC-biologie", Docenten = new[] { teacherId }, Leerlingen = new[] { new LeerlingVestiging { Uuid = includedStudentId } } } },
            new[] { new Medewerker { Uuid = teacherId, Emailadres = "teacher@example.test" } },
            new[] { new Leerling { Uuid = includedStudentId, Emailadres = "included@example.test" }, new Leerling { Uuid = excludedStudentId, Emailadres = "excluded@example.test" } });
        location.OuderVerzorgers.Add(new OuderVerzorger
        {
            Uuid = guardianId, Emailadres = "guardian@example.test", Achternaam = "Guardian",
            Leerlingen_van_vestiging = new[] { includedStudentId, excludedStudentId }
        });

        SDScsvV1 v1 = new SDScsvHelperV1(location).ConvertToSDSCSV();
        SDScsvV2 v2 = new SDScsvHelperV2(location).ConvertToSDSCSV();

        Assert.Single(v1.User); Assert.Single(v1.Guardianrelationship); Assert.Equal(includedStudentId.ToString(), v1.Guardianrelationship[0].SISid);
        Assert.Single(v2.users, user => user.sourcedId == guardianId.ToString()); Assert.Single(v2.relationships); Assert.Equal(includedStudentId.ToString(), v2.relationships[0].userSourcedId);
    }

    [Fact]
    public void Merged_v2_output_deduplicates_users_and_exact_relationship_rows()
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        SDScsvV2 rowSet = new SDScsvV2
        {
            orgs = new List<SdsOrganization>(), classes = new List<SdsClass>(), enrollments = new List<SdsEnrollment>(), roles = new List<SdsRole>(),
            users = new List<SdsUser> { new SdsUser { sourcedId = "user" } },
            relationships = new List<SdsRelationship> { new SdsRelationship { userSourcedId = "student", relationshipUserSourcedId = "user", relationshipRole = "guardian" } }
        };
        new FileHelper().SaveV2ToDisk(new List<SDScsvV2> { rowSet, rowSet }, folder + Path.DirectorySeparatorChar);

        Assert.Equal(2, File.ReadAllLines(Path.Combine(folder, "users.csv")).Length);
        Assert.Equal(2, File.ReadAllLines(Path.Combine(folder, "relationships.csv")).Length);
        Directory.Delete(folder, recursive: true);
    }

    private static VestigingModel CreateLocation(Lesgroep group, Medewerker teacher = null, Leerling student = null)
        => CreateLocation(new[] { group }, teacher == null ? Array.Empty<Medewerker>() : new[] { teacher }, student == null ? Array.Empty<Leerling>() : new[] { student });

    private static VestigingModel CreateLocation(Lesgroep first, Lesgroep second, Medewerker firstTeacher, Medewerker secondTeacher, Leerling firstStudent, Leerling secondStudent)
        => CreateLocation(new[] { first, second }, new[] { firstTeacher, secondTeacher }, new[] { firstStudent, secondStudent });

    private static VestigingModel CreateLocation(IEnumerable<Lesgroep> groups, IEnumerable<Medewerker> teachers, IEnumerable<Leerling> students)
        => new VestigingModel
        {
            Vestiging = new Vestiging { Uuid = Guid.NewGuid(), Naam = "Locatie", Afkorting = "LOC" },
            Lesgroepen = groups.ToList(), Medewerkers = teachers.ToList(), Leerlingen = students.ToList(), OuderVerzorgers = new List<OuderVerzorger>()
        };
}
