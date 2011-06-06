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
using ChemProV.PFD.Streams.PropertiesWindow;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace OsbleRubric
{
    public class RubricModel
    {
        #region Attributes
        private CustomDataGrid myGrid = new CustomDataGrid();
        private Rubric thisView = new Rubric();
        #endregion

        #region constants
        const double Col0Width = 180;
        const double Row0Height = 100;
        const double ColWidth = 250;
        const double RowHeight = 138;
        const double Col1Width = 150;
        const int GlobalFontSize = 12;
        private ImageSource helpIconSource = new BitmapImage(new Uri("Icons/help.png", UriKind.Relative));
        private ImageSource deleteIconSource = new BitmapImage(new Uri("Icons/delete.png", UriKind.Relative));
        #endregion


        public RubricModel()
        {
            initialize();
        }

        private void initialize()
        {

            //adding grid to the view
            thisView.LayoutRoot.Children.Add(myGrid.BaseGrid);

            //setting color for grid
            myGrid.BaseGrid.Background = new SolidColorBrush(Colors.LightGray);

            //setting borders for grid
            myGrid.HideBordersForLastRow = false;
            myGrid.HideBordersForLastTwoColumns = false;
            myGrid.BorderBrush = new SolidColorBrush(Colors.Black);
            myGrid.BorderThickness = 1.0;


            //adding initial columns & rows
            myGrid.PlaceUIElement(createCol0Row0(), 0, 0);
            myGrid.PlaceUIElement(createCol1Row0(), 1, 0);
            myGrid.PlaceUIElement(createLevelTitleCell(), 2, 0);
            createCriterionRow();

            //setting widths/heights for the table - possibly best to set width/height somewhere else
            myGrid.BaseGrid.RowDefinitions[0].Height = new GridLength(Row0Height);
            for (int i = 1; i < myGrid.BaseGrid.RowDefinitions.Count; ++i)
            {
                myGrid.BaseGrid.RowDefinitions[i].Height = new GridLength(RowHeight);
            }
            myGrid.BaseGrid.ColumnDefinitions[0].Width = new GridLength(Col0Width);
            myGrid.BaseGrid.ColumnDefinitions[1].Width = new GridLength(Col1Width);
            for (int i = 2; i < myGrid.BaseGrid.ColumnDefinitions.Count; ++i)
            {
                myGrid.BaseGrid.ColumnDefinitions[i].Width = new GridLength(ColWidth);
            }



        }



        private void removeRow()
        {
            int toRemove = 2;
            //for this, look at the row deleting, all rows before that (lower index) dont change, all rows after, reduce index by 1 (to account for the delete) and then delete the last row (might not be last, probably second to last because of bottom row)
            //problems might be hooking up to DB

            //new strat: delete all UIielements in each column, then use removeat

            /*
            for(int i = 0; i < myGrid.BaseGrid.ColumnDefinitions.Count; ++i)
            {

            }*/
            List<FrameworkElement> elementsToRemove= new List<FrameworkElement>();

            foreach (FrameworkElement element in myGrid.BaseGrid.Children)
            {
                if (Grid.GetRow(element) == toRemove)
                {
                    elementsToRemove.Add(element);
                }
            }
            foreach (FrameworkElement element in elementsToRemove)
            {
               // myGrid.BaseGrid.Children.Remove(element);
            }

          //myGrid.BaseGrid.RowDefinitions.RemoveAt(toRemove-1);


        }
        private void removeCol()
        {
            
            //do combo of this plus remove the last. then change all row definitions. ugh.
            int toRemove = 2;
            
            //removes UI elements
            
            for (int i = myGrid.BaseGrid.RowDefinitions.Count-1 ; i >=0 ; --i)
            {
                myGrid.RemoveUIElementAt(toRemove, i);
            }

            //myGrid.UpdateGridSize();
            //Tries to remove the column...not working correctly 
           // myGrid.BaseGrid.ColumnDefinitions.RemoveAt(toRemove);


        }

        //This method creates and correctly populates an entire column
        private void createLevelColumn()
        {
            //create a column, must go from 0 to max-1 (handle the latter later)
            //in 0 put createLevelTitleCell, in rest, they need createLevelCell()
            int colIndexToInsert = myGrid.BaseGrid.ColumnDefinitions.Count;
            myGrid.PlaceUIElement(createLevelTitleCell(), colIndexToInsert, 0);
            for (int i = 1; i < myGrid.BaseGrid.RowDefinitions.Count; i++)
            {
                myGrid.PlaceUIElement(createLevelCell(), colIndexToInsert, i);
            }
        }

        //This method creates and correctly populates an entire row
        private void createCriterionRow()
        {
            int rowPlacement = myGrid.BaseGrid.RowDefinitions.Count;
            myGrid.PlaceUIElement(createPerformanceCritCell(), 0, rowPlacement);
            myGrid.PlaceUIElement(createCritWeightCell(), 1, rowPlacement);

            for (int i = 2; i < myGrid.BaseGrid.ColumnDefinitions.Count; i++)
            {
                myGrid.PlaceUIElement(createLevelCell(), i, rowPlacement);
            }
            
        }

        //This method creates the cells used for a Level column (With the exception of the first row)
        private StackPanel createLevelCell()
        {
            StackPanel returnVal = new StackPanel()
            {
                Margin = new Thickness(5, 5, 5, 5)
            };

            TextBox textbox = new TextBox()
            {
                Height = RowHeight-12,
                Width = returnVal.Width,
                TextWrapping = TextWrapping.Wrap
            };

            returnVal.Children.Add(textbox);

            return returnVal;
        }

        //This method creates the cells used for the Criterion Weight column (With the exception of the first row)
        private StackPanel createCritWeightCell()
        {
            StackPanel returnVal = new StackPanel()
            {
                Margin = new Thickness(5, 5, 5, 5),
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBox textbox = new TextBox()
            {
                Width = 70,
                Height = 22
            };
            TextBlock textBlock = new TextBlock() {Text = "Points", Margin = new Thickness(5,0,0,0), VerticalAlignment = VerticalAlignment.Center};

            returnVal.Children.Add(textbox);
            returnVal.Children.Add(textBlock);

            return returnVal;
        }

        //This method creates the cells used for the Performance Criterion column (With the exception of the first row)
        private StackPanel createPerformanceCritCell()
        {
            StackPanel returnVal = new StackPanel()
            {
                Margin = new Thickness(5,5,5,5)
            };

            TextBox textbox = new TextBox()
            {
                Height = 90,
                Width = returnVal.Width,
                TextWrapping = TextWrapping.Wrap
            };
            Image deleteIcon = new Image { Source = deleteIconSource, Height = 32, Width = 32, Margin = new Thickness(0,3,0,0) };
            deleteIcon.MouseLeftButtonDown += new MouseButtonEventHandler(Delete_MouseLeftButtonDown);

            returnVal.Children.Add(textbox);
            returnVal.Children.Add(deleteIcon);

            return returnVal;
        }

        //This method creates the top row for the Performance Criterion column
        private StackPanel createCol0Row0()
        {
            StackPanel returnVal = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };

            //stuff for returnVal
            TextBlock TBlock1 = new TextBlock() { Text = "Performance Criterion", FontWeight = FontWeights.Bold, FontSize = GlobalFontSize, VerticalAlignment = VerticalAlignment.Center };
            Image Img1 = new Image() { Source = helpIconSource, Width = 16, Height = 16, Margin = new Thickness(3, 0, 0, 0) };
            ToolTipService.SetToolTip(Img1, "Input your Performance Criterion in the text area.\nThe minus button is for removing a criterion row.");

            /*DELETE ME SOON*/
            Img1.MouseLeftButtonDown += new MouseButtonEventHandler(Img1_MouseLeftButtonDown);
            TBlock1.MouseLeftButtonDown += new MouseButtonEventHandler(TBlock1_MouseLeftButtonDown);


            //adding children to returnVal
            returnVal.Children.Add(TBlock1);
            returnVal.Children.Add(Img1);
            return returnVal;
        }

        //This method creates the top row for the Weight Criterion column
        private StackPanel createCol1Row0()
        {
            StackPanel returnVal = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };

            //stuff for returnVal
            TextBlock TBlock2 = new TextBlock() { Text = "Criterion Weight", FontWeight = FontWeights.Bold, FontSize = GlobalFontSize, VerticalAlignment = VerticalAlignment.Center };
            Image Img2 = new Image() { Source = helpIconSource, Width = 16, Height = 16, Margin = new Thickness(3, 0, 0, 0) };
            ToolTipService.SetToolTip(Img2, "The criterion weight column is used to set the\nweights of each criterion row. By default the\nweight is evenly distrbuted among the criterion.\nTo set the weight simply input a number between\n1 and 100 in the column below. The weight is\nautomatically recalculated upon input.");

            /*DELETE ME SOON*/
            Img2.MouseLeftButtonDown += new MouseButtonEventHandler(Img2_MouseLeftButtonDown);
            TBlock2.MouseLeftButtonDown += new MouseButtonEventHandler(TBlock2_MouseLeftButtonDown);

            //adding children to returnVal
            returnVal.Children.Add(TBlock2);
            returnVal.Children.Add(Img2);
            return returnVal;
        }

        //This method is used to create the top row for a Level column. It adds and formats all the appropriate UIelements
        private StackPanel createLevelTitleCell()
        {
            Thickness generalMargin = new Thickness(5, 0, 5, 0);
            int comboBoxMin = 1;
            int comboBoxMax = 10;
            StackPanel returnVal = new StackPanel();
            returnVal.Orientation = Orientation.Vertical;
            returnVal.HorizontalAlignment = HorizontalAlignment.Center;
            returnVal.VerticalAlignment = VerticalAlignment.Center;

            //first stackpanel
            StackPanel SP1 = new StackPanel();
            SP1.Orientation = Orientation.Horizontal;
            SP1.HorizontalAlignment = HorizontalAlignment.Center;

            //stuff for SP1
            TextBlock TBlock1 = new TextBlock();
            TBlock1.Text = "Level Title";
            TBlock1.FontWeight = FontWeights.Bold;
            TBlock1.FontSize = GlobalFontSize;
            TBlock1.VerticalAlignment = VerticalAlignment.Center;

            TextBox TBox1 = new TextBox();
            TBox1.Width = 100;
            TBox1.Height = 22;

            Image Img1 = new Image() { Source = deleteIconSource, Height=32, Width=32 };
            Img1.MouseLeftButtonDown +=new MouseButtonEventHandler(DeleteCol_MouseLeftButtonDown);

            //margins
            TBlock1.Margin = generalMargin;
            TBox1.Margin = generalMargin;
            Img1.Margin = generalMargin;


            //adding children to SP1
            SP1.Children.Add(TBlock1);
            SP1.Children.Add(TBox1);
            SP1.Children.Add(Img1);

            //second stackpanel
            StackPanel SP2 = new StackPanel();
            SP2.Orientation = Orientation.Horizontal;

            //stuff for SP2 - Textblock for label, Combobox for ranges, Image for help icon, Tooltip for the image for the help icon
            TextBlock TBlock2 = new TextBlock() { Text = "Level Point Spread", FontSize = GlobalFontSize, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };

            ComboBox CB1 = new ComboBox();
            for (int i = comboBoxMin; i <= comboBoxMax; i++) //populating combobox
            {
                CB1.Items.Add(i);
            }
            CB1.SelectedItem = CB1.Items[0];

            Image Img2 = new Image() { Source = helpIconSource, Height = 16, Width = 16 };
            TextBlock ToolTipTB = new TextBlock();
            ToolTipTB.Text = "The point spread determines the minimum and\n maximum point range for a quality level.\n The system starts at 1 in the first column\n and determines the starting point of other\n columns based on your previous input.";
            ToolTipService.SetToolTip(Img2, ToolTipTB);
            
            //margins
            
            TBlock2.Margin = generalMargin;
            CB1.Margin = generalMargin;

            //adding children to SP2
            SP2.Children.Add(TBlock2);
            SP2.Children.Add(CB1);
            SP2.Children.Add(Img2);

            returnVal.Children.Add(SP1);
            returnVal.Children.Add(SP2);
            return returnVal;
        }

        public Rubric GetView()
        {
            return thisView;
        }

        #region Events

        private void Delete_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            removeRow();
        }
        private void DeleteCol_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            removeCol();
        }

        //all the following are temp events
        private void TBlock1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            createCriterionRow();
        }

        private void Img1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            createLevelColumn();
        }
        private void Img2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            removeCol();
            //e.GetPosition(thisView);
            /*
            var eles = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(thisView), thisView.LayoutRoot);
            foreach (UIElement rd in eles)
            {
                if (rd is Grid)
                {
                    //(rd as Grid).RowDefinitions
                }
            }
            */
            //VisualTreeHelper.

        }
        void TBlock2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            myGrid.BaseGrid.ColumnDefinitions.RemoveAt(2);
           // myGrid.BaseGrid.ColumnDefinitions.RemoveAt(myGrid.BaseGrid.ColumnDefinitions.Count - 1);
        }
        #endregion
    }
}
