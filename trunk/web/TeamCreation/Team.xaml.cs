using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace TeamCreation
{
    public partial class Team : UserControl
    {
        private static int i = 0;

        private ObservableCollection<Member> memberList = new ObservableCollection<Member>();

        public ObservableCollection<Member> MemberList
        {
            get { return memberList; }
            set { memberList = value; }
        }

        public Team()
        {
            // Required to initialize variables
            InitializeComponent();

            this.TeamName.Text = "Team" + i.ToString();
            i++;
            this.Members.ItemsSource = memberList;
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Dropped(object sender, ItemDragEventArgs e)
        {
        }

        private void GroupName_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}