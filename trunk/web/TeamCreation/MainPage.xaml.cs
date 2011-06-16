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
        private Dictionary<int, List<SerializableTeamMember>> membersBySection = new Dictionary<int, List<SerializableTeamMember>>();
        private List<SerializableTeamMember> moderators;

        private bool changedNotSaved = false;

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

            List<SerializableTeamMember> teamMembers = JsonConvert.DeserializeObject<List<SerializableTeamMember>>(SerializedTeamMembersJSON);
            InitilizeTeams(teamMembers);

            this.LayoutRoot.Loaded += new RoutedEventHandler(LayoutRoot_Loaded);
        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            HtmlPage.Window.Invoke("TeamGenerationReady", "");
        }

        private void NewTeamBox_AddTeamRequested(object sender, EventArgs e)
        {
            CreateTeam();
        }

        private void InitilizeTeams(List<SerializableTeamMember> teamMembers)
        {
            List<string> teamNames = findAllTeams(teamMembers);

            //if teamNames.Count == 0 then no teams have been assigned
            if (teamNames.Count == 0)
            {
                moderators = (from c in teamMembers where c.IsModerator == true orderby c.Name select c).ToList();
                var members = from c in teamMembers where c.IsModerator == false select c;

                foreach (var member in members)
                {
                    if (membersBySection.Keys.Contains(member.Section))
                    {
                        membersBySection[member.Section].Add(member);
                    }
                    else
                    {
                        membersBySection.Add(member.Section, new List<SerializableTeamMember>() { member });
                    }
                }

                if (membersBySection.Count() == 1 || membersBySection.Count() == 0)
                {
                    ComboBoxItem cbi = new ComboBoxItem();
                    cbi.Content = "1";
                    comboSections.Items.Add(cbi);
                    comboSections.SelectedIndex = 0;
                    HeaderStackPanel.Children.Remove(SectionTextBlock);
                    HeaderStackPanel.Children.Remove(comboSections);
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
            List<SerializableTeamMember> members;
            if (membersBySection.Count > 1)
            {
                members = membersBySection[sectionNum];
            }
            else
            {
                //if only one then get that one
                members = membersBySection.FirstOrDefault().Value;
            }

            foreach (SerializableTeamMember member in members)
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

        private List<string> findAllTeams(List<SerializableTeamMember> teamMembers)
        {
            List<string> teamNames = new List<string>();

            foreach (SerializableTeamMember member in teamMembers)
            {
                if (member.InTeamName != null && member.InTeamName != "" && !teamNames.Contains(member.InTeamName))
                {
                    teamNames.Add(member.InTeamName);
                }
            }

            return teamNames;
        }

        private string findFirstAvailableTeamName()
        {
            bool[] intFound = new bool[Teams.Children.Count];
            var teams = from c in Teams.Children where c is Team select c as Team;
            foreach (Team t in teams)
            {
                string[] name = t.TeamName.Text.Split(new string[] { "Team " }, StringSplitOptions.RemoveEmptyEntries);
                int i;
                if (name.Count() > 0 && Int32.TryParse(name[0], out i) && i < intFound.Count() && i >= 0)
                {
                    intFound[i] = true;
                }
            }

            //we start at 1 since Team0 isn't normal counting
            int count = 1;
            while (count < intFound.Count())
            {
                if (intFound[count] == false)
                {
                    return "Team " + count.ToString();
                }
                count++;
            }
            return "Team " + count.ToString();
        }

        private Team CreateTeam()
        {
            Team team = new Team(findFirstAvailableTeamName());
            team.DeleteRequest += new EventHandler(team_DeleteRequest);
            team.NameChanged += new EventHandler(Team_NameChanged);
            team.MemberListChanged += new EventHandler(team_MemberListChanged);
            if (Teams.Children.Count > 0)
            {
                Teams.Children.Insert(Teams.Children.Count - 1, team);
            }
            else
            {
                Teams.Children.Add(team);
            }
            return team;
        }

        private void team_MemberListChanged(object sender, EventArgs e)
        {
            changedNotSaved = true;
        }

        private void CreateTeam(SerializableTeamMember member, string teamName = null)
        {
            Team team = CreateTeam();
            if (teamName != null)
            {
                team.TeamName.Text = teamName;
            }
            team.MemberList.Add(new Member(member));
        }

        private void team_DeleteRequest(object sender, EventArgs e)
        {
            changedNotSaved = true;
            Team team = sender as Team;

            foreach (Member member in team.MemberList)
            {
                UnassignedList.MemberList.Add(member);
            }
            team.MemberList.Clear();
            Teams.Children.Remove(team);
        }

        private void Team_NameChanged(object sender, EventArgs e)
        {
            changedNotSaved = true;
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
            changedNotSaved = true;
            var teams = (from c in Teams.Children where c is Team select c as Team).ToList();
            foreach (Team team in teams)
            {
                //pretend like Delete was just request because it was
                team_DeleteRequest(team, EventArgs.Empty);
            }
        }

        private void PublishChanges_Click(object sender, RoutedEventArgs e)
        {
            List<SerializableTeamMember> allMembers = new List<SerializableTeamMember>();
            foreach (List<SerializableTeamMember> membersInSection in membersBySection.Values)
            {
                allMembers.AddRange(membersInSection);
            }

            ScriptObject js = HtmlPage.Window.CreateInstance("$", new string[] { "#newTeams" });

            js.Invoke("val", Uri.EscapeDataString(JsonConvert.SerializeObject(allMembers)));

            MessageBox.Show("Save Complete");
            changedNotSaved = false;
        }

        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
            if (changedNotSaved)
            {
                MessageBoxResult r = MessageBox.Show("All changes will be lost", "Cancel Changes", MessageBoxButton.OKCancel);
                if (r == MessageBoxResult.OK)
                {
                    HtmlPage.Window.Invoke("CloseTeamGenerationWindow", "");
                }
            }
            else
            {
                HtmlPage.Window.Invoke("CloseTeamGenerationWindow", "");
            }
        }

        [ScriptableMemberAttribute]
        public void GenerateTeamsFromNumberOfTeams(double numberOfTeams)
        {
            changedNotSaved = true;
            foreach (KeyValuePair<int, List<SerializableTeamMember>> section in membersBySection)
            {
                GenerateSectionTeamsFromNumberOfTeams(section.Value, (int)numberOfTeams);
            }
            comboSections_SelectionChanged(comboSections, EventArgs.Empty as SelectionChangedEventArgs);
        }

        private void GenerateSectionTeamsFromNumberOfTeams(List<SerializableTeamMember> members, int numberOfTeams)
        {
            foreach (SerializableTeamMember member in members)
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
            changedNotSaved = true;
            foreach (KeyValuePair<int, List<SerializableTeamMember>> section in membersBySection)
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