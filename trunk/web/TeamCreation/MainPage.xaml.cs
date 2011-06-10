using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace TeamCreation
{
    public partial class MainPage : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged Members

        public MainPage(string SerializedTeamMembersJSON)
        {
            InitializeComponent();

            this.UnassignedList.DeleteList.Visibility = Visibility.Collapsed;
            this.UnassignedList.TeamName.IsReadOnly = true;
            this.UnassignedList.TeamName.Text = "Unassigned Team";

            this.NewTeamBox.AddTeamRequested += new EventHandler(NewTeamBox_AddTeamRequested);

            List<SerializableTeamMembers> teamMembers = JsonConvert.DeserializeObject<List<SerializableTeamMembers>>(SerializedTeamMembersJSON);
            InitilizeTeams(teamMembers);

            this.LayoutRoot.Loaded += new RoutedEventHandler(LayoutRoot_Loaded);
        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            HtmlPage.Window.Invoke("SLReady", "");
        }

        private void NewTeamBox_AddTeamRequested(object sender, EventArgs e)
        {
            this.Teams.Children.Insert(this.Teams.Children.Count - 1, new Team());
        }

        private void InitilizeTeams(List<SerializableTeamMembers> teamMembers)
        {
            List<string> teamNames = findAllTeams(teamMembers);

            //if teamNames.Count == 0 then no teams have been assigned
            if (teamNames.Count == 0)
            {
                var moderators = from c in teamMembers where c.IsModerator == true orderby c.Name select c;
                var students = from c in teamMembers where c.IsModerator == false orderby c.Name select c;

                //this.UnassignedList.MemberList.Clear();
                foreach (SerializableTeamMembers member in students)
                {
                    this.UnassignedList.MemberList.Add(new Member(member));
                }

                foreach (SerializableTeamMembers member in moderators)
                {
                    this.UnassignedList.MemberList.Add(new Member(member));
                }
            }
            else
            {
            }
        }

        private List<string> findAllTeams(List<SerializableTeamMembers> teamMembers)
        {
            List<string> teamNames = new List<string>();

            foreach (SerializableTeamMembers member in teamMembers)
            {
                if (member.InTeamName != null && member.InTeamName != "" && !teamNames.Contains(member.InTeamName))
                {
                    teamNames.Add(member.InTeamName);
                }
            }

            return teamNames;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ClearTeams_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
        }

        private void combos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void PublishChanges_Click(object sender, RoutedEventArgs e)
        {
        }

        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
        }

        [ScriptableMemberAttribute]
        public void GenerateTeamsFromNumberOfTeams(int numberOfTeams)
        {
            Random rand = new Random();
            this.Teams.Children.Clear();
            int i = 0;
            while (i < numberOfTeams)
            {
                this.Teams.Children.Add(new Team());
                i++;
            }

            var students = (from c in UnassignedList.MemberList where c.IsModerator == false select c).ToList();
            var moderators = (from c in UnassignedList.MemberList where c.IsModerator == true select c).ToList();

            Team current = this.Teams.Children[0] as Team;

            while (students.Count > 0)
            {
                int index = rand.Next(0, students.Count());
                current.MemberList.Add(students[index]);
                students.RemoveAt(index);
                current = GetNextTeam(current);
            }

            current = this.Teams.Children[0] as Team;

            while (moderators.Count > 0)
            {
                int index = rand.Next(0, moderators.Count());
                current.MemberList.Add(moderators[index]);
                moderators.RemoveAt(index);
                current = GetNextTeam(current);
            }
        }

        private Team GetNextTeam(Team currentTeam)
        {
            if (currentTeam == null)
            {
                return Teams.Children[0] as Team;
            }
            else
            {
                int index = Teams.Children.IndexOf(currentTeam);
                if (Teams.Children[index] is Team)
                {
                    return Teams.Children[index] as Team;
                }
                else
                {
                    return Teams.Children[0] as Team;
                }
            }
        }

        [ScriptableMemberAttribute]
        public void GenerateTeamsFromNumberOfPeople(int peoplePerTeam)
        {
        }
    }
}