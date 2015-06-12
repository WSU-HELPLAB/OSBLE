namespace OSBLEPlus.Logic.DomainObjects.Interfaces
{
    public interface ICourse
    {
        int CourseId { get; set; }
        int Number { get; set; }
        string NamePrefix { get; set; }
        string Description { get; set; }
        string Name { get; }
    }
}
