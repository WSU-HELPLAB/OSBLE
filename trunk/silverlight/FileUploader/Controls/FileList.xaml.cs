using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using FileUploader.OsbleServices;
namespace FileUploader.Controls
{
    public partial class FileList : UserControl
    {
        /// <summary>
        /// Used to track the last selected item in the list.  A hacky way to provide
        /// "double click" behavior.
        /// </summary>
        public FileListItem LastSelectedItem
        {
            get;
            set;
        }

        /// <summary>
        /// Exposes the currently selected AbstractListing
        /// </summary>
        public AbstractListing SelectedItem
        {
            get
            {
                if (LastSelectedItem != null)
                {
                    return LastSelectedItem.DataContext as AbstractListing;
                }
                return null;
            }
        }

        //Event handlers used to inform consumers that we might need additional information.
        public EventHandler ParentDirectoryRequest = delegate { };
        public EventHandler EmptyDirectoryEncountered = delegate { };

        //used to get us back to the previous data context when doing directory traversals
        private Stack<DirectoryListing> previousDataContexts = new Stack<DirectoryListing>();

        private DirectoryListing dataContext;
        public new DirectoryListing DataContext
        {
            get
            {
                return dataContext;
            }
            set
            {
                dataContext = value;
                UpdateListing();

                //if the data context is empty, it might be a sign that we don't have a listing
                //for the directory
                if (DataContext.Files.Count == 0 && DataContext.Directories.Count == 0)
                {
                    EmptyDirectoryEncountered(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Clears the previous directory stack
        /// </summary>
        public void ClearPreviousDirectories()
        {
            previousDataContexts.Clear();

        }

        /// <summary>
        /// Clears the currently selected item
        /// </summary>
        public void ClearSelection()
        {
            ListOfFiles.SelectedItem = null;
        }

        /// <summary>
        /// Constructor method.  'Nuff said.
        /// </summary>
        public FileList()
        {
            InitializeComponent();

            //register event listeners
            ListOfFiles.MouseLeftButtonUp += new MouseButtonEventHandler(ListOfFiles_MouseLeftButtonUp);
        }

        /// <summary>
        /// Moves the selected item up in the list ordering
        /// </summary>
        public void MoveSelectionUp()
        {
            if (LastSelectedItem != null)
            {
                if (ListOfFiles.SelectedIndex > 1)
                {
                    AbstractListing previousItem = (ListOfFiles.Items[ListOfFiles.SelectedIndex - 1] as FileListItem).DataContext;
                    AbstractListing currentItem = LastSelectedItem.DataContext;
                    int tmpOrder = previousItem.SortOrder;
                    previousItem.SortOrder = currentItem.SortOrder;
                    currentItem.SortOrder = tmpOrder;
                    MoveItem(ListOfFiles.SelectedIndex, ListOfFiles.SelectedIndex - 1);
                }
            }
        }

        /// <summary>
        /// Moves the selected item down in the list ordering
        /// </summary>
        public void MoveSelectionDown()
        {
            if (LastSelectedItem != null)
            {
                if (ListOfFiles.SelectedIndex < ListOfFiles.Items.Count - 1)
                {
                    AbstractListing currentItem = LastSelectedItem.DataContext;
                    AbstractListing nextItem = (ListOfFiles.Items[ListOfFiles.SelectedIndex + 1] as FileListItem).DataContext;
                    int tmpOrder = nextItem.SortOrder;
                    nextItem.SortOrder = currentItem.SortOrder;
                    currentItem.SortOrder = tmpOrder;
                    MoveItem(ListOfFiles.SelectedIndex, ListOfFiles.SelectedIndex + 1);
                }
            }
        }

        /// <summary>
        /// Moves a list item to a new location
        /// </summary>
        /// <param name="oldLocation"></param>
        /// <param name="newLocation"></param>
        /// <param name="listItem"></param>
        private void MoveItem(int oldLocation, int newLocation)
        {
            if (oldLocation > 0 && newLocation > 0)
            {
                FileListItem listItem = (ListOfFiles.Items[oldLocation] as FileListItem);
                ListOfFiles.Items.RemoveAt(oldLocation);
                ListOfFiles.Items.Insert(newLocation, listItem);
                ListOfFiles.SelectedItem = listItem;
            }
        }

        /// <summary>
        /// We listen for left mouse button clicks to check for item selection and directory
        /// traversal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ListOfFiles_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FileListItem selectedItem = (sender as ListBox).SelectedItem as FileListItem;
            if (LastSelectedItem == selectedItem && selectedItem != null)
            {
                //we only care about double clicks on directory listings
                if (selectedItem.DataContext is DirectoryListing)
                {
                    //is the a "parent" directory
                    if (selectedItem.DataContext is ParentDirectoryListing)
                    {
                        //we received a request to move up the folder hierarchy.  Pop the
                        //last data context off of the stack.
                        if (previousDataContexts.Count > 0)
                        {
                            DirectoryListing lastDataContext = previousDataContexts.Pop();
                            DataContext = lastDataContext;
                        }
                        else
                        {
                            //We've reached our limit.  Inform the captain!
                            ParentDirectoryRequest(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        //used to prevent constant casting
                        DirectoryListing newDataContext = selectedItem.DataContext as DirectoryListing;

                        //push the current data context onto the stack
                        previousDataContexts.Push(DataContext);

                        //replace the data context with the select item's data context
                        DataContext = newDataContext;
                    }
                }
            }
            LastSelectedItem = selectedItem;
        }

        /// <summary>
        /// Updates the current list of files
        /// </summary>
        private void UpdateListing()
        {
            ListOfFiles.Items.Clear();

            //add directories
            List<DirectoryListing> directories = (from directory in DataContext.Directories
                                                  orderby directory.SortOrder
                                                  select directory).ToList();
            foreach (DirectoryListing dl in directories)
            {
                FileListItem item = new FileListItem();
                item.DataContext = dl;
                item.MouseRightButtonDown += new MouseButtonEventHandler(ListItem_MouseRightButtonDown);
                ListOfFiles.Items.Add(item);
            }

            //and then files
            List<FileListing> files = (from file in DataContext.Files
                                       orderby file.SortOrder
                                       select file).ToList();
            foreach (FileListing fl in files)
            {
                FileListItem item = new FileListItem();
                item.DataContext = fl;
                item.MouseRightButtonDown += new MouseButtonEventHandler(ListItem_MouseRightButtonDown);
                ListOfFiles.Items.Add(item);
            }
        }

        void ListItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListOfFiles.SelectedItem = sender;
            this.LastSelectedItem = sender as FileListItem;
        }
    }
}
