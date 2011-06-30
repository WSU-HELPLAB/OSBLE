using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Browser;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using OsbleRubric.OsbleServices;

namespace OsbleRubric
{
    public class RubricViewModel
    {
        #region Attributes

        private CustomDataGrid customDataGrid = new CustomDataGrid();
        private RubricView thisView = new RubricView();
        private List<ICell> dataFromCells = new List<ICell>();
        public enum CheckboxValues { ColumnComment, GlobalComment };

        #endregion Attributes

        #region constants

        const double Col0Width = 145;
        const double Row0Height = 70;
        const double ColWidth = 190;
        const double RowHeight = 122;
        const double Col1Width = 105;
        const int GlobalFontSize = 11;
        const double finalRowHeight = 22;

        const double addColColumnWidth = 22;
        const double commentColumnWidth = 120;

        private ImageSource helpIconSource = new BitmapImage(new Uri("Icons/help.png", UriKind.Relative));
        private ImageSource deleteIconSource = new BitmapImage(new Uri("Icons/delete.png", UriKind.Relative));
        private ImageSource addIconSource = new BitmapImage(new Uri("Icons/add.png", UriKind.Relative));
        private ImageSource downArrowIconSource = new BitmapImage(new Uri("Icons/downArrow.png", UriKind.Relative));
        private ImageSource upArrowIconSource = new BitmapImage(new Uri("Icons/upArrow.png", UriKind.Relative));
        private ImageSource grayUpArrowIconSource = new BitmapImage(new Uri("Icons/upArrowGrayedOut.png", UriKind.Relative));
        private ImageSource grayDownArrowIconSource = new BitmapImage(new Uri("Icons/downArrowGrayedOut.png", UriKind.Relative));

        #endregion constants

        /// <summary>
        /// Constructor
        /// </summary>
        public RubricViewModel()
        {
            initialize();
        }

        /// <summary>
        /// sets up all the default paramaters for the grid & adds the initial rows/columns
        /// </summary>
        private void initialize()
        {
            //adding grid to the view
            if (thisView.LayoutRoot.Children.Contains(customDataGrid.BaseGrid) == false) //won't try to add the grid if init is run again
            {
                thisView.LayoutRoot.Children.Add(customDataGrid.BaseGrid);
            }

            //setting color for grid
            customDataGrid.BaseGrid.Background = new SolidColorBrush(Colors.LightGray);

            //setting borders for grid
            customDataGrid.HideBordersForLastRow = false;
            customDataGrid.HideBordersForLastTwoColumns = false;
            customDataGrid.LastRowAsTwoCells = true;
            customDataGrid.BorderBrush = new SolidColorBrush(Colors.Black);
            customDataGrid.BorderThickness = 2.0;

            //adding initial columns & rows
            customDataGrid.PlaceUIElement(createPerformanceCritTitleCell(), 0, 0);
            customDataGrid.PlaceUIElement(createCritWeightTitleCell(), 1, 0);
            customDataGrid.PlaceUIElement(createLevelTitleCell(), 2, 0);
            createAddButtonColumn();
            createCommentColumn();
            createCriterionRow();
            createAddButtonRow();
            

            //setting widths/heights for the table
            //setting heights for rows
            customDataGrid.BaseGrid.RowDefinitions[0].Height = new GridLength(Row0Height);
            for (int i = 1; i < customDataGrid.BaseGrid.RowDefinitions.Count-1; ++i)
            {
                customDataGrid.BaseGrid.RowDefinitions[i].Height = new GridLength(RowHeight);
            }

            //setting widths for columns
            customDataGrid.BaseGrid.ColumnDefinitions[0].Width = new GridLength(Col0Width);
            customDataGrid.BaseGrid.ColumnDefinitions[1].Width = new GridLength(Col1Width);


            //sets up initial tabIndexes
            setTabIndex();

            //setting up the initial grayed arrows
            adjustArrowIcons();

            //attach event listeners to our view
            thisView.CancelChanges.Click += new RoutedEventHandler(CancelChanges_Click);
            thisView.PublishChanges.Click += new RoutedEventHandler(PublishChanges_Click);
        }

