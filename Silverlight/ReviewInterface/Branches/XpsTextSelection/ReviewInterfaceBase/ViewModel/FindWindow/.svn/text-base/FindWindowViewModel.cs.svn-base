using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ReviewInterfaceBase.Model.FindWindow;
using ReviewInterfaceBase.View.Document.FindWindow;
using ReviewInterfaceBase.ViewModel.DocumentHolder;

namespace ReviewInterfaceBase.ViewModel.FindWindow
{
    public class FindWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public event EventHandler FindNext = delegate { };

        FindWindowModel thisModel = new FindWindowModel();
        FindWindowView thisView = new FindWindowView();
        private FindWindowOptions options;
        private IDocumentHolderViewModel firstDocument;

        public IDocumentHolderViewModel FirstDocument
        {
            get { return firstDocument; }
            set { firstDocument = value; }
        }

        private object currentLocationFound = null;

        public object CurrentLocationFound
        {
            get { return currentLocationFound; }
            set { currentLocationFound = value; }
        }

        private IDocumentHolderViewModel currentDocument;

        public IDocumentHolderViewModel CurrentDocument
        {
            get { return currentDocument; }
            set { currentDocument = value; }
        }

        public bool isOpen
        {
            get { return thisView.Find_Popup.IsOpen; }
            set { thisView.Find_Popup.IsOpen = value; }
        }

        public FindWindowOptions Options
        {
            get
            {
                return options;
            }
        }

        public bool MatchCase
        {
            get { return thisModel.MatchCase; }
            set { thisModel.MatchCase = value; NotifyPropertyChanged("MatchCase"); }
        }

        public bool MatchWholeWord
        {
            get { return thisModel.MatchWholeWord; }
            set { thisModel.MatchWholeWord = value; NotifyPropertyChanged("MatchWholeWord"); }
        }

        public bool SearchUp
        {
            get { return thisModel.SearchUp; }
            set { thisModel.SearchUp = value; NotifyPropertyChanged("SearchUp"); }
        }

        public SearchIn SearchIn
        {
            get { return thisModel.SearchIn; }
            set { thisModel.SearchIn = value; NotifyPropertyChanged("SearchIn"); }
        }

        public string LookingFor
        {
            get { return thisModel.LookingFor; }
            set { thisModel.LookingFor = value; NotifyPropertyChanged("LookingFor"); }
        }

        public FindWindowViewModel()
        {
            thisView.DataContext = this;

            thisView.SearchIn_ComboBox.SelectionChanged += new SelectionChangedEventHandler(SearchIn_ComboBox_SelectionChanged);
            thisView.FindNext_Button.Click += new RoutedEventHandler(FindNext_Button_Click);
            thisView.Header.Close_Button.Click += new RoutedEventHandler(Close_Button_Click);

            initialize_SearchIn_ComboBox();

            options = new FindWindowOptions(thisModel);
        }

        private void FindNext_Button_Click(object sender, RoutedEventArgs e)
        {
            FindNext(this, EventArgs.Empty);
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            thisView.Find_Popup.IsOpen = false;
        }

        public UIElement GetView()
        {
            return thisView;
        }

        private void initialize_SearchIn_ComboBox()
        {
            //an index Enums inplicently start at 0
            int i = 0;

            //we set search to i (0) so the first enum in SearchIn
            SearchIn search = (SearchIn)i;

            //we then enter this loop which exists when i gets bigger than the number of elements
            //in the enum SearchIn
            while (Enum.IsDefined(typeof(SearchIn), i))
            {
                Type type = search.GetType();

                FieldInfo fi = type.GetField(search.ToString());

                //we get the attriutes of the selected search
                DisplyStringAttribute[] attrs = (fi.GetCustomAttributes(typeof(DisplyStringAttribute), false) as DisplyStringAttribute[]);

                //make sure we have more than (should be exactly 1)
                if (attrs.Length > 0 && attrs[0] is DisplyStringAttribute)
                {
                    thisView.SearchIn_ComboBox.Items.Add(new ComboBoxItem() { Content = attrs[0].Value });
                }
                else
                {
                    //throw and exception if not decorated with any attrs because it is a requirment
                    throw new Exception("SearchIn must have be decorated with a DisplyStringAttribute");
                }

                //didn't find it so try the next one
                i++;
                search = (SearchIn)i;
            }
            thisView.SearchIn_ComboBox.SelectedIndex = 0;
        }

        private void SearchIn_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //this converts the index back into the enum type
            thisModel.SearchIn = (SearchIn)thisView.SearchIn_ComboBox.SelectedIndex;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}