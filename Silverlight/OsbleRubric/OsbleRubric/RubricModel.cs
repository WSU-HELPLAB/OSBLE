﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChemProV.PFD.Streams.PropertiesWindow;
using System.Collections.Generic;

namespace OsbleRubric
{
    public class RubricModel
    {
        #region Attributes

        private CustomDataGrid customDataGrid = new CustomDataGrid();
        private Rubric thisView = new Rubric();

        #endregion Attributes

        #region constants

        const double Col0Width = 180;
        const double Row0Height = 100;
        const double ColWidth = 250;
        const double RowHeight = 138;
        const double Col1Width = 150;
        const int GlobalFontSize = 12;
        const double finalRowHeight = 32;

        const double addColColumnWidth = 32;
        const double commentColumnWidth = 150;


        private ImageSource helpIconSource = new BitmapImage(new Uri("Icons/help.png", UriKind.Relative));
        private ImageSource deleteIconSource = new BitmapImage(new Uri("Icons/delete.png", UriKind.Relative));
        private ImageSource addIconSource = new BitmapImage(new Uri("Icons/add.png", UriKind.Relative));

        #endregion constants

        public RubricModel()
        {
            initialize();
        }

        //sets up all the default paramaters for the grid
        private void initialize()
        {
            //adding grid to the view
            thisView.LayoutRoot.Children.Add(customDataGrid.BaseGrid);

            //setting color for grid
            customDataGrid.BaseGrid.Background = new SolidColorBrush(Colors.LightGray);

            //setting borders for grid
            customDataGrid.HideBordersForLastRow = false;
            customDataGrid.HideBordersForLastTwoColumns = false;
            customDataGrid.LastRowAsTwoCells = true;
            customDataGrid.BorderBrush = new SolidColorBrush(Colors.Black);
            customDataGrid.BorderThickness = 2.0;

            //adding initial columns & rows
            customDataGrid.PlaceUIElement(createCol0Row0(), 0, 0);
            customDataGrid.PlaceUIElement(createCol1Row0(), 1, 0);
            customDataGrid.PlaceUIElement(createLevelTitleCell(), 2, 0);
            //
            createAddButtonColumn();
            createCommentColumn();
            createCriterionRow();
            createAddButtonRow();
            

            //setting widths/heights for the table - possibly best to set width/height somewhere else
            //setting heights for rows
            customDataGrid.BaseGrid.RowDefinitions[0].Height = new GridLength(Row0Height);
            for (int i = 1; i < customDataGrid.BaseGrid.RowDefinitions.Count-1; ++i)
            {
                customDataGrid.BaseGrid.RowDefinitions[i].Height = new GridLength(RowHeight);
            }
            //customDataGrid.BaseGrid.RowDefinitions[customDataGrid.BaseGrid.RowDefinitions.Count - 1].Height = new GridLength(finalRowHeight);

            //setting widths for columns
            customDataGrid.BaseGrid.ColumnDefinitions[0].Width = new GridLength(Col0Width);
            customDataGrid.BaseGrid.ColumnDefinitions[1].Width = new GridLength(Col1Width);
            for (int i = 2; i < customDataGrid.BaseGrid.ColumnDefinitions.Count - 2; ++i)
            {
                customDataGrid.BaseGrid.ColumnDefinitions[i].Width = new GridLength(ColWidth);
            }

            //sets up initial tabIndexes
            setTabIndex();
        }

        //This method creates the comment column and adds it to the grid (last column)
        private void createCommentColumn()
        {
            StackPanel stackPanel = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 5, 5, 5),
                Orientation = Orientation.Horizontal,
                
            };

            CheckBox commentsOn = new CheckBox()
            {
                IsChecked = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsTabStop = true,
            };
            TextBlock commentsTB = new TextBlock()
            {
                Text = "Comments",
                FontWeight = FontWeights.Bold,
                FontSize = GlobalFontSize,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            Image helpIcon = new Image()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Source = helpIconSource,
                Height = 16,
                Width = 16,
            };
            ToolTipService.SetToolTip(helpIcon, "This checkbox determines if a column is included in the final table");

            stackPanel.Children.Add(commentsOn);
            stackPanel.Children.Add(commentsTB);
            stackPanel.Children.Add(helpIcon);

            int columnToPlace = customDataGrid.BaseGrid.ColumnDefinitions.Count;
            customDataGrid.PlaceUIElement(stackPanel, columnToPlace, 0);