        /// <summary>
        /// attempts to place an ICell into the grid at the ICells given row/column/data. Returns false if no data was added
        /// </summary>
        /// <param name="cellToAdd"></param>
        /// <returns></returns>
        private bool placeICell(ICell cellToAdd)
        {
            bool returnVal = false;

            UIElement firstSP = customDataGrid.GetUIElementAt(cellToAdd.Column, cellToAdd.Row);
            if (firstSP is StackPanel) //all cells in the grid contain at least one stackpanel
            {
                //addressing each type of cell individually
                if (cellToAdd is RubricCell)  //RubricCells data will always be placed into textboxes, sometimes within nested stackpanels
                {
                    foreach (UIElement ui in (firstSP as StackPanel).Children)
                    {
                        if (ui is StackPanel) //handling nested StackPanels
                        {
                            foreach (UIElement ui2 in (ui as StackPanel).Children)
                            {
                                if (ui2 is TextBox) //Returning true since data was changed
                                {
                                    (ui2 as TextBox).Text = cellToAdd.Information;
                                    returnVal = true;
                                }
                            }
                        }
                        else if (ui is TextBox) //handling non-nested StackPanels. Returning true since data was changed
                        {
                            (ui as TextBox).Text = cellToAdd.Information;
                            returnVal = true;
                        }
                    }
                }
                else if (cellToAdd is CheckBoxCell) //CheckBoxCells data will always be placed into CheckBoxes inside of a stackpanel
                {
                    foreach (UIElement ui in (firstSP as StackPanel).Children)
                    {
                        if (ui is CheckBox) //returning true since data was changed
                        {
                            (ui as CheckBox).IsChecked = (cellToAdd as CheckBoxCell).CheckBoxValue;
                            returnVal = true;
                        }
                    }
                }
                else if (cellToAdd is HeaderCell) //HeaderCells data will be placed into stackpanels nested within firstSP. The first stackpanel contains the textbox, the second stackpanel contains the combobox
                {
                    bool textBoxAdded = false;
                    bool comboBoxAdded = false;
                    foreach (UIElement ui in (firstSP as StackPanel).Children) 
                    {
                        if (ui is StackPanel) //looking for the nested stackpanels
                        {

                            foreach (UIElement ui2 in (ui as StackPanel).Children) //looking through the nested stackpanels for a combobox or textbox
                            {
                                if (ui2 is TextBox)
                                {
                                    (ui2 as TextBox).Text = cellToAdd.Information;
                                    textBoxAdded = true;
                                }
                                else if (ui2 is ComboBox)
                                {
                                    (ui2 as ComboBox).SelectedItem = (cellToAdd as HeaderCell).ComboBoxValue;
                                    comboBoxAdded = true;
                                }
                            }
                        }
                    }
                    if (comboBoxAdded && textBoxAdded) //if the textbox and combobox are added, return true
                    {
                        returnVal = true;
                    }
                }
            }


            return returnVal;
        }
        
        /// <summary>
        ///  /// returns the number of rows from dataFromCells list
        /// </summary>
        /// <returns></returns>
        private int getRowCount()
        {
            int returnVal = 0;
            foreach (ICell ic in dataFromCells)
            {
                if (ic.Row > returnVal) returnVal = ic.Row;
            }
            return (returnVal+1); //+1 to account for the 0 offset
        }

        /// <summary>
        /// returns the number of columns from dataFromCells list
        /// </summary>
        /// <returns></returns>
        private int getColumnCount()
        {
            int returnVal = 0;
            foreach (ICell ic in dataFromCells)
            {
                if (ic.Column > returnVal) returnVal = ic.Column;
            }
            return (returnVal + 1); //+1 to account for the 0 offset
        }

        /// <summary>
        /// This method clears the grid and rebuilds it based off the data in dataFromCells
        /// </summary>
        private void buildGridFromData()
        {
            //getting the number of rows/columns
            int numOfRows = getRowCount();
            int numofCols = getColumnCount();

            customDataGrid.ClearAll();
            initialize();

            //adding empty rows
            customDataGrid.RemoveRow(customDataGrid.BaseGrid.RowDefinitions.Count - 1); //remove the add button row temporarily
            while (numOfRows > (customDataGrid.BaseGrid.RowDefinitions.Count + 1))//+1 to account for last rows
            {
                createCriterionRow();
            }
            createAddButtonRow(); //adding add button row back in

            //adding empty columns 
            customDataGrid.RemoveColumn(customDataGrid.BaseGrid.ColumnDefinitions.Count - 1); //removing two last columns temporarily
            customDataGrid.RemoveColumn(customDataGrid.BaseGrid.ColumnDefinitions.Count - 1);
            while (numofCols > (customDataGrid.BaseGrid.ColumnDefinitions.Count + 2)) //+2 to account for last two rows
            {
                createLevelColumn();
            }
            createAddButtonColumn(); //adding last two columns back
            createCommentColumn();

            //setting tabs, updating last rows textblock position, fixing/adjusting arrow icons
            setTabIndex();
            updateLastRow();
            changeDownArrow(1, downArrowIconSource);
            adjustArrowIcons();

            //taking data from dataFromCells and inserting it into the datagrid
            foreach (ICell ic in dataFromCells)
            {
                if (ic is HeaderCell || ic is CheckBoxCell || ic is RubricCell)
                {
                    placeICell(ic);
                }
            }
        }

