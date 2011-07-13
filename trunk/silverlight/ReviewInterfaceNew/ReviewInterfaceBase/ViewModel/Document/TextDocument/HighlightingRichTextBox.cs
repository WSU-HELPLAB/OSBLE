using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System;

namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument
{
    /// <summary>
    /// NOTE: For some reason this misses the last character on each line which I assume to be the newline char dunno why
    /// </summary>
    public class HighlightingRichTextBox
    {
        #region Fields

        private TextPointer textStart;
        private TextPointer textEnd;
        private Canvas canvas;
        private Brush fillBrush;
        private Brush strokeBrush;
        private double opacity;
        private Rect contentStart;

        #endregion Fields

        #region Properties

        public TextPointer TextStart
        {
            get { return textStart; }
            set { textStart = value; }
        }

        public TextPointer TextEnd
        {
            get { return textEnd; }
            set { textEnd = value; }
        }

        public Canvas Canvas
        {
            get { return canvas; }
            set { canvas = value; }
        }

        public Brush FillBrush
        {
            get { return fillBrush; }
            set { fillBrush = value; }
        }

        public Brush StrokeBrush
        {
            get { return strokeBrush; }
            set { strokeBrush = value; }
        }

        public double Opacity
        {
            get { return opacity; }
            set { opacity = value; }
        }

        public TextPointer ContentStart
        {
            set { contentStart = value.GetCharacterRect(LogicalDirection.Forward); }
        }

        #endregion Properties

        #region Constructor

        public HighlightingRichTextBox()
        {
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        /// This updates the rectangles' Fill with fillBrush
        /// </summary>
        /// <param name="rectangles">rectangles to be updated</param>
        public void UpdateFillColor(List<Rectangle> rectangles)
        {
            if (rectangles != null)
            {
                foreach (Rectangle rec in rectangles)
                {
                    rec.Fill = fillBrush;
                }
            }
        }

        /// <summary>
        /// This updates the rectangles' Opacity with Opacity
        /// </summary>
        /// <param name="rectangles">rectangles to be updated</param>
        public void UpdateOpacity(List<Rectangle> rectangles)
        {
            if (rectangles != null)
            {
                foreach (Rectangle rec in rectangles)
                {
                    rec.Opacity = opacity;
                }
            }
        }

        /// <summary>
        /// This removes the rectangles
        /// </summary>
        /// <param name="rectangles">rectangles to be removed</param>
        public void ClearHighlighting(IEnumerable<Rectangle> rectangles)
        {
            foreach (Rectangle rec in rectangles)
            {
                canvas.Children.Remove(rec);
            }
        }


        /// <summary>
        /// This does a full update based on the Properties of this class
        /// NOTE: TextStart, TextEnd, Canvas, FillBrush, ContentStart, StrokeBrush must all not equal null or this function does nothing
        /// </summary>
        /// <returns>A list of the generated rectangles that are over TextStart to TextEnd</returns>
        public List<Rectangle> Update()
        {
            List<Rectangle> rectangles = new List<Rectangle>();
            if (textStart != null && textEnd != null && canvas != null && fillBrush != null && contentStart != null && strokeBrush != null)
            {
                //if textend or textstart's parent is a Paragraph, move it to the next insertion position so it's parent will be a Run.
                if (textEnd.Parent is Paragraph)
                {
                    textEnd = textEnd.GetNextInsertionPosition(LogicalDirection.Forward);
                }
                if (textStart.Parent is Paragraph)
                {
                    textStart = textStart.GetNextInsertionPosition(LogicalDirection.Forward);
                }
                Rectangle rect;
                //We get the rect for the first and last char
                Rect start = textStart.GetCharacterRect(LogicalDirection.Forward);
                Rect end = textEnd.GetCharacterRect(LogicalDirection.Backward);

                while (start.Top < end.Top)
                {
                    TextPointer index;
                    if (textStart.Parent is Run)
                    {
                        index = (textStart.Parent as Run).ElementEnd;
                    }
                    else
                    {
                        //this is a bug fix until I learn how to get the position of LineBreaks
                        index = (textStart.GetNextInsertionPosition(LogicalDirection.Forward));
                        //index = (textStart.GetNextInsertionPosition(LogicalDirection.Backward));
                    }
                    rect = MakeRectange(textStart, index);
                    if (rect != null)
                    {
                        rectangles.Add(rect);
                    }
                    textStart = index.GetNextInsertionPosition(LogicalDirection.Forward);
                    start = textStart.GetCharacterRect(LogicalDirection.Forward);
                }
                
                rect = MakeRectange(textStart, textEnd);
                if (rect != null)
                {
                    rectangles.Add(rect);
                }
            }

            if (rectangles.Count == 0)
            {
                throw new Exception("Can't make comments on newlines");
            }
            return rectangles;
        }

        /// <summary>
        /// This sets the rectangles Stroke to transpernt and the the Fill to FillBrush
        /// </summary>
        /// <param name="rectangles">rectangles to be updated</param>
        public void SolidHighlighting(IEnumerable<Rectangle> rectangles)
        {
            if (rectangles != null)
            {
                foreach (Rectangle rect in rectangles)
                {
                    rect.Fill = fillBrush;
                    rect.Stroke = new SolidColorBrush(Colors.Transparent);
                }
            }
        }

        /// <summary>
        /// This sets the rectangles Fill to transpernt and the the Stroke to StrokeBrush
        /// </summary>
        /// <param name="rectangles">rectangles to be updated</param>
        public void OutLineHighLighting(IEnumerable<Rectangle> rectangles)
        {
            if (rectangles != null)
            {
                foreach (Rectangle rect in rectangles)
                {
                    rect.Fill = new SolidColorBrush(Colors.Transparent);
                    rect.Stroke = strokeBrush;
                }
            }
        }

        #endregion Methods

        #region HelperFunctions



        private Rectangle MakeRectange(TextPointer tpStart, TextPointer tpEnd)
        {
            //We get the rect for the first and last char
            if (tpStart == null || tpEnd == null)
            {
                return null;
            }
            Rect start = tpStart.GetCharacterRect(LogicalDirection.Forward);
            Rect end = tpEnd.GetCharacterRect(LogicalDirection.Forward);



            //then we make a new Rectangle and set it to the correct size, location, fillBrush and Opacity
            Rectangle rect = new Rectangle();
            //rect.IsHitTestVisible = false;

            //dont have to worry about more than one line.
            if (start.Left < end.Right)
            {
                rect.Width = end.Right - start.Left;
                rect.Height = start.Height;

                //we gotta offset it by the content starts
                rect.SetValue(Canvas.TopProperty, start.Top - contentStart.Top);
                rect.SetValue(Canvas.LeftProperty, start.Left - contentStart.Left);
                rect.Fill = fillBrush;
                rect.StrokeThickness = 2;
                rect.Opacity = opacity;
                canvas.Children.Add(rect);
                return rect;
            }
            return null;
        }

        #endregion HelperFunctions
    }
}