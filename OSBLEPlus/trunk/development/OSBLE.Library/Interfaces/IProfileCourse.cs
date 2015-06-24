using System;

namespace OSBLE.Interfaces
{
    public interface IProfileCourse
    {
        int CourseId { get; set; }
        int Number { get; set; }
        string NamePrefix { get; set; }
        string Description { get; set; }
        string Name { get; set; }
        string Semester { get; set; }
        int Year { get; set; }
        DateTime StartDate { get; set; }
        DateTime EndDate { get; set;  }
    }
}
