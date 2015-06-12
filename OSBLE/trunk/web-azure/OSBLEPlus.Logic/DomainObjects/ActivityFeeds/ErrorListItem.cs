namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class ErrorListItem
    {
        public int Id { get; set; }

        public int Column { get; set; }

        public int Line { get; set; }

        public string File { get; set; }

        public string Project { get; set; }

        public string Description { get; set; }
    }
}
