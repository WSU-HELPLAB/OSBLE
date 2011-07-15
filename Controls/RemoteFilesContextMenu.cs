using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace FileUploader.Controls
{
    public enum RemoteFileActions { MoveUp, MoveDown, Download, Delete, None };

    public class RemoteFilesContextMenu : ContextMenu
    {
        /// <summary>
        /// Will be called whenever a user selectes a menu item
        /// </summary>
        public event EventHandler<RemoteFileEventArgs> MenuItemSelected = delegate { };

        //make things cleaner by using constants for string names
        private const string MoveSelectionUpText = "Move Selection Up";
        private const string MoveSelectionDownText = "Move Selection Down";
        private const string DownloadText = "Download File";
        private const string DeleteText = "Delete Selection";

        public RemoteFilesContextMenu()
            : base()
        {
            MenuItem mi;
            
            // "move up"
            mi = new MenuItem();
            mi.Header = MoveSelectionUpText;
            mi.Click += new RoutedEventHandler(mi_Click);
            mi.MouseLeftButtonUp += new MouseButtonEventHandler(mi_MouseLeftButtonUp);
            this.Items.Add(mi);

            //move down
            mi = new MenuItem();
            mi.Header = MoveSelectionDownText;
            mi.Click += new RoutedEventHandler(mi_Click);
            mi.MouseLeftButtonUp += new MouseButtonEventHandler(mi_MouseLeftButtonUp);
            this.Items.Add(mi);

            //Download.  Turned off because a change in OSBLE proper requires that
            //you be authenticated in OSBLE prior to downloading files.  
            /*
            mi = new MenuItem();
            mi.Header = DownloadText;
            mi.Click += new RoutedEventHandler(mi_Click);
            mi.MouseLeftButtonUp += new MouseButtonEventHandler(mi_MouseLeftButtonUp);
            this.Items.Add(mi);
             * */

            //Delete
            mi = new MenuItem();
            mi.Header = DeleteText;
            mi.Click += new RoutedEventHandler(mi_Click);
            mi.MouseLeftButtonUp += new MouseButtonEventHandler(mi_MouseLeftButtonUp);
            this.Items.Add(mi);
        }

        void mi_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedItem = sender as MenuItem;
            if (selectedItem != null)
            {
                RemoteFileActions action = StringToAction(selectedItem.Header.ToString());
                MenuItemSelected(this, new RemoteFileEventArgs(action));
            }
        }

        private RemoteFileActions StringToAction(string text)
        {
            if (text.CompareTo(MoveSelectionUpText) == 0)
            {
                return RemoteFileActions.MoveUp;
            }
            else if (text.CompareTo(MoveSelectionDownText) == 0)
            {
                return RemoteFileActions.MoveDown;
            }
            else if (text.CompareTo(DownloadText) == 0)
            {
                return RemoteFileActions.Download;
            }
            else if (text.CompareTo(DeleteText) == 0)
            {
                return RemoteFileActions.Delete;
            }
            return RemoteFileActions.None;
        }
    }

    /// <summary>
    /// Event args for an event handler used by "FileUploadBegin"
    /// </summary>
    public class RemoteFileEventArgs : EventArgs
    {
        public RemoteFileActions ActionRequested
        {
            get;
            set;
        }

        public RemoteFileEventArgs(RemoteFileActions actionRequest)
        {
            ActionRequested = actionRequest;
        }
    } 
}
