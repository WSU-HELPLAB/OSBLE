using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBIDE.Controls.ViewModels
{
    public class SubmissionEntryViewModel : ViewModelBase
    {
        public SubmissionEntryViewModel()
        {
            DownloadCommand = new DelegateCommand(Download, CanIssueCommand);
        }

        private SubmitEvent _submission;
        public SubmitEvent Submission
        {
            get
            {
                return _submission;
            }
            set
            {
                _submission = value;
                OnPropertyChanged("Submission");
                OnPropertyChanged("SubmissionLog");
            }
        }

        public ICommand DownloadCommand { get; set; }

        private void Download(object param)
        {
            var save = new SaveFileDialog
            {
                Filter = "zip|*.zip",
                Title = "Download Submission"
            };
            save.ShowDialog();
            if (save.FileName == "") return;

            using (var stream = (FileStream)save.OpenFile())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(Submission.SolutionData);
                }
            }

            //Notify that a submission has been downloaded
            var generator = EventGenerator.GetInstance();
            generator.NotifySolutionDownloaded(Submission);
        }


        private bool CanIssueCommand(object param)
        {
            return true;
        }
    }
}
