using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TeamCreation
{
    public partial class Team : UserControl
    {
        public event EventHandler NameChanged = delegate { };
        public event EventHandler DeleteRequest = delegate { };
        public event EventHandler MemberListChanged = delegate { };

        private ObservableCollection<Member> memberList = new ObservableCollection<Member>();

        public bool IsValidTeamName
        {
            get;
            set;
        }

        public ObservableCollection<Member> MemberList
        {
            get { return memberList; }
            set { memberList = value; }
        }

        public Team()
        {
            LocalInitializer("");
        }

        public Team(string teamName)
        {
            LocalInitializer(teamName);
        }

        private void LocalInitializer(string teamName)
        {
            // Required to initialize variables
            InitializeComponent();

            this.TeamName.Text = teamName;
            this.Members.ItemsSource = memberList;

            memberList.CollectionChanged += new NotifyCollectionChangedEventHandler(memberList_CollectionChanged);
        }

        private void memberList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Member item in e.NewItems)
                {
                    item.InTeamName = TeamName.Text;
                }
            }
            MemberListChanged(this, EventArgs.Empty);
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            if (MemberList.Count > 0)
            {
                var r = MessageBox.Show("Are you sure that you want to delete this team?", "Delete Team", MessageBoxButton.OKCancel);
                if (r == MessageBoxResult.OK)
                {
                    DeleteRequest(this, EventArgs.Empty);
                }
            }
            else
            {
                DeleteRequest(this, EventArgs.Empty);
            }
        }

        private void GroupName_TextChanged(object sender, TextChangedEventArgs e)
        {
            NameChanged(this, EventArgs.Empty);
        }

        public void NameChangedValid(bool isValid)
        {
            IsValidTeamName = isValid;

            if (isValid)
            {
                TeamName.Background = new SolidColorBrush(Colors.White);
                TeamName.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                MessageBox.Show("The team name " + TeamName.Text + " is not valid and will not be saved please change it");
                TeamName.Background = new SolidColorBrush(Colors.Red);
                TeamName.Foreground = new SolidColorBrush(Colors.White);
            }

            if (isValid == true)
            {
                foreach (Member member in memberList)
                {
                    member.InTeamName = TeamName.Text;
                }
            }
        }
    }
}