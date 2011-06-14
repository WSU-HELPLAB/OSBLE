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
        private Dictionary<int, List<SerializableTeamMembers>> membersBySection = new Dictionary<int, List<SerializableTeamMembers>>();
        private List<SerializableTeamMembers> moderators;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

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
            HtmlPage.Window.Invoke("TeamGenerationReady", "");
        }

        private void NewTeamBox_AddTeamRequested(object sender, EventArgs e)
        {
            Team team = new Team();
            team.NameChanged += new EventHandler(Team_NameChanged);
            this.Teams.Children.Insert(this.Teams.Children.Count - 1, team);
        }

        private void InitilizeTeams(List<SerializableTeamMembers> teamMembers)
        {
            List<string> teamNames = findAllTeams(teamMembers);

            //if teamNames.Count == 0 then no teams have been assigned
            if (teamNames.Count == 0)
            {
                moderators = (from c in teamMembers where c.IsModerator == true orderby c.Name select c).ToList();
                var members = from c in teamMembers where c.IsModerator == false select c;

                foreach (var memb in members)
                {
                    if (membersBySection.Keys.Contains(memb.Section))
                    {
                        membersBySection[memb.Section].Add(memb);
                    }
                    else
                    {
                        membersBySection.Add(memb.Section, new List<SerializableTeamMembers>() { memb });
                    }
                }

                if (membersBySection.Count() == 1 || membersBySection.Count() == 0)
                {
                    //remove the section combo box only one section
                }
                else
                {
                    foreach (var c in membersBySection)
                    {
                        ComboBoxItem cbi = new ComboBoxItem();
                        cbi.Content = c.Key.ToString();
                        comboSections.Items.Add(cbi);
                    }
                    comboSections.SelectionChanged += new SelectionChangedEventHandler(comboSections_SelectionChanged);
                    comboSections.SelectedIndex = 0;
                }
            }
            else
            {
            }
        }

        private void comboSections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.UnassignedList.MemberList.Clear();
            this.Teams.Children.Clear();
            int sectionNum = int.Parse((comboSections.SelectedItem as ComboBoxItem).Content as string);
            var members = membersBySection[sectionNum];
            foreach (SerializableTeamMembers member in members)
            {
                if (member.InTeamName != null && member.InTeamName != "Unassigned Team")
                {
                    bool teamFound = false;
                    foreach (Team team in Teams.Children)
                    {
                        if (team.TeamName.Text == member.InTeamName)
                        {
                            team.MemberList.Add(new Member(member));
                            teamFound = true;
                            break;
                        }
                    }
                    if (teamFound == false)
                    {
                        CreateTeam(member, member.InTeamName);
                    }
                }
                else
                {
                    this.UnassignedList.MemberList.Add(new Member(member));
                }
            }

            AddButton addbutton = new AddButton();
            addbutton.AddTeamRequested += new EventHandler(NewTeamBox_AddTeamRequested);
            Teams.Children.Add(addbutton);
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

        private void CreateTeam(SerializableTeamMembers member, string teamName = null)
        {
            Team team = new Team();
            if (teamName != null)
            {
                team.TeamName.Text = teamName;
            }
            team.NameChanged += new EventHandler(Team_NameChanged);
            team.MemberList.Add(new Member(member));
            Teams.Children.Add(team);
        }

        private void Team_NameChanged(object sender, EventArgs e)
        {
            string name = (sender as Team).TeamName.Text;
            bool isValid = true;
            foreach (var item in Teams.Children)
            {
                if (item is Team && ((sender != item && (item as Team).TeamName.Text == name) || name == "Unassigned Team"))
                {
                    isValid = false;
                }
            }
            (sender as Team).NameChangedValid(isValid);
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
            HtmlPage.Window.Invoke("CloseTeamGenerationWindow", "");
        }

        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult r = MessageBox.Show("All changes will be lost", "Cancel Changes", MessageBoxButton.OKCancel);
            if (r == MessageBoxResult.OK)
            {
            }
            else
            {
                HtmlPage.Window.Invoke("CloseTeamGenerationWindow", "");
            }
        }

        [ScriptableMemberAttribute]
        public void GenerateTeamsFromNumberOfTeams(double numberOfTeams)
        {
            foreach (KeyValuePair<int, List<SerializableTeamMembers>> section in membersBySection)
            {
                GenerateSectionTeamsFromNumberOfTeams(section.Value, (int)numberOfTeams);
            }
            comboSections_SelectionChanged(comboSections, EventArgs.Empty as SelectionChangedEventArgs);
        }

        private void GenerateSectionTeamsFromNumberOfTeams(List<SerializableTeamMembers> members, int numberOfTeams)
        {
            foreach (SerializableTeamMembers member in members)
            {
                member.InTeamName = null;
            }
            Random rand = new Random();
            int currentTeam = 1;
            int peoplePlaced = 0;

            while (peoplePlaced < members.Count)
            {
                int index = rand.Next(0, members.Count);
                if (members[index].InTeamName == null)
                {
                    members[index].InTeamName = "Team " + currentTeam.ToString();
                    currentTeam++;
                    if (currentTeam > numberOfTeams)
                    {
                        currentTeam = 1;
                    }
                    peoplePlaced++;
                }
            }
        }

        [ScriptableMemberAttribute]
        public void GenerateTeamsFromNumberOfPeople(double studentsPerTeam)
        {
            foreach (KeyValuePair<int, List<SerializableTeamMembers>> section in membersBySection)
            {
                //integer division on purpose
                int numOfTeams = section.Value.Count() / (int)studentsPerTeam;
                if (studentsPerTeam * numOfTeams != section.Value.Count())
                {
                    numOfTeams++;
                }
                GenerateSectionTeamsFromNumberOfTeams(section.Value, (int)numOfTeams);
            }
            comboSections_SelectionChanged(comboSections, EventArgs.Empty as SelectionChangedEventArgs);
        }
    }
}