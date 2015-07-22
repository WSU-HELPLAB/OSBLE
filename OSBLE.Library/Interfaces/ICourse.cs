using System;

namespace OSBLE.Interfaces
{
    public interface ICourse
    {
        int Id { get; set; }
        int Number { get; set; }
        string NamePrefix { get; set; }
        string Description { get; set; }
        string Name { get; }
        string Semester { get; }
        int Year { get; }
        DateTime StartDate { get; }
        DateTime EndDate { get; }
    }
}
