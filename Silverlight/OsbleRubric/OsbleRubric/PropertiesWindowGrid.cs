/*
Copyright 2010 HELP Lab @ Washington State University

This file is part of ChemProV (http://helplab.org/chemprov).

ChemProV is distributed under the Open Software License ("OSL") v3.0.
Consult "LICENSE.txt" included in this package for the complete OSL license.
*/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChemProV.PFD.Streams.PropertiesWindow
{
    public class CustomDataGrid
    {
        public event SizeChangedEventHandler SizeChanged = delegate { };

        private Grid baseGrid = new Grid();

        public Grid BaseGrid
        {
            get { return baseGrid; }
        }

        Brush borderBrush = new SolidColorBrush(Colors.Black);

        bool hideBordersForLastTwoColumns = true;

        public bool HideBordersForLastTwoColumns
        {
            get { return hideBordersForLastTwoColumns; }
            set { hideBordersForLastTwoColumns = value; }
        }

        bool hideBordersForLastRow = false;

        public bool HideBordersForLastRow
        {
            get { return hideBordersForLastRow; }
            set { hideBordersForLastRow = value; }
        }

        /// <summary>
        /// The brush used for the border of each cell
        /// </summary>
        public Brush BorderBrush
        {
            get { return borderBrush; }
            set
            {
                borderBrush = value;
                changeBorderBrush();
            }
        }

        double borderThickness = 1;

        /// <summary>
        /// This sets the boarder thickness for the all the borders note that it will take into account shared edges and
        /// display the boarder only once.
        /// </summary>
        public double BorderThickness
        {
            get { return borderThickness; }
            set
            {
                borderThickness = value;
                changeBorderThickness();
            }
        }

        public CustomDataGrid()
        {
            baseGrid.MouseLeftButtonDown += new MouseButtonEventHandler(my_Grid_MouseLeftButtonDown);
            baseGrid.SizeChanged += new SizeChangedEventHandler(baseGrid_SizeChanged);
        }

        private void baseGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChanged(this, e);
        }

        private void my_Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            baseGrid.CaptureMouse();
        }

        public void ClearAll()
        {
            foreach (UIElement ui in baseGrid.Children)
            {
                //gotta do this or the child thinks it is still attached to Border
                (ui as Border).Child = null;
            }
            baseGrid.Children.Clear();
            baseGrid.RowDefinitions.Clear();
            baseGrid.ColumnDefinitions.Clear();
            placeBorders();
        }

        private void changeBorderBrush()
        {
            foreach (Border br in baseGrid.Children)
            {
                br.BorderBrush = borderBrush;
            }
        }

        private void changeBorderThickness()
        {
            foreach (Border br in baseGrid.Children)
            {
                int row = (int)br.GetValue(Grid.RowProperty);
                int column = (int)br.GetValue(Grid.ColumnProperty);
                if (hideBordersForLastRow != true || row != baseGrid.RowDefinitions.Count - 1)
                {
                    if (hideBordersForLastTwoColumns != true || column < baseGrid.ColumnDefinitions.Count - 2)
                    {
                        //set the borders we do not want super thickboards when an edge is shared but we also dont want to be be missing an edge
                        //assume we need to place the right and bottem edges except in the case when it is the leftmost or topmost edge then we must
                        //place that edge as well.
                        if (column != 0 && row != 0)
                        {
                            br.BorderThickness = new Thickness(0, 0, borderThickness, borderThickness);
                        }
                        else if (column == 0 && row != 0)
                        {
                            br.BorderThickness = new Thickness(borderThickness, 0, borderThickness, borderThickness);
                        }
                        else if (column != 0 && row == 0)
                        {
                            br.BorderThickness = new Thickness(0, borderThickness, borderThickness, borderThickness);
                        }
                        else
                        {
                            br.BorderThickness = new Thickness(borderThickness, borderThickness, borderThickness, borderThickness);
                        }
                    }
                }
            }
        }

        public void UpdateGridSize()
        {
            foreach (Border br in baseGrid.Children)
            {
                if (br.Child != null)
                {
                    br.Height = (double)(br.Child as FrameworkElement).ActualHeight;
                    br.Width = (double)(br.Child as FrameworkElement).ActualWidth;
                }
            }

            //this will affect the size AFTER this function ends so we cannot call this function directly
            //so use an event handler to get around it, hack-ish but it works
            baseGrid.SizeChanged += new SizeChangedEventHandler(PropertiesWindowGrid_FixCellBorders);
        }

        private void placeBorderAt(int column, int row)
        {
            Border br = new Border();
            br.BorderBrush = borderBrush;
            br.CornerRadius = new CornerRadius(0);
            br.SetValue(Grid.ColumnProperty, column);
            br.SetValue(Grid.RowProperty, row);
            br.UseLayoutRounding = false;
            //add the boarder to our grid
            baseGrid.Children.Add(br);
        }

        private void placeBorders()
        {
            for (int column = 0; column < baseGrid.ColumnDefinitions.Count; column++)
            {
                for (int row = 0; row < baseGrid.RowDefinitions.Count; row++)
                {
                    placeBorderAt(column, row);
                }
            }
            changeBorderThickness();
        }

        public UIElement GetUIElementAt(int column, int row)
        {
            foreach (UIElement ui in baseGrid.Children)
            {
                if ((int)ui.GetValue(Grid.ColumnProperty) == column && (int)ui.GetValue(Grid.RowProperty) == row)
                {
                    return (ui as Border).Child;
                }
            }
            return null;
        }

        public void RemoveUIElementAt(int column, int row)
        {
            ((GetUIElementAt(column, row) as FrameworkElement).Parent as Border).Child = null;
        }

        /// <summary>
        /// This places the passed UIElement into a boarder object and then that into the grid at the given location.
        /// </summary>
        /// <param name="ui">UIElement to placed into the grid</param>
        /// <param name="column">The coulmn in which u want the UIElemnt placed</param>
        /// <param name="row">The row in which u want the UIElemnt placed</param>
        /// <returns></returns>
        public bool PlaceUIElement(UIElement ui, int column, int row)
        {
            bool found = false;
            if (column >= baseGrid.ColumnDefinitions.Count)
            {
                while (baseGrid.ColumnDefinitions.Count <= column)
                {
                    baseGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30, GridUnitType.Auto) });
                    int i = 0;
                    while (i < baseGrid.ColumnDefinitions.Count)
                    {
                        placeBorderAt(baseGrid.ColumnDefinitions.Count - 1, i);
                        i++;
                    }
                }
                placeBorders();
            }
            if (row >= baseGrid.RowDefinitions.Count)
            {
                while (baseGrid.RowDefinitions.Count <= row)
                {
                    baseGrid.RowDefinitions.Add(new RowDefinition());
                    int i = 0;
                    while (i < baseGrid.RowDefinitions.Count)
                    {
                        placeBorderAt(i, baseGrid.RowDefinitions.Count - 1);
                        i++;
                    }
                }
                placeBorders();
            }
            foreach (Border br in baseGrid.Children)
            {
                if ((int)br.GetValue(Grid.ColumnProperty) == column && (int)br.GetValue(Grid.RowProperty) == row)
                {
                    found = true;
                    br.Child = ui;
                    Grid.SetRowSpan(br, Grid.GetRowSpan(ui as FrameworkElement));
                    Grid.SetColumnSpan(br, Grid.GetColumnSpan(ui as FrameworkElement));
                    break;
                }
            }
            if (found == false)
            {
                return false;
            }

            //this will affect the size AFTER this function ends so we cannot call this function directly
            //so use an event handler to get around it, hack-ish but it works
            baseGrid.SizeChanged += new SizeChangedEventHandler(PropertiesWindowGrid_FixCellBorders);
            return true;
        }

        public void RemoveUIElement(UIElement ui)
        {
            foreach (Border br in baseGrid.Children)
            {
                if (br.Child == ui)
                {
                    br.Child = null;
                }
            }
        }

        private void PropertiesWindowGrid_FixCellBorders(object sender, SizeChangedEventArgs e)
        {
            baseGrid.SizeChanged -= new SizeChangedEventHandler(PropertiesWindowGrid_FixCellBorders);
            foreach (UIElement child in baseGrid.Children)
            {
                Border br = child as Border;
                int columnIndex = (int)br.GetValue(Grid.ColumnProperty);
                int rowIndex = (int)br.GetValue(Grid.RowProperty);
                //  br.Width = baseGrid.ColumnDefinitions[columnIndex].ActualWidth;
                // br.Height = baseGrid.ColumnDefinitions[rowIndex].ActualWidth;
            }
        }
    }
}