        /// <summary>
        /// this method saves all the data from the cells into a list. This method will most probably need changes as changes are made within the grid
        /// </summary>
        private void getData()
        {
            dataFromCells.Clear(); //clear the list before appending to it

            foreach (Border br in customDataGrid.BaseGrid.Children)
            {
                int row = (int)br.GetValue(Grid.RowProperty);
                int col = (int)br.GetValue(Grid.ColumnProperty);
                if (row == 0) //All cells in row 0 are HeaderCells, except the one that is the last column, which is a CheckBoxCell
                {
                    if(br.Child is StackPanel) //if its child is not a stackpanel, don't bother looking at it
                    {
                        if (col != customDataGrid.BaseGrid.ColumnDefinitions.Count - 1) //if the column is not the last, must be a HeaderCell at this point
                        {
                            HeaderCell hc = new HeaderCell(row, col);
                            foreach (UIElement ui in (br.Child as StackPanel).Children) //look through children for textbox and combobox to save into a HeaderCell
                            {
                                if (ui is StackPanel) //looking for the nested stackpanel
                                {
                                    foreach (UIElement ui2 in (ui as StackPanel).Children) //looking for a textbox/combobox in the nested stackpanel, then adjusting hc based on those values
                                    {
                                        if (ui2 is TextBox)
                                        {
                                            hc.Information = (ui2 as TextBox).Text;
                                        }
                                        else if (ui2 is ComboBox)
                                        {
                                            hc.ComboBoxValue = Convert.ToInt32((ui2 as ComboBox).SelectedItem);
                                        }
                                    }
                                }
                            }
                            if (hc.ComboBoxValue != 0) //Only adding hc to dataFromCells if it got a valid combobox
                            {
                                dataFromCells.Add(hc);
                            }
                        }
                        else if (col == (customDataGrid.BaseGrid.ColumnDefinitions.Count - 1)) //if the column is the last column, must be CheckBoxCell
                        {
                            foreach (UIElement ui in (br.Child as StackPanel).Children) //look through the the stackpanel for a checkbox, add that to dataFromCells
                            {
                                if (ui is CheckBox) dataFromCells.Add(new CheckBoxCell(row, col, (bool)(ui as CheckBox).IsChecked) {Information = CheckboxValues.ColumnComment.ToString() });
                            }
                        }
                    }
                }
                else if (row == customDataGrid.BaseGrid.RowDefinitions.Count - 1) //Last row, should only have a single checkbox value
                {
                    if (br.Child is StackPanel && col == 1) //Only data(the checbox) is in a stackpanel in the first column
                    {
                        foreach (UIElement ui in (br.Child as StackPanel).Children)
                        {
                            if (ui is CheckBox) dataFromCells.Add(new CheckBoxCell(row, col, (bool)(ui as CheckBox).IsChecked) { Information = CheckboxValues.GlobalComment.ToString() });
                        }
                    }
                }
                else //The remaining cells will be between first and last row, all should be RubricCells 
                {
                    if (br.Child is StackPanel)//Only want to look through br.Child if its a stackpanel
                    {
                        foreach (UIElement ui in (br.Child as StackPanel).Children) //look through children
                        {
                            if (ui is TextBox) //if its a textbox, add to dataFromCells via a new RubricCell
                            {
                                dataFromCells.Add(new RubricCell(row, col, (ui as TextBox).Text));
                            }
                            else if (ui is StackPanel) //if its a stackpanel, look through the stackpanels children, add any new textboxes to dataFromCells via a new RubricCell
                            {
                                foreach (UIElement ui2 in (ui as StackPanel).Children)
                                {
                                    if (ui2 is TextBox) dataFromCells.Add(new RubricCell(row, col, (ui2 as TextBox).Text));
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method creates the comment column and adds it to the grid (last column)
        /// </summary>
        private void createCommentColumn()
        {
            StackPanel stackPanel = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 5, 5, 5),
                Orientation = Orientation.Horizontal,
                
            };

            CheckBox commentsOnCheckBox = new CheckBox()
            {
                IsChecked = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsTabStop = true,
            };
            TextBlock commentsTextBlock = new TextBlock()
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
                Margin = new Thickness(5, 0,0,0)
            };
            ToolTipService.SetToolTip(helpIcon, "This checkbox determines if a column for comments is included in the final rubric");

            stackPanel.Children.Add(commentsOnCheckBox);
            stackPanel.Children.Add(commentsTextBlock);
            stackPanel.Children.Add(helpIcon);

            int columnToPlace = customDataGrid.BaseGrid.ColumnDefinitions.Count;
            customDataGrid.PlaceUIElement(stackPanel, columnToPlace, 0);

            for (int i = 1; i < customDataGrid.BaseGrid.RowDefinitions.Count - 1; ++i)
            {
                customDataGrid.PlaceUIElement(createCommentCell(), columnToPlace, i);
            }
        }

        /// <summary>
        /// This method creates the UI elements to be inserted into a comment cell
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// This method creates the add button column and adds it to the grid (second to last column)
        /// </summary>
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
            //image to be added to addIconSP
            Image addIcon = new Image()
            {
                Source = addIconSource,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22,
                Width = 22
            };

            //setting tooltip for addIcon
            ToolTipService.SetToolTip(addIcon, "Click to add a new column");

            //adding the image and setting up its event
            stackPanel.Children.Add(addIcon);
            addIcon.MouseLeftButtonDown += new MouseButtonEventHandler(addCol_MouseLeftButtonDown);

            //adding column into grid
            customDataGrid.PlaceUIElement(stackPanel, customDataGrid.BaseGrid.ColumnDefinitions.Count , 0);
        }

        /// <summary>
        /// This method creates the add button row and adds it to the bottom of the grid
        /// </summary>
        private void createAddButtonRow()
        {
            //stack Panel to be added to first column
            StackPanel addIconSP = new StackPanel() 
            { 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = ColWidth,
                Height = finalRowHeight,
                Margin = new Thickness(5, 5, 5, 5)
            };
            //image to be added to addIconSP
            Image addIcon = new Image() 
            { 
                Source = addIconSource, 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22,
                Width = 22 
            };

            //setting tooltip for addIcon
            ToolTipService.SetToolTip(addIcon, "Click to add a new row");

            //adding the image and setting up its event
            addIconSP.Children.Add(addIcon);
            addIcon.MouseLeftButtonDown += new MouseButtonEventHandler(addRow_MouseLeftButtonDown);

            //stackpanel for textblock/checkbox
            StackPanel checkBoxSP = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5,5,5,5),
                Orientation = Orientation.Horizontal
            };
            //setting the stackpanel to stretch a specific gridlength
            checkBoxSP.SetValue(Grid.ColumnSpanProperty, 2);

            //creating/formating checkbox & textblock to be added to checkBoxSP
            CheckBox globalComments = new CheckBox()
            {
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Center,
            };

            TextBlock globalCommentsTB = new TextBlock()
            {
                Text = "Enable Global Comments",
                FontSize = GlobalFontSize,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(1,0,0,0)
            };

            Image helpIcon = new Image()
            {
                Source = helpIconSource,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            ToolTipService.SetToolTip(helpIcon, "This checkbox determines if a comment block for the\nentire rubric will be included in the final rubric.");


            //addeding checkbox & textblock to checkBoxSP
            checkBoxSP.Children.Add(globalComments);
            checkBoxSP.Children.Add(globalCommentsTB);
            checkBoxSP.Children.Add(helpIcon);

            //textblock for points possible
            TextBlock pointsPossible_TextBlock = new TextBlock()
            {
                Text = totalPointsToString(),
                FontSize = GlobalFontSize,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5,5,5,5)
            };
            //setting the textblock to span two columns
            pointsPossible_TextBlock.SetValue(Grid.ColumnSpanProperty, 2);


            //adding UIelements into the grid
            int rowToAdd = customDataGrid.BaseGrid.RowDefinitions.Count;
            customDataGrid.PlaceUIElement(addIconSP, 0, rowToAdd);
            customDataGrid.PlaceUIElement(checkBoxSP, 1, rowToAdd);
            customDataGrid.PlaceUIElement(pointsPossible_TextBlock,  customDataGrid.BaseGrid.ColumnDefinitions.Count-2, rowToAdd);

            
        }

        /// <summary>
        /// This method creates and correctly populates an entire column, and then adds it to the grid
        /// </summary>
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

        /// <summary>
        /// This method creates and correctly populates an entire row, and then adds it to the grid
        /// </summary>
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

        /// <summary>
        /// This method creates the cells used for a Level column (With the exception of the first row)
        /// </summary>
        /// <returns></returns>
        private StackPanel createLevelCell()
        {
            StackPanel returnVal = new StackPanel()
            {
                Margin = new Thickness(5, 5, 5, 5),
                Width = ColWidth,
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

        /// <summary>
        /// This method creates the cells used for the Criterion Weight column (With the exception of the first row)
        /// </summary>
        /// <returns></returns>
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
                Width = 55,
                Height = 22
            };

            //sets the event for each textbox to update the overall points
            textbox.SelectionChanged += new RoutedEventHandler(textbox_SelectionChanged);

            TextBlock textBlock = new TextBlock() { Text = "Points", Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            returnVal.Children.Add(textbox);
            returnVal.Children.Add(textBlock);

            return returnVal;
        }

        /// <summary>
        /// This method creates the cells used for the Performance Criterion column (With the exception of the first row)
        /// </summary>
        /// <returns></returns>
        private StackPanel createPerformanceCritCell()
        {

            //Overall StackPanel to return
            StackPanel returnVal = new StackPanel()
            {
                Margin = new Thickness(5, 5, 5, 5),
                Orientation = Orientation.Horizontal,
                Width = Col0Width
            };

            //vert stackpanel for the textbox/delete icon
            StackPanel textBoxAndDelete = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBox textbox = new TextBox() { Height = 85, Width = (Col0Width - 35), TextWrapping = TextWrapping.Wrap };
            Image deleteIcon = new Image { Source = deleteIconSource, Height = 22, Width = 22, Margin = new Thickness(0, 3, 17, 0), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            //tooltip for deleteIcon
            ToolTipService.SetToolTip(deleteIcon, "Click to delete the entire row");

            //events for deleteIcon
            deleteIcon.MouseLeftButtonDown += new MouseButtonEventHandler(DeleteRow_MouseLeftButtonDown);

            //adding children to textBoxAndDelete stackpanel
            textBoxAndDelete.Children.Add(textbox);
            textBoxAndDelete.Children.Add(deleteIcon);

            //Vert Stackpanel for up/down arrow images
            StackPanel arrowsStackPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0,0,5,0),
            };
            //up/down arrows to be added to arrowStackPanel
            Image upArrow = new Image { Source = upArrowIconSource, Width = 15, Height = 17, Margin = new Thickness(0, 0, 0, 5) };
            Image downArrow = new Image { Source = downArrowIconSource, Width = 15, Height = 17};

            //tying events for up and down arrow
            upArrow.MouseLeftButtonDown += new MouseButtonEventHandler(upArrow_MouseLeftButtonDown);
            downArrow.MouseLeftButtonDown += new MouseButtonEventHandler(downArrow_MouseLeftButtonDown);

            //tooltips for up and down arrows
            ToolTipService.SetToolTip(upArrow, "Click to move the row up a level");
            ToolTipService.SetToolTip(downArrow, "Click to move the row down a level");

            //adding children to arrowsStackPanel stackpanel
            arrowsStackPanel.Children.Add(upArrow);
            arrowsStackPanel.Children.Add(downArrow);

            //adding stackpanels to returnVal stackpanel
            returnVal.Children.Add(arrowsStackPanel);
            returnVal.Children.Add(textBoxAndDelete);

            return returnVal;
        }

        /// <summary>
        /// This method creates the top row for the Performance Criterion column
        /// </summary>
        /// <returns></returns>
        private StackPanel createPerformanceCritTitleCell()
        {
            StackPanel returnVal = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };

            //stuff for returnVal
            TextBlock TBlock1 = new TextBlock() { Text = "Performance\n   Criterion", FontWeight = FontWeights.Bold, FontSize = GlobalFontSize, VerticalAlignment = VerticalAlignment.Center };
            Image helpIconImg = new Image() { Source = helpIconSource, Width = 16, Height = 16, Margin = new Thickness(3, 0, 0, 0) };
            ToolTipService.SetToolTip(helpIconImg, "Input your Performance Criterion in the text area.\nUse the plus and minus buttons to add and remove criterion rows.");

            //adding children to returnVal
            returnVal.Children.Add(TBlock1);
            returnVal.Children.Add(helpIconImg);
            return returnVal;
        }

