using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace TeamCreation
{
    public partial class Team : UserControl
    {
        public event EventHandler NameChanged = delegate { };

        private static int i = 0;

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
            // Required to initialize variables
            InitializeComponent();

            this.TeamName.Text = "Team" + i.ToString();
            i++;
            this.Members.ItemsSource = memberList;

            memberList.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(memberList_CollectionChanged);
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
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Dropped(object sender, ItemDragEventArgs e)
        {
        }

        private void GroupName_TextChanged(object sender, TextChangedEventArgs e)
        {
            NameChanged(this, EventArgs.Empty);
        }

        public void NameChangedValid(bool isValid)
        {
            IsValidTeamName = isValid;

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