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
    public enum LocalFileActions { SendSingleFile, None };

    public class LocalFilesContextMenu : ContextMenu
    {
        /// <summary>
        /// Will be called whenever a user selectes a menu item
        /// </summary>
        public event EventHandler<LocalFileEventArgs> MenuItemSelected = delegate { };

        //make things cleaner by using constants for string names
        private const string SendSelectionText = "Send Selection";

        public LocalFilesContextMenu()
            : base()
        {
            MenuItem mi;

            // "Send Selected File"
            mi = new MenuItem();
            mi.Header = SendSelectionText;
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
                LocalFileActions action = StringToAction(selectedItem.Header.ToString());
                MenuItemSelected(this, new LocalFileEventArgs(action));
            }
        }

        private LocalFileActions StringToAction(string text)
        {
            if (text.CompareTo(SendSelectionText) == 0)
            {
                return LocalFileActions.SendSingleFile;
            }
            return LocalFileActions.None;
        }
    }

    /// <summary>
    /// Event args for an event handler used by "FileUploadBegin"
    /// </summary>
    public class LocalFileEventArgs : EventArgs
    {
        public LocalFileActions ActionRequested
        {
            get;
            set;
        }

        public LocalFileEventArgs(LocalFileActions actionRequest)
        {
            ActionRequested = actionRequest;
        }
    }
}
