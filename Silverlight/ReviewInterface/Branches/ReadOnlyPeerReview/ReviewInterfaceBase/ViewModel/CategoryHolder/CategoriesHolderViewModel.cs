using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.Model.CatergoryHolder;
using ReviewInterfaceBase.View.CategoryHolder;
using ReviewInterfaceBase.ViewModel.Category;
using ReviewInterfaceBase.View.Category;

namespace ReviewInterfaceBase.ViewModel.CategoryHolder
{
    public class CategoriesHolderViewModel
    {
        public event SizeChangedEventHandler SizeChanged = delegate { };

        public event EventHandler LoadComplete = delegate { };

        private CategoriesHolderView thisView = new CategoriesHolderView();

        private CategoryHolderModel thisModel;

        public int AllowedCategories
        {
            get { return thisModel.AllowedCategories; }
        }

        public CategoriesHolderView GetView()
        {
            return thisView;
        }

        public List<CategoryViewModel> Categories
        {
            get { return thisModel.Categories; }
        }


        private void updateView()
        {
            int i = 0;
            int column = 0;
            int row = 0;

            //we will have 2 rows
            thisView.LayoutRoot.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            thisView.LayoutRoot.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });

            //we will have as many columns as needed, +1 to round up
            for (int j = 0; j < (Categories.Count + 1) / 2; j++)
            {
                thisView.LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            }

            //This adds the Categories in order as shown below
            /* _____________
             * |1st|3rd|5th|
             * |2nd|4th|6th|
             * -------------
             */
            while (i < Categories.Count)
            {
                if (Categories[i] == null)
                {
                    break;
                }
                else
                {
                    Categories[i].GetView().SetValue(Grid.RowProperty, row);
                    Categories[i].GetView().SetValue(Grid.ColumnProperty, column);
                    thisView.LayoutRoot.Children.Add(Categories[i].GetView());
                }

                i++;
                row++;
                if (row >= 2)
                {
                    row = 0;
                    column++;
                }
            }
        }
        public void CategorySelection()
        {
            foreach (UIElement ui in thisView.LayoutRoot.Children)
            {
                //hmm
            }
            foreach (CategoryViewModel cvm in Categories)
            {
                //cvm.SelectedTagIndex = 1;
                foreach (string s in (cvm.GetView() as CategoryView).TagHolder.Items)
                {
                    MessageBox.Show(s);
                }
            }
        }
        public CategoriesHolderViewModel()
        {
            thisView = new CategoriesHolderView();
        }

        public void LoadIssueVotingCategories()
        {
            thisModel = new CategoryHolderModel();
            thisModel.LoadCompleted += new EventHandler(thisModel_LoadComplete);
            thisModel.LoadIssueVotingCategories();
        }

        public void LoadCategories(int documentID)
        {
            thisModel = new CategoryHolderModel(documentID);
            thisModel.TagsLoaded += new EventHandler(thisModel_TagsLoaded);
            thisModel.LoadCompleted += new EventHandler(thisModel_LoadComplete);
            thisModel.Load();
        }

        void thisModel_TagsLoaded(object sender, EventArgs e)
        {
            //This event needs to be tied into whatever owns CHVM to let them know the tags are loaded and that they are selectable now
            LoadComplete(sender, e);
        }

        public void ReadXml(XElement categories)
        {
            thisModel = new CategoryHolderModel();
            foreach (XElement category in categories.Descendants("Category"))
            {
                CategoryViewModel cvm = new CategoryViewModel();
                cvm.ReadXml(category);
                Categories.Add(cvm);
            }
            updateView();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Categories");
            foreach (CategoryViewModel cvm in Categories)
            {
                cvm.WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        private void thisView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChanged(this, EventArgs.Empty as SizeChangedEventArgs);
        }

        private void thisModel_LoadComplete(object sender, EventArgs e)
        {
            updateView();



            thisView.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);
            foreach (CategoryViewModel cvm in Categories)
            {
                //whenever one of the sizes changes we assume we changed so this wires it up to do that
                cvm.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);
            }

            //LoadComplete(this, EventArgs.Empty);
        }
    }
}