        /// <summary>
        /// This method swaps row1 with row2 if its a valid swap
        /// </summary>
        /// <param name="row1"></param>
        /// <param name="row2"></param>
        private void performSwap(int row1, int row2)
        {
            
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

        /// <summary>
        /// //This method creates the top row for the Weight Criterion column
        /// </summary>
        /// <returns></returns>
        private StackPanel createCritWeightTitleCell()
        {
            StackPanel returnVal = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };

            //stuff for returnVal
            TextBlock TBlock2 = new TextBlock() { Text = "Criterion\n Weight", FontWeight = FontWeights.Bold, FontSize = GlobalFontSize, VerticalAlignment = VerticalAlignment.Center };
            Image Img2 = new Image() { Source = helpIconSource, Width = 16, Height = 16, Margin = new Thickness(3, 0, 0, 0) };
            //ToolTipService.SetToolTip(Img2, "The criterion weight column is used to set the\nweights of each criterion row. By default the\nweight is evenly distrbuted among the criterion.\nTo set the weight simply input a number between\n1 and 100 in the column below. The weight is\nautomatically recalculated upon input.");
            ToolTipService.SetToolTip(Img2, "The criterion weight column is used to set the weight\nof each criterion row. To set the weight input a number\ninto the column below. The weight of that row will then\nbe the rows point value divided by the overall points\npossible (Shown at the bottom right of the table)");


            //adding children to returnVal
            returnVal.Children.Add(TBlock2);
            returnVal.Children.Add(Img2);
            return returnVal;
        }

