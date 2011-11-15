namespace TeamCreation
{
    /// <summary>
    /// This must match the SerializableTeamMembers class that OSBLE has exactly
    /// </summary>
    public class SerializableTeamMember
    {
        public bool isUser { get; set; }

        public int UserID { get; set; }

        public int TeamID { get; set; }

        public string Name { get; set; }

        public int Section { get; set; }

        public bool IsModerator { get; set; }

        public bool Subbmitted { get; set; }

        public int InTeamID { get; set; }

        public string InTeamName { get; set; }
    }
}