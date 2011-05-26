using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReviewInterfaceBase.ViewModel.Document.XpsDocument
{
    public class GlyphSelection
    {
        /*GLYPHS:  Glyphs originX is the left side of the start of the glyph
        * originY is the base line of the text.  That is the bottom of most includes like h. You should note that letters such as g is below this line
        * Height is the defined as from the base line to the top of the glyph.  For example the height this e would be from the bottom of the letter
        * all the way to the bottom of the next base line (I think).
        * Width is what you would expect no surprises yay
        * Notice that if we want to highlight a line and use the originY and Height it will cut of g's and might get the line above g's.
        * So we subtract 1/4 of the height from the originY we get exactly from the top of a T to the bottom of a g.
        * At this point am unsure how to get the exact rectangle of just on character in a glyph.
        */

        #region Deleges

        public event EventHandler SelectionChanged = delegate { };

        #endregion Deleges

        #region Fields

        private List<Glyphs> allGlyphs = new List<Glyphs>();
        private Point startPoint;
        private Glyphs startGlyph = null;
        private Glyphs endGlyph = null;
        private int indexOfLastGlyph = -1;
        private List<Rectangle> selection = new List<Rectangle>();
        private bool highlightingDown = true;

        #endregion Fields

        #region Properties

        public List<Rectangle> Selection
        {
            get
            {
                List<Rectangle> rectangles = new List<Rectangle>();
                foreach (Rectangle oldRect in selection)
                {
                    Rectangle newRect = new Rectangle();
                    newRect.Width = oldRect.Width;
                    newRect.Height = oldRect.Height;
                    newRect.SetValue(Canvas.TopProperty, oldRect.GetValue(Canvas.TopProperty));
                    newRect.SetValue(Canvas.LeftProperty, oldRect.GetValue(Canvas.LeftProperty));
                    (oldRect.Parent as Panel).Children.Add(newRect);
                    rectangles.Add(newRect);
                }
                mergeRectangles(rectangles);
                return rectangles;
            }
            set { selection = value; }
        }

        #endregion Properties

        #region HelperFunctions

        private List<Glyphs> getAllGlyphs(Panel p)
        {
            List<Glyphs> listOfGlyphs = new List<Glyphs>();

            foreach (UIElement ui in p.Children)
            {
                if (ui is Glyphs)
                {
                    listOfGlyphs.Add(ui as Glyphs);
                }
                else if (ui is Panel)
                {
                    listOfGlyphs.AddRange(getAllGlyphs(ui as Panel));
                }
            }

            return listOfGlyphs;
        }

        private void highlightGlyph(Glyphs glyph, Point left, Point right)
        {
            Panel panel = getPanel(glyph);
            Rectangle rect = new Rectangle() { IsHitTestVisible = false, Height = glyph.ActualHeight, Width = Math.Abs(right.X - left.X), Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 128)) };
            Point p = new Point(startPoint.X, glyph.OriginY - (glyph.ActualHeight - glyph.ActualHeight / 4));

            //If left is really right then we draw the rectangle starting at right and then use its width to get back to left
            //This happens when we highlight backwards
            if (left.X > right.X)
            {
                p.X -= rect.Width;
            }

            rect.SetValue(Canvas.TopProperty, p.Y);
            rect.SetValue(Canvas.LeftProperty, p.X);
            selection.Add(rect);
            panel.Children.Add(rect);
        }

        private void highlightGlyph(Glyphs glyph, Point startPoint, bool highlightRight)
        {
            Panel panel = getPanel(glyph);
            Point size = new Point(0, glyph.ActualHeight);
            Point topLeft = new Point(0, glyph.OriginY - (glyph.ActualHeight - glyph.ActualHeight / 4));
            if (highlightRight)
            {
                topLeft.X = startPoint.X;
                size.X = Math.Abs(glyph.ActualWidth - (startPoint.X - glyph.OriginX));
            }
            else
            {
                topLeft.X = glyph.OriginX;
                size.X = Math.Abs(startPoint.X - glyph.OriginX);
            }

            Rectangle rect = new Rectangle() { IsHitTestVisible = false, Height = size.Y, Width = size.X, Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 128)) };
            rect.SetValue(Canvas.TopProperty, topLeft.Y);
            rect.SetValue(Canvas.LeftProperty, topLeft.X);
            selection.Add(rect);
            panel.Children.Add(rect);
        }

        private void highlightGlyphs(Glyphs start, Glyphs end)
        {
            Glyphs tempGlyph;
            int count;
            int index;
            int startIndex = allGlyphs.IndexOf(start);
            int endIndex = allGlyphs.IndexOf(end);

            if (startIndex > endIndex)
            {
                count = endIndex;
                index = startIndex;

                if (count < 0 || index >= allGlyphs.Count)
                {
                    return;
                }

                tempGlyph = allGlyphs[index];

                while (index > count)
                {
                    createRectangle(tempGlyph);

                    tempGlyph = allGlyphs[--index];
                }
            }
            else
            {
                count = endIndex;
                index = startIndex;

                if (index < 0 || count >= allGlyphs.Count)
                {
                    return;
                }

                tempGlyph = allGlyphs[index];

                while (index < count)
                {
                    createRectangle(tempGlyph);
                    tempGlyph = allGlyphs[++index];
                }
            }

            //we go from startIndex to endIndex

            //We are either out of range or we will be so don't do anything
        }

        private void createRectangle(Glyphs glyph)
        {
            Panel panel;
            panel = getPanel(glyph);
            Point size = new Point(glyph.ActualWidth, glyph.ActualHeight);
            Rectangle rect = new Rectangle() { IsHitTestVisible = false, Height = size.Y, Width = size.X, Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 128)) };
            Point topleft = new Point(glyph.OriginX, glyph.OriginY - (size.Y - size.Y / 4));
            rect.SetValue(Canvas.TopProperty, topleft.Y);
            rect.SetValue(Canvas.LeftProperty, topleft.X);
            selection.Add(rect);
            panel.Children.Add(rect);
        }

        private Panel getPanel(Glyphs glyph)
        {
            //If this is anything but a canvas we are in trouble....
            return (glyph.Parent as Panel);
        }

        private void removeLastSelectionRectangle()
        {
            if (selection.Count > 0)
            {
                int lastIndex = selection.Count - 1;
                Rectangle lastRect = selection[lastIndex];
                (lastRect.Parent as Panel).Children.Remove(lastRect);
                selection.RemoveAt(lastIndex);
            }
        }

        private void removeFirstSelectionRectangle()
        {
            if (selection.Count < 0)
            {
                Rectangle firstRect = selection[0];
                (firstRect.Parent as Panel).Children.Remove(firstRect);
                selection.RemoveAt(0);
            }
        }

        private void clearSelection()
        {
            foreach (Rectangle rect in selection)
            {
                (rect.Parent as Panel).Children.Remove(rect);
            }
            selection.Clear();
        }

        private void fakemergeRectangles()
        {
            int i = 0;
            while (i + 1 < selection.Count)
            {
                Rectangle rect = selection[i];
                Rectangle rect2 = selection[i + 1];
                if ((rect.Parent == rect2.Parent) &&
                    (rect.Height == rect2.Height) &&
                    ((double)rect.GetValue(Canvas.TopProperty) == (double)rect2.GetValue(Canvas.TopProperty)))
                {
                    if ((double)rect.GetValue(Canvas.LeftProperty) < (double)rect2.GetValue(Canvas.LeftProperty))
                    {
                        rect.Width = (double)rect2.GetValue(Canvas.LeftProperty) - (double)rect.GetValue(Canvas.LeftProperty);
                    }
                    else
                    {
                        rect2.Width = (double)rect.GetValue(Canvas.LeftProperty) - (double)rect2.GetValue(Canvas.LeftProperty);
                    }
                }
                i++;
            }
        }

        private void mergeRectangles(List<Rectangle> rectangles)
        {
            int i = 0;
            while (i + 1 < rectangles.Count)
            {
                Rectangle rect = rectangles[i];
                Rectangle rect2 = rectangles[i + 1];
                if ((rect.Parent == rect2.Parent) &&
                    (rect.Height == rect2.Height) &&
                    ((double)rect.GetValue(Canvas.TopProperty) == (double)rect2.GetValue(Canvas.TopProperty)))
                {
                    if ((double)rect.GetValue(Canvas.LeftProperty) < (double)rect2.GetValue(Canvas.LeftProperty))
                    {
                        rect.Width = (double)rect2.GetValue(Canvas.LeftProperty) + rect2.Width - (double)rect.GetValue(Canvas.LeftProperty);
                        rectangles.Remove(rect2);
                        (rect2.Parent as Panel).Children.Remove(rect2);
                        i--;
                    }
                    else
                    {
                        rect2.Width = (double)rect.GetValue(Canvas.LeftProperty) + rect.Width - (double)rect2.GetValue(Canvas.LeftProperty);
                        rectangles.Remove(rect);
                        (rect.Parent as Panel).Children.Remove(rect);
                        i--;
                    }
                }
                i++;
            }
        }

        #endregion HelperFunctions

        #region Public Methods

        public void ClearSelection()
        {
            clearSelection();
            SelectionChanged(this, EventArgs.Empty);
        }

        public void initilizeGlyph(Canvas xpsPage)
        {
            allGlyphs.AddRange(getAllGlyphs(xpsPage));

            foreach (Glyphs glyph in allGlyphs)
            {
                glyph.MouseLeftButtonDown += new MouseButtonEventHandler(glyph_MouseLeftButtonDown);
                glyph.MouseMove += new MouseEventHandler(glyph_MouseMove);
                glyph.MouseLeftButtonUp += new MouseButtonEventHandler(glyph_MouseLeftButtonUp);
            }
        }

        #endregion Public Methods

        #region MouseEvents

        private void glyph_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (startGlyph != null)
            {
                startGlyph = null;

                e.Handled = true;

                SelectionChanged(this, EventArgs.Empty);
            }
        }


        public void MoveGlyphSelection(object sender, MouseEventArgs e)
        {
            glyph_MouseMove(sender, e);
        }
        public void EndGlyphSelection(Glyphs glyph, MouseButtonEventArgs e)
        {
            glyph_MouseLeftButtonUp(glyph, e);
        }
        public void StartGlyphSelection(Glyphs glyph, MouseButtonEventArgs e)
        {
            glyph_MouseLeftButtonDown(glyph, e);
        }

        private void glyph_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            indexOfLastGlyph = -1;
            endGlyph = null;
            foreach (Rectangle rect in selection)
            {
                (rect.Parent as Panel).Children.Remove(rect);
            }
            selection.Clear();
            startGlyph = sender as Glyphs;
            startPoint = e.GetPosition(startGlyph.Parent as FrameworkElement);
            e.Handled = true;
        }

        private void glyph_MouseMove(object sender, MouseEventArgs e)
        {
            if (startGlyph != null)
            {
                //Panel page = getPanel(startGlyph);
                //Point endPoint = e.GetPosition(page);

                if (endGlyph == null)
                {
                    endGlyph = startGlyph;
                }

                Glyphs newEndGlyph = sender as Glyphs;
                int indexNewEndGlyph = allGlyphs.IndexOf(newEndGlyph);
                int indexStartGlyph = allGlyphs.IndexOf(startGlyph);
                int indexEndGlyph = allGlyphs.IndexOf(endGlyph);

                if (indexNewEndGlyph < indexStartGlyph)
                {
                    if (highlightingDown)
                    {
                        clearSelection();
                        highlightingDown = false;
                    }

                    //we are going up the way
                    //Because we are going up then End should be higher value then newEnd if we are adding new stuff
                    //and less if we are taking stuff away
                    if (indexEndGlyph < indexNewEndGlyph)
                    {
                        //we have gone backwards from where we were
                        if (indexEndGlyph - indexNewEndGlyph < selection.Count / 2)
                        {
                            // the difference is over half so just kill the selection and make it again
                            clearSelection();

                            highlightGlyph(startGlyph, startPoint, false);

                            highlightGlyphs(allGlyphs[allGlyphs.IndexOf(startGlyph) - 1], newEndGlyph);

                            highlightGlyph(newEndGlyph, e.GetPosition(getPanel(newEndGlyph)), true);
                        }
                        else
                        {
                            for (int index = indexNewEndGlyph; index < indexEndGlyph + 1; index--)
                            {
                                removeLastSelectionRectangle();
                            }
                            highlightGlyph(newEndGlyph, e.GetPosition(getPanel(newEndGlyph)), true);
                        }
                    }
                    else if (indexEndGlyph > indexNewEndGlyph)
                    {
                        //we have added more selection

                        removeLastSelectionRectangle();
                        if (selection.Count == 0)
                        {
                            //This is a special case because we don't want to highlight all of this line
                            //like every other line but we just want to highlight where we started to the end
                            highlightGlyph(endGlyph, startPoint, false);
                            endGlyph = allGlyphs[allGlyphs.IndexOf(endGlyph) - 1];
                        }
                        highlightGlyphs(endGlyph, newEndGlyph);
                        highlightGlyph(newEndGlyph, e.GetPosition(getPanel(newEndGlyph)), true);
                    }
                    else
                    {
                        removeLastSelectionRectangle();
                        highlightGlyph(newEndGlyph, e.GetPosition(getPanel(newEndGlyph)), true);
                    }
                }
                else if (indexNewEndGlyph > indexStartGlyph)
                {
                    if (!highlightingDown)
                    {
                        clearSelection();
                        highlightingDown = true;
                    }

                    //we are going down
                    if (indexEndGlyph > indexNewEndGlyph)
                    {
                        //we have gone backwards from where we were
                        if (indexEndGlyph - indexNewEndGlyph > selection.Count / 2)
                        {
                            // the difference is over half so just kill the selection and make it again
                            clearSelection();

                            highlightGlyph(startGlyph, startPoint, true);

                            highlightGlyphs(allGlyphs[allGlyphs.IndexOf(startGlyph) + 1], newEndGlyph);

                            highlightGlyph(newEndGlyph, e.GetPosition(getPanel(newEndGlyph)), false);

                            fakemergeRectangles();
                        }
                        else
                        {
                            for (int index = indexNewEndGlyph; index < indexEndGlyph + 1; index++)
                            {
                                removeLastSelectionRectangle();
                            }
                            highlightGlyph(newEndGlyph, e.GetPosition(getPanel(newEndGlyph)), false);
                        }
                    }
                    else if (indexEndGlyph < indexNewEndGlyph)
                    {
                        //we have added more selection

                        removeLastSelectionRectangle();
                        if (selection.Count == 0)
                        {
                            //This is a special case because we don't want to highlight all of this line
                            //like every other line but we just want to highlight where we started to the end
                            highlightGlyph(endGlyph, startPoint, true);
                            endGlyph = allGlyphs[allGlyphs.IndexOf(endGlyph) + 1];
                        }
                        highlightGlyphs(endGlyph, newEndGlyph);
                        highlightGlyph(newEndGlyph, e.GetPosition(getPanel(newEndGlyph)), false);
                        fakemergeRectangles();
                    }
                    else
                    {
                        removeLastSelectionRectangle();
                        highlightGlyph(newEndGlyph, e.GetPosition(getPanel(newEndGlyph)), false);
                    }
                }
                else
                {
                    //they are the same glyph

                    //count decreases that is why we not increment i
                    for (int i = 0; i < selection.Count; removeLastSelectionRectangle()) ;

                    //then highlight part of that glyph
                    highlightGlyph(startGlyph, startPoint, e.GetPosition(getPanel(startGlyph)));
                }

                endGlyph = newEndGlyph;
            }
        }

        #endregion MouseEvents
    }
}