        /// <summary>
        /// This method is used to create the top row for a Level column. It adds and formats all the appropriate UIelements
        /// </summary>
        /// <returns></returns>
        private StackPanel createLevelTitleCell()
        {
            Thickness generalMargin = new Thickness(5, 0, 5, 0); //marign used for most controls in the returned stackpanel for formatting

            int comboBoxMin = 1; //range of values for combobox
            int comboBoxMax = 10;

            StackPanel returnVal = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            //first stackpanel
            StackPanel topStackPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            
            //Textblock to act as label
            TextBlock levelTitleTextBlock = new TextBlock()
            {
                Text = "Level Title",
                FontWeight = FontWeights.Bold,
                FontSize = GlobalFontSize,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5,0,5,0),
            };

            //Textbox for user to enter level title
            TextBox levelTitleTextBox = new TextBox()
            {
                Width = 82,
                Height = 22,
                Margin = new Thickness(5, 0, 5, 0),
            };
            
            //deleteImage to act as delete column button
            Image deleteImage = new Image()
            { 
                Source = deleteIconSource, 
                Height = 22, 
                Width = 22,
                Margin = new Thickness(5,0,5,0)
            };
            //setting tooltip and event for deleteImage
            ToolTipService.SetToolTip(deleteImage, "Click to delete the entire column");
            deleteImage.MouseLeftButtonDown += new MouseButtonEventHandler(DeleteCol_MouseLeftButtonDown);

            //adding children to topStackPanel
            topStackPanel.Children.Add(levelTitleTextBlock);
            topStackPanel.Children.Add(levelTitleTextBox);
            topStackPanel.Children.Add(deleteImage);

