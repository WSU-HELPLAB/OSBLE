using System;
namespace OSBLE.Models.ViewModels
{
    /// <summary>
    /// This must match the SerializableTeamMembers class that the TeamCreation (silverlight tool) has exactly
    /// </summary>
    public class SerializableTeamMember : IComparable
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

        public int CompareTo(object obj)
        {
            SerializableTeamMember other = obj as SerializableTeamMember;
            if (other == null)
            {
                return -1;
            }

            if (isUser != other.isUser)
            {
                return -1;
            }

            if (UserID != other.UserID)
            {
                return -1;
            }

            if (TeamID != other.TeamID)
            {
                return -1;
            }

            if (Name.CompareTo(other.Name) != 0)
            {
                Name.CompareTo(other.Name);
            }

            if (Section != other.Section)
            {
                return -1;
            }

            if (IsModerator != other.IsModerator)
            {
                return -1;
            }

            if (Subbmitted != other.Subbmitted)
            {
                return -1;
            }

            if (InTeamID != other.InTeamID)
            {
                return -1;
            }

            return 0;
        }
    }
}