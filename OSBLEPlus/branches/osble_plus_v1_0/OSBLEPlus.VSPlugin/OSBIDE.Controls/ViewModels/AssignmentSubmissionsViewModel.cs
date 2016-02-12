using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using Ionic.Zip;
using OSBIDE.Library.ServiceClient.ServiceHelpers;

namespace OSBIDE.Controls.ViewModels
{
    public class AssignmentSubmissionsViewModel : ViewModelBase
    {
        private string _selectedAssignment = "";
        private string _errorMessage = "";

        public ObservableCollection<string> AvailableAssignments { get; set; }
        public ObservableCollection<SubmissionEntryViewModel> SubmissionEntries { get; set; }
        public ICommand DownloadCommand { get; set; }
        public string SelectedAssignment
        {
            get
            {
                return _selectedAssignment;
            }
            set
            {
                _selectedAssignment = value;
                OnPropertyChanged("SelectedAssignment");
            }
        }

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            private set
            {
                _errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }

        public AssignmentSubmissionsViewModel()
        {
            DownloadCommand = new DelegateCommand(Download, CanIssueCommand);
            AvailableAssignments = new ObservableCollection<string>();
            SubmissionEntries = new ObservableCollection<SubmissionEntryViewModel>();
        }

        private void Download(object param)
        {
            var save = new FolderBrowserDialog();
            var result = save.ShowDialog();
            if (result != DialogResult.OK) return;

            var directory = save.SelectedPath;
            foreach (var vm in SubmissionEntries)
            {
                var unpackDir = Path.Combine(directory, vm.Submission.Sender.FullName);
                using (var zipStream = new MemoryStream())
                {
                    zipStream.Write(vm.Submission.SolutionData, 0, vm.Submission.SolutionData.Length);
                    zipStream.Position = 0;
                    try
                    {
                        using (var zip = ZipFile.Read(zipStream))
                        {
                            foreach (var entry in zip)
                            {
                                try
                                {
                                    entry.Extract(unpackDir, ExtractExistingFileAction.OverwriteSilently);
                                }
                                catch (BadReadException ex)
                                {
                                    ErrorMessage = ex.Message;
                                }
                                catch (Exception ex)
                                {
                                    ErrorMessage = ex.Message;
                                }
                            }
                        }
                    }
                    catch (ZipException ex)
                    {
                        ErrorMessage = ex.Message;
                        MessageBox.Show(string.Format("extract failed for {0}", vm.Submission.Sender.FullName));
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = ex.Message;
                    }
                }

                //notify that someone has downloaded a submission
                var generator = EventGenerator.GetInstance();
                generator.NotifySolutionDownloaded(vm.Submission);
            }
        }

        private bool CanIssueCommand(object param)
        {
            return true;
        }
    }
}
