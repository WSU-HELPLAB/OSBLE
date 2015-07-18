using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBLE.Interfaces;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBIDE.Controls.ViewModels
{
    public class SubmitAssignmentViewModel : ViewModelBase
    {
        private readonly string _authToken;
        private readonly SubmitEvent _submitEvent;

        public SubmitAssignmentViewModel(string userName, string authToken, SubmitEvent submitEvent)
        {
            _authToken = authToken;
            _submitEvent = submitEvent;

            UserName = userName;
            SolutionName = Path.GetFileNameWithoutExtension(submitEvent.SolutionName);
            ContinueCommand = new DelegateCommand(Continue, CanIssueCommand);
            CancelCommand = new DelegateCommand(Cancel, CanIssueCommand);
            Assignments = new ObservableCollection<SubmisionAssignment>();
            Courses = new ObservableCollection<ICourse>();
            LastSubmitted = "N/A";

            // load courses
            IsLoading = true;
            GetCoursesForUserAsync(authToken);
        }

        private void GetCoursesForUserAsync(string authToken)
        {
            var courses = AsyncServiceClient.GetCoursesForUser(authToken).Result;

            Courses.Clear();
            foreach (var course in courses.OfType<ICourse>())
            {
                Courses.Add(course);
            }

            IsLoading = false;
        }

        private void GetAssignmentsForCourseAsync(int courseId, string authToken)
        {
            var assignments = AsyncServiceClient.GetAssignmentsForCourse(courseId, authToken).Result;
            Assignments.Clear();
            foreach (var assignment in assignments)
            {
                Assignments.Add(assignment);
            }

            IsLoading = false;
        }

        private void GetLastAssignmentSubmitDateAsync(int assignmentId, string authToken)
        {
            var submissionDate = AsyncServiceClient.GetLastAssignmentSubmitDate(assignmentId, authToken).Result;
            if (!submissionDate.HasValue || submissionDate.Value == DateTime.MinValue)
            {
                LastSubmitted = "N/A";
            }
            else
            {
                //convert from UTC to local time
                var utc = new DateTime(submissionDate.Value.Ticks, DateTimeKind.Utc);
                LastSubmitted = utc.ToLocalTime().ToString("MM/dd @ hh:mm tt");
            }
        }

        private void SubmitAssignmentAsync()
        {
            _submitEvent.CreateSolutionBinary(_submitEvent.GetSolutionBinary());
            _submitEvent.AssignmentId = SelectedAssignment;

            var confirmation = AsyncServiceClient.SubmitAssignment(_submitEvent, _authToken).Result;
            SubmitAssignmentCompleted(confirmation);
        }

        void SubmitAssignmentCompleted(int confirmation)
        {
            if (confirmation > 0)
            {
                ServerMessage = "Your assignment was successfully submitted.  Your confirmation number is: \"" + confirmation + "\".";
            }
            else
            {
                ServerMessage = "Transmission error.  If the problem persists, please contact your course instructor.";
            }

            GetLastAssignmentSubmitDateAsync(SelectedAssignment, _authToken);

            IsLoading = false;
        }

        #region properties

        public event EventHandler RequestClose = delegate { };

        public ObservableCollection<SubmisionAssignment> Assignments { get; set; }
        public ObservableCollection<ICourse> Courses { get; set; }
        public MessageBoxResult Result { get; private set; }
        public ICommand ContinueCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private string _serverMessage = string.Empty;
        public string ServerMessage
        {
            get
            {
                return _serverMessage;
            }
            set
            {
                _serverMessage = value;
                OnPropertyChanged("ServerMessage");
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                _isLoading = value;
                OnPropertyChanged("IsLoading");
            }
        }

        private int _selectedAssignment = -1;
        public int SelectedAssignment
        {
            get
            {
                return _selectedAssignment;
            }
            set
            {
                _selectedAssignment = value;

                //find last submit date
                GetLastAssignmentSubmitDateAsync(value, _authToken);

                OnPropertyChanged("SelectedAssignment");
                OnPropertyChanged("HasAssignmentSelected");
            }
        }

        public bool HasAssignmentSelected
        {
            get
            {
                return _selectedAssignment > 0;
            }
        }

        private int _selectedCourse = -1;
        public int SelectedCourse
        {
            get
            {
                return _selectedCourse;
            }
            set
            {
                _selectedCourse = value;

                //load assignments
                IsLoading = true;
                GetAssignmentsForCourseAsync(_selectedCourse, _authToken);

                OnPropertyChanged("SelectedCourse");
                OnPropertyChanged("HasCourseSelected");
            }
        }

        public bool HasCourseSelected
        {
            get
            {
                return _selectedCourse > 0;
            }
        }

        public string UserName
        {
            private set;
            get;
        }

        private string _lastSubmitted = string.Empty;
        public string LastSubmitted
        {
            get
            {
                return _lastSubmitted;
            }
            set
            {
                _lastSubmitted = value;
                OnPropertyChanged("LastSubmitted");
            }
        }

        private string _solutionName = string.Empty;
        public string SolutionName
        {
            get
            {
                return _solutionName;
            }
            set
            {
                _solutionName = value;
                OnPropertyChanged("SolutionName");
            }
        }

        #endregion

        private void Continue(object param)
        {
            Result = MessageBoxResult.OK;
            IsLoading = true;

            SubmitAssignmentAsync();
        }

        private void Cancel(object param)
        {
            Result = MessageBoxResult.Cancel;
            RequestClose(this, EventArgs.Empty);
        }

        private bool CanIssueCommand(object param)
        {
            return true;
        }
    }
}
