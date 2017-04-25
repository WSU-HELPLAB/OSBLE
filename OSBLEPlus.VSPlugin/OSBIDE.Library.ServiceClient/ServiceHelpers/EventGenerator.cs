using System;
using OSBLE.Interfaces;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public class EventGenerator : IEventGenerator
    {
        private static EventGenerator _instance;

        public event EventHandler<SubmitAssignmentArgs> SolutionSubmitRequest = delegate { };
        public event EventHandler<SubmitEventArgs> SubmitEventRequested = delegate { };
        public event EventHandler<SolutionDownloadedEventArgs> SolutionDownloaded = delegate { };

        private EventGenerator()
        {
        }

        public static EventGenerator GetInstance()
        {
            return _instance ?? (_instance = new EventGenerator());
        }

        public void SubmitEvent(IActivityEvent evt)
        {
            SubmitEventRequested(this, new SubmitEventArgs(evt));
        }

        /// <summary>
        /// Triggers a request for the system to save the active solution
        /// </summary>
        public void RequestSolutionSubmit(int assignmentId)
        {
            SolutionSubmitRequest(this, new SubmitAssignmentArgs(assignmentId));
        }

        public void NotifySolutionDownloaded(SubmitEvent downloadedSubmission)
        {
            SolutionDownloaded(downloadedSubmission.Sender, new SolutionDownloadedEventArgs(downloadedSubmission.Sender, downloadedSubmission));
        }
    }

    public class SubmitEventArgs : EventArgs
    {
        public IActivityEvent Event { get; private set; }
        public SubmitEventArgs(IActivityEvent evt)
        {
            Event = evt;
        }
    }

    public class SubmitAssignmentArgs : EventArgs
    {
        public int AssignmentId { get; private set; }
        public SubmitAssignmentArgs(int assignmentId)
        {
            AssignmentId = assignmentId;
        }
    }

    public class SolutionDownloadedEventArgs : EventArgs
    {
        public IUser DownloadingUser { get; private set; }
        public SubmitEvent DownloadedSubmission { get; private set; }

        public SolutionDownloadedEventArgs(IUser downloadingUser, SubmitEvent downloadedSubmission)
        {
            DownloadingUser = downloadingUser;
            DownloadedSubmission = downloadedSubmission;
        }
    }
}
