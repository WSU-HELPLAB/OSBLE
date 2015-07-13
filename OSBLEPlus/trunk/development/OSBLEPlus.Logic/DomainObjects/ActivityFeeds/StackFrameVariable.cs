namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class StackFrameVariable
    {
        public int Id { get; set; }

        public int StackFrameId { get; set; }

        public virtual StackFrame StackFrame { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}
