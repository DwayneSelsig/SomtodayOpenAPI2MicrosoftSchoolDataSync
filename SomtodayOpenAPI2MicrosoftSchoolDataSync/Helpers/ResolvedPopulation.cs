using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    /// <summary>
    /// The single source of truth for the people and classes that may be exported for a location.
    /// Source group membership is not sufficient: every member must exist in the downloaded
    /// location population before it can make a class exportable.
    /// </summary>
    internal sealed class ResolvedPopulation
    {
        public List<ResolvedClass> Classes { get; } = new List<ResolvedClass>();

        public HashSet<Guid> TeacherIds { get; } = new HashSet<Guid>();

        public HashSet<Guid> StudentIds { get; } = new HashSet<Guid>();

        public static ResolvedPopulation Create(VestigingModel location)
        {
            ResolvedPopulation result = new ResolvedPopulation();
            Dictionary<Guid, Medewerker> teachers = (location.Medewerkers ?? new List<Medewerker>())
                .GroupBy(teacher => teacher.Uuid)
                .ToDictionary(group => group.Key, group => group.First());
            Dictionary<Guid, Leerling> students = (location.Leerlingen ?? new List<Leerling>())
                .GroupBy(student => student.Uuid)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (Lesgroep group in location.Lesgroepen ?? new List<Lesgroep>())
            {
                if (string.IsNullOrWhiteSpace(group.Naam))
                {
                    continue;
                }

                List<Medewerker> resolvedTeachers = (group.Docenten ?? Array.Empty<Guid>())
                    .Distinct()
                    .Where(teachers.ContainsKey)
                    .Select(id => teachers[id])
                    .ToList();
                List<Leerling> resolvedStudents = (group.Leerlingen ?? Array.Empty<LeerlingVestiging>())
                    .Select(student => student.Uuid)
                    .Distinct()
                    .Where(students.ContainsKey)
                    .Select(id => students[id])
                    .ToList();

                if (resolvedTeachers.Count == 0 || resolvedStudents.Count == 0)
                {
                    continue;
                }

                result.Classes.Add(new ResolvedClass(group, BusinessLogicHelper.GetFilteredName(group.Naam), resolvedTeachers, resolvedStudents));
                result.TeacherIds.UnionWith(resolvedTeachers.Select(teacher => teacher.Uuid));
                result.StudentIds.UnionWith(resolvedStudents.Select(student => student.Uuid));
            }

            return result;
        }
    }

    internal sealed class ResolvedClass
    {
        public ResolvedClass(Lesgroep group, string filteredName, List<Medewerker> teachers, List<Leerling> students)
        {
            Group = group;
            FilteredName = filteredName;
            Teachers = teachers;
            Students = students;
        }

        public Lesgroep Group { get; }
        public string FilteredName { get; }
        public List<Medewerker> Teachers { get; }
        public List<Leerling> Students { get; }
    }
}
