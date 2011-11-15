using System.Windows.Controls;
using System.Windows.Media;

namespace TeamCreation
{
    public partial class Member : UserControl
    {
        private SerializableTeamMember serializableMember;

        private Color red1 = Color.FromArgb(255, 121, 90, 90);
        private Color red2 = Color.FromArgb(255, 180, 154, 154);
        private Color white1 = Color.FromArgb(255, 255, 255, 255);
        private Color white2 = Color.FromArgb(255, 140, 140, 140);

        public Color TopColor
        {
            get
            {
                if (serializableMember.Subbmitted)
                {
                    return red1;
                }
                else
                {
                    return white1;
                }
            }
        }

        public Color BottemColor
        {
            get
            {
                if (serializableMember.Subbmitted)
                {
                    return red2;
                }
                else
                {
                    return white2;
                }
            }
        }

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

        public Member(SerializableTeamMember serializableMember)
        {
            // Required to initialize variables
            InitializeComponent();

            this.serializableMember = serializableMember;
            this.DataContext = this;
        }
    }
}