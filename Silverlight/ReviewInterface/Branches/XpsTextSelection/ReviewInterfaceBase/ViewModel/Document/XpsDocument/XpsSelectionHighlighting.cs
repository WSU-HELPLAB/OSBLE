using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Documents;

namespace ReviewInterfaceBase.ViewModel.Document.XpsDocument
{
    public class RectangleSelectionHighlighting
    {
        #region Delegates

        public event EventHandler SelectionChanged = delegate { };

        #endregion Delegates

        #region Fields

        private GlyphSelection glyphSelection = new GlyphSelection();
        private Point selectionAnchorPoint;
        private Rectangle selectionRectangle;
        private Canvas displayCanvas;
        private Canvas hitCanvas;
        private bool isSelecting = false;
        private bool isSelectingGlyphs = false;

        #endregion Fields

        #region Properties

        public Canvas XpsCanvas
        {
            get { return displayCanvas; }
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
                    (selectionRectangle.Parent as Panel).Children.Add(rect);
                    return rect;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion Properties

        #region Constructor

        public RectangleSelectionHighlighting(Canvas canvas, Canvas hitCanvas, GlyphSelection glyphSelect)
        {
            this.glyphSelection = glyphSelect;
            this.displayCanvas = canvas;
            this.hitCanvas = hitCanvas;
            hitCanvas.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 128));
            hitCanvas.MouseLeftButtonDown += new MouseButtonEventHandler(canvas_MouseLeftButtonDown);
            hitCanvas.MouseLeftButtonUp += new MouseButtonEventHandler(canvas_MouseLeftButtonUp);
            hitCanvas.MouseMove += new MouseEventHandler(canvas_MouseMove);

            selectionRectangle = newSelectionRectangle();

            this.displayCanvas.Children.Add(selectionRectangle);

            hitCanvas.LayoutUpdated += new EventHandler(canvas_LayoutUpdated);
        }

        #endregion Constructor

        #region HelperFunctions

        private void canvas_LayoutUpdated(object sender, EventArgs e)
        {
            //the canvas we use has no size and thus we need to find something that does.
            FrameworkElement parent = hitCanvas.Parent as FrameworkElement;
            while (parent != null && parent.ActualHeight == 0)
            {
                parent = parent.Parent as FrameworkElement;
            }
            if (parent != null && parent.ActualHeight != 0)
            {
                //we use the rectangle only to get mouse events otherwise the user would have to do a mouse down on an element
                //like a glyph for example.
                hitCanvas.LayoutUpdated -= new EventHandler(canvas_LayoutUpdated);
                Border rect = new Border();
                rect.MouseLeftButtonDown += new MouseButtonEventHandler(rect_MouseLeftButtonDown);
                rect.SetValue(Canvas.ZIndexProperty, -255);

                //we make it as big as the canvas we are using (ideally, practically as big as the first thing
                //that has a real size which should be equal to the canvas's size.
                rect.Height = parent.ActualHeight;
                rect.Width = parent.ActualWidth;
                hitCanvas.Children.Add(rect);
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

        private void rect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(isSelectingGlyphs == true)
            {
                Glyphs tempGlyph = isOverGlyphs(e);
                if (tempGlyph != null)
                    glyphSelection.MoveGlyphSelection(tempGlyph, e);
                
            }
            else
            {
                if (isSelecting)
                {
                    Point mouseLocation = e.GetPosition(displayCanvas);

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
            
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            

            if (isSelectingGlyphs)
            {
                glyphSelection.EndGlyphSelection(isOverGlyphs(e), e);
                isSelectingGlyphs = false;
            }
            else if(isSelecting)
            {
                hitCanvas.ReleaseMouseCapture();
                isSelecting = false;
                SelectionChanged(this, EventArgs.Empty);
                e.Handled = true;
            }
        }
        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //removing any existing highlights
            glyphSelection.ClearSelection();
            this.ClearSelection();

            if (isOverGlyphs(e)!=null) //mouse click on a glyph, reroute event to GlyphSelection
            {

                glyphSelection.StartGlyphSelection(isOverGlyphs(e), e);
                isSelectingGlyphs = true;
            }
            else //mouse click on empty area for rectangle
            {
                selectionAnchorPoint = e.GetPosition(displayCanvas);
                hitCanvas.CaptureMouse();
                selectionRectangle.SetValue(Canvas.LeftProperty, selectionAnchorPoint.X);
                selectionRectangle.SetValue(Canvas.TopProperty, selectionAnchorPoint.Y);
                selectionRectangle.Width = 0;
                selectionRectangle.Height = 0;
                isSelecting = true;
                selectionRectangle.Visibility = Visibility.Visible;
                e.Handled = true;
            }
             
        }
        /// <summary>
        /// returns null if not over a Glyphs, else returns the Glyphs
        /// </summary>
        private Glyphs isOverGlyphs(MouseEventArgs e)
        {
            IEnumerable<UIElement> elemList = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(Application.Current.RootVisual), hitCanvas.Parent as Grid);
            elemList.GetEnumerator();

            foreach (UIElement i in elemList)
            {
                if (i is Glyphs)
                {
                    return (i as Glyphs); //found the glyph (should only be 1 glyph, no need to continue foreach)
                }
            }
            return null;
        }

        #endregion HelperFunctions

        #region Public Methods

        public void ClearSelection()
        {
            //this.canvas.Children.Remove(selectionRectangle);
            //selectionRectangle = newSelectionRectangle();
            //this.canvas.Children.Add(selectionRectangle);

            selectionRectangle.Visibility = Visibility.Collapsed;
        }

        #endregion Public Methods
    }
}