            //second stackpanel
            StackPanel bottomStackPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0,5,0,0),
            };
            
            //textblock to act as label for pointSpreadComboBox
            TextBlock pointSpreadTextBlock = new TextBlock() 
            { 
                Text = "Level Point Spread",
                FontSize = GlobalFontSize, 
                FontWeight = FontWeights.Bold, 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5,0,5,0),
            };

            //Created a combobox and populating it with a range of values
            ComboBox pointSpreadComboBox = new ComboBox()
            {
                Margin = new Thickness(5, 0, 5, 0)
            };
            for (int i = comboBoxMin; i <= comboBoxMax; i++)
            {
                pointSpreadComboBox.Items.Add(i);
            }
            //setting initial selected item to first item added 
            pointSpreadComboBox.SelectedItem = pointSpreadComboBox.Items[0];

            //creating help icon and setting its tooltip
            Image helpImage = new Image() 
            { 
                Source = helpIconSource, 
                Height = 16, 
                Width = 16 
            };
            ToolTipService.SetToolTip(helpImage, "The point spread determines the minimum and\n maximum point range for a quality level.\n The system starts at 1 in the first column\n and determines the starting point of other\n columns based on your previous input.");


            //adding children to bottomStackPanel
            bottomStackPanel.Children.Add(pointSpreadTextBlock);
            bottomStackPanel.Children.Add(pointSpreadComboBox);
            bottomStackPanel.Children.Add(helpImage);

            //setting tabindex for controls
            levelTitleTextBox.TabIndex = 0;
            pointSpreadComboBox.TabIndex = 0;

            //adding top and bottom stackpanels to overall stackpanel
            returnVal.Children.Add(topStackPanel);
            returnVal.Children.Add(bottomStackPanel);
            return returnVal;
        }

        /// <summary>
        /// returns the row of a MouseEventArgs. If there is no row -1
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private int getRow(MouseEventArgs e)
        {
            int returnVal = -1;
            //var eles = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(thisView), thisView.LayoutRoot);
            var eles = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(Application.Current.RootVisual), thisView);
            foreach (FrameworkElement rd in eles)
            {
                if (rd is Border)
                {
                    //returnVal = Grid.GetRow(rd);
                    return Grid.GetRow(rd);
                }
            }
            return returnVal;
        }

        /// <summary>
        /// returns the column of a MouseEventArgs. If there is no column -1
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private int getColumn(MouseEventArgs e)
        {
            int returnVal = -1;
            
            //1- var eles = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(thisView), customDataGrid.BaseGrid);
            var eles = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(Application.Current.RootVisual), thisView);
            foreach (FrameworkElement rd in eles)
            {
                if (rd is Border) //Looking at the borders only, since all other elements are referenced by their borders index
                {
                    return Grid.GetColumn(rd);
                }
            }
            return returnVal;
        }

        /// <summary>
        /// Updates the position and points for the "Points possible" textblock
        /// </summary>
        private void updateLastRow()
        {
            //checks for the old textblock for "Possible Points " textblock. If it exists, adjust its text, if it has been deleted during column deletion, recreate with same attributes and place it
            UIElement oldTB;
            oldTB = customDataGrid.GetUIElementAt(customDataGrid.BaseGrid.ColumnDefinitions.Count - 2, customDataGrid.BaseGrid.RowDefinitions.Count - 1);
            if (oldTB is TextBlock)
            {
                (oldTB as TextBlock).Text = totalPointsToString();
            }
            else //recreating textblock
            {
                TextBlock pointsPossible_TextBlock = new TextBlock()
                {
                    Text = totalPointsToString(),
                    FontSize = GlobalFontSize,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                customDataGrid.PlaceUIElement(pointsPossible_TextBlock, customDataGrid.BaseGrid.ColumnDefinitions.Count - 1, customDataGrid.BaseGrid.RowDefinitions.Count - 1);
            }
        }

        /// <summary>
        /// This method sets the tab indexes for each cell (excluding the first row, which is already set up)
        /// </summary>
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
                        if (tb is StackPanel) //checking for nested stackpanels
                        {
                            foreach (UIElement tb2 in (tb as StackPanel).Children) 
                            {
                                if (tb2 is Control)
                                {
                                    (tb2 as Control).TabIndex = row * 100 + col;
                                }
                            }
                        }

                        else if (tb is Control)
                        {
                            //we want tabs to go from left to right, and then down. So to do this, we should make tab index:
                            //row*10 + column. This will produce an effect of 0,1,2,3,4,5 for the first row, 10,11,12... for the second and so on
                            (tb as Control).TabIndex = row * 100 + col;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// this method returns the string to be displayed for the points possible textbox
        /// </summary>
        /// <returns></returns>
        private string totalPointsToString()
        {
            string returnVal;
            returnVal = "Points Possible: " + calcTotalPoints().ToString();
            return returnVal;
        }

        /// <summary>
        /// This method calculates the total points by summing all the values from the point boxes (in the second column). If the value is "" or contains non-number value, then it will not be included
        /// </summary>
        /// <returns></returns>
        private double calcTotalPoints()
        {
            double returnVal = 0;
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
                            double temp;
                            if (double.TryParse((tb as TextBox).Text, out temp))
                            {
                                returnVal += temp;
                            }   
                        }
                    }
                }
            }
            return returnVal;
        }

        /// <summary>
        /// This method adjusts the imagesource for various arrows to keep the top rows up arrow gray, the bottom rows down arrow gray, and the remaining normal
        /// </summary>
        private void adjustArrowIcons()
        {
            int firstrow = 1;
            int lastrow = customDataGrid.BaseGrid.RowDefinitions.Count - 2;

            changeUpArrow(firstrow, grayUpArrowIconSource); //changing first arrow to gray
            changeUpArrow(firstrow+1, upArrowIconSource); //second to normal

            changeDownArrow(lastrow, grayDownArrowIconSource); //changing first arrow to gray
            changeDownArrow(lastrow - 1, downArrowIconSource); //second to normal
            
        }

        /// <summary>
        /// this method changes the imagesource of the downarrow of rowToChange to imgSource
        /// </summary>
        /// <param name="rowToChange"></param>
        /// <param name="imgSource"></param>
        private void changeDownArrow(int rowToChange, ImageSource imgSource)
        {
            UIElement firstSP = customDataGrid.GetUIElementAt(0, rowToChange);
            if (firstSP is StackPanel)
            {
                UIElement secondSP = (firstSP as StackPanel).Children[0];
                if (secondSP is StackPanel)
                {
                    UIElement upArrow = (secondSP as StackPanel).Children[1];
                    (upArrow as Image).Source = imgSource;
                }
            }
        }

        /// <summary>
        /// this method changes the imagesource of the uparrow of rowToChange to imgSource
        /// </summary>
        /// <param name="rowToChange"></param>
        /// <param name="imgSource"></param>
        private void changeUpArrow(int rowToChange, ImageSource imgSource)
        {
            UIElement firstSP = customDataGrid.GetUIElementAt(0, rowToChange);
            if (firstSP is StackPanel)
            {
                UIElement secondSP = (firstSP as StackPanel).Children[0];
                if (secondSP is StackPanel)
                {
                    UIElement upArrow = (secondSP as StackPanel).Children[0];
                    (upArrow as Image).Source = imgSource;
                }
            }
        }
        /// <summary>
        /// returns the view for a rubric
        /// </summary>
        /// <returns></returns>
        public RubricView GetView()
        {
            return thisView;
        }

        #region Events

        /// <summary>
        /// Called when the user clicks on the "Cancel" button in the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
            HtmlPage.Window.Invoke("CloseRubric", "");
        }

        /// <summary>
        /// Called when the user clicks the "Publish" button in the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PublishChanges_Click(object sender, RoutedEventArgs e)
        {
            this.getData();

            //will house the final form of the data that we will send to the client
            Rubric rubric = new Rubric();
            List<CellDescription> cellDescriptions = new List<CellDescription>();
            rubric.Criteria = new ObservableCollection<Criterion>();
            rubric.Levels = new ObservableCollection<Level>();

            //loop through all of our data
            foreach(ICell cell in this.dataFromCells)
            {
                //Unfortunately, our data is scattered all over the place.  As such, we really
                //don't know the dimensions of our rubric.  As a little hack, we can
                //just create new rows & columns based on the current cell
                while (rubric.Levels.Count + 1 < cell.Column)
                {
                    rubric.Levels.Add(new Level() { ID = rubric.Levels.Count} );
                }
                while (rubric.Criteria.Count + 1 < cell.Row)
                {
                    rubric.Criteria.Add(new Criterion() {ID = rubric.Criteria.Count} );
                }

                //header cells are analogous to Levels
                if (cell is HeaderCell)
                {
                    HeaderCell header = cell as HeaderCell;
                    Level currentLevel = rubric.Levels[header.Column];
                    currentLevel.RangeStart = 0;
                    currentLevel.RangeEnd = header.ComboBoxValue;
                    currentLevel.LevelTitle = header.Information;
                }

                //rubric cells are cell descriptions
                else if (cell is RubricCell)
                {
                    RubricCell currentCell = cell as RubricCell;
                    
                    //huge assumption: If the current column is 1, then we have a points spread
                    //otherwise, we have a cell description
                    if (currentCell.Column == 1)
                    {
                        double someDouble = 0.0;
                        double.TryParse(currentCell.Information, out someDouble);
                        rubric.Criteria[currentCell.Row].Weight = someDouble;
                    }
                    else
                    {
                        CellDescription desc = new CellDescription();
                        desc.CriterionID = currentCell.Row;
                        desc.LevelID = currentCell.Column;
                        desc.Description = currentCell.Information;
                        cellDescriptions.Add(desc);
                    }
                    
                }

                //there should only be two checkbox values.  One to enable column comments and one
                //to enable global comments
                else if (cell is CheckBoxCell)
                {
                    CheckBoxCell cb = cell as CheckBoxCell;
                    if (cell.Information.CompareTo(CheckboxValues.ColumnComment.ToString()) == 0)
                    {
                        rubric.HasCriteriaComments = cb.CheckBoxValue;
                    }
                    else if (cell.Information.CompareTo(CheckboxValues.GlobalComment.ToString()) == 0)
                    {
                        rubric.HasGlobalComments = cb.CheckBoxValue;
                    }
                }
            }

            //at the end of all of this, we should have a fully realized rubric with criteria and levels
            //and a list of cell descriptions for each

            
        }

        /// <summary>
        /// event for the delete button(s) in the first column (deletes a row)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (customDataGrid.BaseGrid.RowDefinitions.Count != 3) //won't remove if its the only row
            {
                customDataGrid.RemoveRow(getRow(e));
                updateLastRow();

                //adjusting the arrow icons since there could be a change to the last or first row
                adjustArrowIcons();
            }   
        }

        /// <summary>
        /// event for the delete button(s) in the first row (deletes a column)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteCol_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (customDataGrid.BaseGrid.ColumnDefinitions.Count != 5) //won't remove if its the only column
            {
                customDataGrid.RemoveColumn(getColumn(e));
            }
        }

        /// <summary>
        /// event for button in first row (adds a column)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addCol_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //getting the value of the checkbox(from the final column) before deleting it
            bool commentCheckboxValue = false;
            UIElement commentCheckboxSP = customDataGrid.GetUIElementAt(customDataGrid.BaseGrid.ColumnDefinitions.Count - 1, 0);
            if (commentCheckboxSP is StackPanel)
            {
                foreach (UIElement cb in (commentCheckboxSP as StackPanel).Children)
                {
                    if (cb is CheckBox) commentCheckboxValue = (bool)(cb as CheckBox).IsChecked;
                }
            }

            //removes last two columns (add button column & comment column), creates the new level column, and recreates the addbutton/comment column
            customDataGrid.RemoveColumn(customDataGrid.BaseGrid.ColumnDefinitions.Count - 1);
            customDataGrid.RemoveColumn(customDataGrid.BaseGrid.ColumnDefinitions.Count - 1);

            createLevelColumn();
            createAddButtonColumn();
            createCommentColumn();
            updateLastRow();
            setTabIndex();

            //setting the value of the new checkbox
            commentCheckboxSP = customDataGrid.GetUIElementAt(customDataGrid.BaseGrid.ColumnDefinitions.Count - 1, 0);
            if (commentCheckboxSP is StackPanel)
            {
                foreach (UIElement cb in (commentCheckboxSP as StackPanel).Children)
                {
                    if (cb is CheckBox) (cb as CheckBox).IsChecked = commentCheckboxValue;
                }
            }
        }

        /// <summary>
        /// event for button in first column (adds a row)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //getting the value of the checkbox(from the final row) before deleting it
            bool commentCheckboxValue = false;
            UIElement commentCheckboxSP = customDataGrid.GetUIElementAt(1, customDataGrid.BaseGrid.RowDefinitions.Count - 1);
            if (commentCheckboxSP is StackPanel)
            {
                foreach (UIElement cb in (commentCheckboxSP as StackPanel).Children)
                {
                    if (cb is CheckBox) commentCheckboxValue = (bool)(cb as CheckBox).IsChecked;
                }
            }

            //removes the last row (the add button), then adds the new criterion row, then adds the add button row back in
            customDataGrid.RemoveRow(customDataGrid.BaseGrid.RowDefinitions.Count - 1);
            createCriterionRow();
            createAddButtonRow();
            setTabIndex();

            //setting the value of the new checkbox
            commentCheckboxSP = customDataGrid.GetUIElementAt(1, customDataGrid.BaseGrid.RowDefinitions.Count - 1);
            if (commentCheckboxSP is StackPanel)
            {
                foreach (UIElement cb in (commentCheckboxSP as StackPanel).Children)
                {
                    if (cb is CheckBox) (cb as CheckBox).IsChecked = commentCheckboxValue;
                }
            }

            //adjusting the arrow icons since there is a new last row
            adjustArrowIcons();
        }

        /// <summary>
        /// this event updates the points by updating the last row each time the textbox loses focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            updateLastRow();
        }

        /// <summary>
        /// this event updates the points by updating the last row each time the textbox value is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            updateLastRow();
        }

        /// <summary>
        /// This event is fired when a user clicks on the down arrow. The event attempts to perform a swap between the row below it and itself
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void downArrow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int initRow = getRow(e);
            int rowBelow = initRow + 1;

            if (rowBelow > 0 && initRow > 0 && rowBelow < (customDataGrid.BaseGrid.RowDefinitions.Count - 1)) //checks to make sure its a valid row to swap (a criterion row)
            {
                performSwap(initRow, rowBelow);
                //fixing tab indexes after move
                setTabIndex();
            }
            adjustArrowIcons();
        }

        /// <summary>
        /// This event is fired when a user clicks on the up arrow. The event attempts to perform a swap between the row above it and itself
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void upArrow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int initRow = getRow(e);
            int rowAbove = initRow - 1;

            if (rowAbove > 0 && initRow > 0)// && rowBelow < (customDataGrid.BaseGrid.RowDefinitions.Count - 2))
            {
                performSwap(initRow, rowAbove);
                //fixing tab indexes after move
                setTabIndex();
            }
            adjustArrowIcons();
        }
        #endregion Events
    }
}