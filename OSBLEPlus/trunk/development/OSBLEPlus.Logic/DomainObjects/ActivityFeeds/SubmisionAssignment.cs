using System;
using OSBLE.Interfaces;
using OSBLE.Models.Assignments;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class SubmisionAssignment : IAssignment
    {
        public int Id { get; set; }
        public AssignmentTypes AssignmentType { get; set; }
        public int CourseId { get; set; }
        public ICourse Course { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime DueDate { get; set; }
    }
}
