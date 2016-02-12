using System;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public class StateMachineEvent
    {
        public int SenderId { get; set; }
        public DateTime EventDate { get; set; }
        public string SolutionName { get; set; }
        public string LogType { get; set; }
        public int? BuildErrorLogId { get; set; }
        public int? ExecutionActionId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Identification { get; set; }
        public string MarkerType { get; set; }
    }
}
