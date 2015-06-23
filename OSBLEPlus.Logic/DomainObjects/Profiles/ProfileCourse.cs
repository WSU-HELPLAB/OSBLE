using System;
using OSBLEPlus.Logic.DomainObjects.Interfaces;

namespace OSBLEPlus.Logic.DomainObjects.Profiles
{
    public class ProfileCourse : IProfileCourse
    {
        public int CourseId { get; set; }
        public int Number { get; set; }
        public string NamePrefix { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public string Semester { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
