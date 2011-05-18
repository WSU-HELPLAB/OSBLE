using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReviewInterfaceBase.ViewModel.Document.XpsDocument
{
    public class XpsSelectionHighlighting
    {
        public event EventHandler SelectionChanged = delegate { };

        Point selectionAnchorPoint;
        Rectangle selectionRectangle;
        Canvas canvas;
        bool isSelecting = false;

        public Canvas XpsCanvas
        {
            get { return canvas; }
        }

        public Rectangle Selection
        {
            get
            {
                if (selectionRectangle.Width != 0 || selectionRectangle.Height != 0)
                {
                    Rectangle rect = new Rectangle();
                    rect.SetValue(Canvas.LeftProperty, selectionRectangle.GetValue(Canvas.LeftProperty));
                    rect.SetValue(Canvas.TopProperty, selectionRectangle.GetValue(Canvas.TopProperty));
                    rect.Width = selectionRectangle.Width;
                    rect.Height = selectionRectangle.Height;
                    return rect;
                }
                else
                {
                    return null;
                }
            }
        }

        private Rectangle newSelectionRectangle()
        {
            Rectangle rect = new Rectangle() { Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0)) };
            rect.SetValue(Canvas.ZIndexProperty, 1);
            rect.Width = 0;
            rect.Height = 0;
            rect.Visibility = Visibility.Collapsed;
            return rect;
        }

        public XpsSelectionHighlighting(Canvas canvas)
        {
            this.canvas = canvas;
            this.canvas.MouseLeftButtonDown += new MouseButtonEventHandler(canvas_MouseLeftButtonDown);
            this.canvas.MouseLeftButtonUp += new MouseButtonEventHandler(canvas_MouseLeftButtonUp);
            this.canvas.MouseMove += new MouseEventHandler(canvas_MouseMove);

            selectionRectangle = newSelectionRectangle();

            this.canvas.Children.Add(selectionRectangle);

            this.canvas.LayoutUpdated += new EventHandler(canvas_LayoutUpdated);
        }

        private void canvas_LayoutUpdated(object sender, EventArgs e)
        {
            //the canvas we use has no size and thus we need to find something that does.
            FrameworkElement parent = this.canvas.Parent as FrameworkElement;
            while (parent != null && parent.ActualHeight == 0)
            {
                parent = parent.Parent as FrameworkElement;
            }
            if (parent != null && parent.ActualHeight != 0)
            {
                //we use the rectangle only to get mouse events otherwise the user would have to do a mouse down on an element
                //like a glyph for example.
                canvas.LayoutUpdated -= new EventHandler(canvas_LayoutUpdated);
                Border rect = new Border() { Background = new SolidColorBrush(Colors.Transparent) };
                rect.SetValue(Canvas.ZIndexProperty, -255);

                //we make it as big as the canvas we are using (ideally, practically as big as the first thing
                //that has a real size which should be equal to the canvas's size.
                rect.Height = parent.ActualHeight;
                rect.Width = parent.ActualWidth;
                canvas.Children.Add(rect);
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                Point mouseLocation = e.GetPosition(canvas);

                //got to be a point because size cant be negative
                Point size = new Point();
                size.X = mouseLocation.X - selectionAnchorPoint.X;
                size.Y = mouseLocation.Y - selectionAnchorPoint.Y;

                if (size.X < 0)
                {
                    selectionRectangle.SetValue(Canvas.LeftProperty, mouseLocation.X);
                    selectionRectangle.Width = size.X * -1;
                }
                else
                {
                    selectionRectangle.SetValue(Canvas.LeftProperty, selectionAnchorPoint.X);
                    selectionRectangle.Width = size.X;
                }

                if (size.Y < 0)
                {
                    selectionRectangle.SetValue(Canvas.TopProperty, mouseLocation.Y);
                    selectionRectangle.Height = size.Y * -1;
                }
                else
                {
                    selectionRectangle.SetValue(Canvas.TopProperty, selectionAnchorPoint.Y);
                    selectionRectangle.Height = size.Y;
                }
            }
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            canvas.ReleaseMouseCapture();
            isSelecting = false;
            SelectionChanged(this, EventArgs.Empty);
            e.Handled = true;
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectionAnchorPoint = e.GetPosition(canvas);
            canvas.CaptureMouse();
            selectionRectangle.SetValue(Canvas.LeftProperty, selectionAnchorPoint.X);
            selectionRectangle.SetValue(Canvas.TopProperty, selectionAnchorPoint.Y);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
            isSelecting = true;
            selectionRectangle.Visibility = Visibility.Visible;
            e.Handled = true;
        }

        public void ClearSelection()
        {
            //this.canvas.Children.Remove(selectionRectangle);
            //selectionRectangle = newSelectionRectangle();
            //this.canvas.Children.Add(selectionRectangle);

            selectionRectangle.Visibility = Visibility.Collapsed;
        }
    }
}