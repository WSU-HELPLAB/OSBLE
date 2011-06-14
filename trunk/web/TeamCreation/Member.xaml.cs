using System.Windows.Controls;
using System.Windows.Media;

namespace TeamCreation
{
    public partial class Member : UserControl
    {
        private SerializableTeamMembers serializableMember;

        public string MembersName
        {
            get
            {
                return serializableMember.Name;
            }
        }

        public bool AssignmentSubmitted
        {
            get
            {
                return serializableMember.Subbmitted;
            }
        }

        public bool IsModerator
        {
            get
            {
                return serializableMember.IsModerator;
            }
        }

        public string InTeamName
        {
            get
            {
                return serializableMember.InTeamName;
            }
            set
            {
                serializableMember.InTeamName = value;
            }
        }

        public Member(SerializableTeamMembers serializableMember)
        {
            // Required to initialize variables
            InitializeComponent();

            this.serializableMember = serializableMember;
            this.DataContext = this;

            if (serializableMember.IsModerator)
            {
                //is moderator
                this.border.Background = new SolidColorBrush(Colors.Blue);
            }
            else if (serializableMember.Subbmitted == false)
            {
                //student didn't submit
                this.border.Background = new SolidColorBrush(Colors.Red);
            }
            else
            {
                //student and submitted
                this.border.Background = new SolidColorBrush(Colors.White);
            }
        }
    }
}