            for (int i = 1; i < customDataGrid.BaseGrid.RowDefinitions.Count - 1; ++i)
            {
                customDataGrid.PlaceUIElement(createCommentCell(), columnToPlace, i);
            }
        }

        //This method creates the UI elements to be inserted into a comment cell
        private TextBox createCommentCell()
        {
            TextBox commentBox = new TextBox()
            {
                Width = commentColumnWidth,
                Margin = new Thickness(5, 5, 5, 5),
                IsReadOnly = true,
                IsTabStop = false,
            };
            return commentBox;
        }

        //This method creates the add button column and adds it to the grid (second to last column)
        private void createAddButtonColumn()
        {
            //stack Panel to be added
            StackPanel stackPanel = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = addColColumnWidth,
                Margin = new Thickness(5, 5, 5, 5)
            };
            //image to be added to stackPanel
            Image addIcon = new Image()
            {
                Source = addIconSource,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 32,
                Width = 32
            };

            //adding the image and setting up its event
            stackPanel.Children.Add(addIcon);
            addIcon.MouseLeftButtonDown += new MouseButtonEventHandler(addCol_MouseLeftButtonDown);

            //adding column into grid
            customDataGrid.PlaceUIElement(stackPanel, customDataGrid.BaseGrid.ColumnDefinitions.Count , 0);
        }

        //This method creates the add button row and adds it to the bottom of the grid
        private void createAddButtonRow()
        {
            //stack Panel to be added
            StackPanel stackPanel = new StackPanel() 
            { 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = ColWidth,
                Height = finalRowHeight,
                Margin = new Thickness(5, 5, 5, 5)
            };
            //image to be added to stackPanel
            Image addIcon = new Image() 
            { 
                Source = addIconSource, 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 32,
                Width = 32 
            };

            //adding the image and setting up its event
            stackPanel.Children.Add(addIcon);
            addIcon.MouseLeftButtonDown += new MouseButtonEventHandler(addRow_MouseLeftButtonDown);

            //textblock for bottom row
            TextBlock pointsPossible_TextBlock = new TextBlock()
            {
                Text = ("Points Possible: " + calcTotalPoints().ToString()),
                FontSize = GlobalFontSize,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5,5,5,5)
            };

            //setting the textblock to span all remaining columns
            pointsPossible_TextBlock.SetValue(Grid.ColumnSpanProperty, customDataGrid.BaseGrid.ColumnDefinitions.Count - 1);

            //adding UIelements into the grid
            int rowToAdd = customDataGrid.BaseGrid.RowDefinitions.Count;
            customDataGrid.PlaceUIElement(stackPanel, 0, rowToAdd);
            customDataGrid.PlaceUIElement(pointsPossible_TextBlock, 1, rowToAdd);
        }

        //This method creates and correctly populates an entire column, and then adds it to the grid
        private void createLevelColumn()
        {
            int colIndexToInsert = customDataGrid.BaseGrid.ColumnDefinitions.Count;

            //adding the top row cell
            customDataGrid.PlaceUIElement(createLevelTitleCell(), colIndexToInsert, 0);

            //adding the rest of the cells
            for (int i = 1; i < customDataGrid.BaseGrid.RowDefinitions.Count-1; i++)
            {
                customDataGrid.PlaceUIElement(createLevelCell(), colIndexToInsert, i);
            }
        }

        //This method creates and correctly populates an entire row, and then adds it to the grid
        private void createCriterionRow()
        {
            int rowPlacement = customDataGrid.BaseGrid.RowDefinitions.Count;

            //adding first two column cells
            customDataGrid.PlaceUIElement(createPerformanceCritCell(), 0, rowPlacement);
            customDataGrid.PlaceUIElement(createCritWeightCell(), 1, rowPlacement);

            //adding the rest of the cells 
            for (int i = 2; i < customDataGrid.BaseGrid.ColumnDefinitions.Count-2; i++)
            {
                customDataGrid.PlaceUIElement(createLevelCell(), i, rowPlacement);
            }

            //adding comment cell
            customDataGrid.PlaceUIElement(createCommentCell(), customDataGrid.BaseGrid.ColumnDefinitions.Count - 1, rowPlacement);
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
                Height = RowHeight - 12,
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

            textbox.LostFocus += new RoutedEventHandler(textbox_LostFocus);

            TextBlock textBlock = new TextBlock() { Text = "Points", Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            returnVal.Children.Add(textbox);
            returnVal.Children.Add(textBlock);

            return returnVal;
        }

        void textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            updateLastRow();
        }

        //This method creates the cells used for the Performance Criterion column (With the exception of the first row)
        private StackPanel createPerformanceCritCell()
        {
            StackPanel returnVal = new StackPanel()
            {
                Margin = new Thickness(5, 5, 5, 5)
            };

            TextBox textbox = new TextBox()
            {
                Height = 90,
                Width = returnVal.Width,
                TextWrapping = TextWrapping.Wrap
            };
            Image deleteIcon = new Image { Source = deleteIconSource, Height = 32, Width = 32, Margin = new Thickness(0, 3, 0, 0) };
            deleteIcon.MouseLeftButtonDown += new MouseButtonEventHandler(DeleteRow_MouseLeftButtonDown);

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

            TBlock1.MouseLeftButtonDown += new MouseButtonEventHandler(TBlock1_MouseLeftButtonDown);

            //adding children to returnVal
            returnVal.Children.Add(TBlock1);
            returnVal.Children.Add(Img1);
            return returnVal;
        }

        void TBlock1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            performSwap(2, 3);
        }

        struct postitionUIElement
        {
            UIElement uiele;
            int row;
            int col;
        }
        private void performSwap(int row1, int row2)
        {
            MessageBox.Show("IM IN");
            List<UIElement> borderList1 = new List<UIElement>();
            List<UIElement> borderList2 = new List<UIElement>();
            foreach (Border br in customDataGrid.BaseGrid.Children) //removing any borders from the grid and putting them in a list
            {
                int row = (int)br.GetValue(Grid.RowProperty);
                int col = (int)br.GetValue(Grid.ColumnProperty);
                if (row == row1)
                {
                    if (br.Child != null)
                    {
                        (br.Child).SetValue(Grid.ColumnProperty, col);
                        borderList1.Add(br.Child);
                        customDataGrid.RemoveUIElement(br.Child);
                    }
                    
                }
                if (row == row2)
                {
                    if (br.Child != null)
                    {
                        (br.Child).SetValue(Grid.ColumnProperty, col);
                        borderList2.Add(br.Child);
                        customDataGrid.RemoveUIElement(br.Child);
                    }
                }
            }

            
            foreach (UIElement br in borderList1)
            {
                if (br != null)
                {
                    br.SetValue(Grid.RowProperty, row2);
                    customDataGrid.PlaceUIElement(br, (int)br.GetValue(Grid.ColumnProperty), row2);
                }
            }
            foreach (UIElement br in borderList2)
            {
                if (br != null)
                {
                    br.SetValue(Grid.RowProperty, row1);
                    customDataGrid.PlaceUIElement(br, (int)br.GetValue(Grid.ColumnProperty), row1);
                }
            }
        }

        //This method creates the top row for the Weight Criterion column
        private StackPanel createCol1Row0()
        {
            StackPanel returnVal = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };

            //stuff for returnVal
            TextBlock TBlock2 = new TextBlock() { Text = "Criterion Weight", FontWeight = FontWeights.Bold, FontSize = GlobalFontSize, VerticalAlignment = VerticalAlignment.Center };
            Image Img2 = new Image() { Source = helpIconSource, Width = 16, Height = 16, Margin = new Thickness(3, 0, 0, 0) };
            ToolTipService.SetToolTip(Img2, "The criterion weight column is used to set the\nweights of each criterion row. By default the\nweight is evenly distrbuted among the criterion.\nTo set the weight simply input a number between\n1 and 100 in the column below. The weight is\nautomatically recalculated upon input.");

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

            Image Img1 = new Image() { Source = deleteIconSource, Height = 32, Width = 32 };
            Img1.MouseLeftButtonDown += new MouseButtonEventHandler(DeleteCol_MouseLeftButtonDown);

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

            //setting tabindex for controls
            TBox1.TabIndex = 0;
            CB1.TabIndex = 0;

            returnVal.Children.Add(SP1);
            returnVal.Children.Add(SP2);
            return returnVal;
        }

        //returns the row of a MouseEventArgs. If there is no row -1
        private int getRow(MouseEventArgs e)
        {
            int returnVal = -1;
            var eles = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(thisView), thisView.LayoutRoot);
            foreach (FrameworkElement rd in eles)
            {
                if (rd is Border)
                {
                    returnVal = Grid.GetRow(rd);
                }
            }
            return returnVal;
        }

        //returns the column of a MouseEventArgs. If there is no column -1
        private int getColumn(MouseEventArgs e)
        {
            int returnVal = -1;
            var eles = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(thisView), thisView.LayoutRoot);
            foreach (FrameworkElement rd in eles)
            {
                if (rd is Border)
                {
                    returnVal = Grid.GetColumn(rd);
                }
            }
            return returnVal;
        }

        //This method deletes the TextBlock in the last row and recreates it. This is the only way I could find to keep its position correct. (Adjusting its columnspan property did not work)
        private void updateLastRow()
        {
            foreach (Border br in customDataGrid.BaseGrid.Children)
            {
                if (br.Child is TextBlock)
                {
                    int row = (int)br.GetValue(Grid.RowProperty);
                    if (row == (customDataGrid.BaseGrid.RowDefinitions.Count - 1)) //last row && a textbox (only 1 textbox in last row)
                    {
                        TextBlock temp = new TextBlock()
                        {
                            Text = ("Points Possible: " + calcTotalPoints().ToString()),
                            FontSize = (br.Child as TextBlock).FontSize,
                            FontWeight = (br.Child as TextBlock).FontWeight,
                            HorizontalAlignment = (br.Child as TextBlock).HorizontalAlignment,
                            VerticalAlignment = (br.Child as TextBlock).VerticalAlignment,
                            Margin = (br.Child as TextBlock).Margin
                        };
                        temp.SetValue(Grid.ColumnSpanProperty, customDataGrid.BaseGrid.ColumnDefinitions.Count - 1);
                        customDataGrid.RemoveUIElement(br.Child as TextBlock);
                        customDataGrid.PlaceUIElement(temp, 1, customDataGrid.BaseGrid.RowDefinitions.Count - 1);
                    }
                }
            }
        }

        //This method sets the tab indexes for each cell (excluding the first row, which is already set up)
        private void setTabIndex()
        {
            foreach (Border k in customDataGrid.BaseGrid.Children)
            {
                int row = (int)k.GetValue(Grid.RowProperty);
                int col = (int)k.GetValue(Grid.ColumnProperty);
                if (k.Child is StackPanel)
                {
                    foreach (UIElement tb in (k.Child as StackPanel).Children)
                    {
                        if (tb is Control)
                        {
                            //we want tabs to go from left to right, and then down. So to do this, we should make tab index:
                            //row*10 + column. This will produce an effect of 0,1,2,3,4,5 for the first row, 10,11,12... for the second and so on
                            (tb as Control).TabIndex = row * 100 + col;
                            
                        }
                    }
                }
            }
        }

        /*This function calculates the total points by summing all the values from the point boxes (in the second column) 
         *If the value is "" or contains non-number value, then it will not be included*/
        private int calcTotalPoints()
        {
            int returnVal = 0;
            foreach (Border br in customDataGrid.BaseGrid.Children) 
            {
                int col = (int)br.GetValue(Grid.ColumnProperty);
                /*The textboxes are contained in a stackpanel in column 2 (1 when considering 0 offset),
                 * so any Textboxes in this field are the ones we want to sum*/
                if (br.Child is StackPanel && col == 1) 
                {
                    foreach (UIElement tb in (br.Child as StackPanel).Children)
                    {
                        if (tb is TextBox)
                        {
                            int temp;
                            if (int.TryParse((tb as TextBox).Text, out temp))
                            {
                                returnVal += temp;
                            }   
                        }
                    }
                }
            }
            return returnVal;
        }

        //Returns the view
        public Rubric GetView()
        {
            return thisView;
        }

        #region Events

        private void DeleteRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (customDataGrid.BaseGrid.RowDefinitions.Count != 3) //won't remove if its the only row
            {
                customDataGrid.RemoveRow(getRow(e));
                updateLastRow();
            }   
        }
        private void DeleteCol_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (customDataGrid.BaseGrid.ColumnDefinitions.Count != 5) //won't remove if its the only column
            {
                customDataGrid.RemoveColumn(getColumn(e));
            }
        }
        private void addCol_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //removes last two columns (add button column & comment column), creates the new level column, and recreates the addbutton/comment column
            
            customDataGrid.RemoveColumn(customDataGrid.BaseGrid.ColumnDefinitions.Count - 1);
            customDataGrid.RemoveColumn(customDataGrid.BaseGrid.ColumnDefinitions.Count - 1);

            createLevelColumn();
            createAddButtonColumn();
            createCommentColumn();
            updateLastRow();
            setTabIndex();
            
        }
        private void addRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //removes the last row (the add button), then adds the new criterion row, then adds the add button row back in
            customDataGrid.RemoveRow(customDataGrid.BaseGrid.RowDefinitions.Count - 1);
            createCriterionRow();
            createAddButtonRow();
            setTabIndex();
        }

        #endregion Events
    }
}