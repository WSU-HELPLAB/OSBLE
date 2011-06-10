namespace TeamCreation
{
    public class SerializableTeamMembers
    {
        public int UserID { get; set; }

        public int TeamID { get; set; }

        public string Name { get; set; }

        public int Section { get; set; }

        public bool IsModerator { get; set; }

        public bool Subbmitted { get; set; }

        public int InTeamID { get; set; }

        public string InTeamName { get; set; }

        public int currentGrade { get; set; }
    }
}