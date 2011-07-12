using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.View.Document;
using ReviewInterfaceBase.ViewModel.Comment.Location;
using ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language;
using ReviewInterfaceBase.ViewModel.FindWindow;

namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument
{
    public class TextDocumentViewModel : ISpatialDocumentViewModel
    {
        /// <summary>
        /// This fires whenever the content has been updated
        /// </summary>
        public event EventHandler ContentUpdated = delegate { };

        /// <summary>
        /// This fires whenever the review item selection has changed.
        /// </summary>
        public event EventHandler ReviewItemSelectedChanged = delegate { };

        #region Fields

        private int documentID;
        private TextDocumentView thisView;
        private Canvas HighlightOverlay;
        private HighlightingRichTextBox selectionHighlighting = new HighlightingRichTextBox();

        private List<Rectangle> currentSelection;
        private List<RowDefinition> lines = new List<RowDefinition>();
        private List<StackPanel> children = new List<StackPanel>();

        private bool reviewItemSelected;

        private bool isDisplayed = false;

        #endregion Fields

        #region Properteis

        /// <summary>
        /// This needs to updated whenever the View of this ViewModel is being displayed.  We cannot rely on the Silverlight Events as they don't work with a tab view
        /// </summary>
        public bool IsDisplayed
        {
            get
            {
                return isDisplayed;
            }
            set
            {
                isDisplayed = value;
            }
        }

        /// <summary>
        /// This gets the documentID for the current document
        /// </summary>
        public int DocumentID
        {
            get { return documentID; }
        }

        /// <summary>
        /// This returns a List of RowDefinitions which is equal to the number of lines of text we have
        /// </summary>
        public List<RowDefinition> Lines
        {
            get { return lines; }
        }

        /// <summary>
        /// This returns a list of StackPanels which is equal to the number of lines of text we have
        /// </summary>
        public List<StackPanel> Children
        {
            get { return children; }
        }

        /// <summary>
        /// This is gets or sets whether or not there is a reviewable object selected. Note it can only be set to false
        /// it can never be set to true and doing so will cause it to throw an exception
        /// </summary>
        public bool ReviewItemSelected
        {
            get { return reviewItemSelected; }
            set
            {
                if (value == true)
                {
                    throw new MemberAccessException("This can not be set to true can only be set to false");
                }
                else
                {
                    reviewItemSelected = value;
                    thisView.DocumentViewer.Selection.Select(thisView.DocumentViewer.Selection.Start, thisView.DocumentViewer.Selection.Start);
                }
            }
        }

        /// <summary>
        /// First yes this is supposed to be a private Property.  This lets us use this to set the contents of the the
        /// RictTextBox in the View without having to worry about the underlying logic
        /// </summary>
        private Paragraph ContentBlock
        {
            get
            {
                if (thisView.DocumentViewer.Blocks != null && thisView.DocumentViewer.Blocks.Count > 0)
                {
                    return thisView.DocumentViewer.Blocks[0] as Paragraph;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                thisView.DocumentViewer.Blocks.Clear();
                thisView.DocumentViewer.Blocks.Add(value);
            }
        }

        #endregion Properteis

        #region Constructor

        /// <summary>
        /// This makes a TextFileDocument, It will make the view and the model and both
        /// can be referenced from the ViewModel.  This is the same for all ViewModels
        /// </summary>
        /// <param name="streamReader">Must viewLocation to a readable file</param>
        public TextDocumentViewModel(int documentID, StreamReader streamReader, string FileExtension)
        {
            if (streamReader == null)
            {
                throw new ArgumentNullException("streamReader");
            }

            this.documentID = documentID;

            //Create a new view for this ViewModel
            thisView = new TextDocumentView();

            //Set up the needed event handlers
            thisView.DocumentViewer.GotFocus += new RoutedEventHandler(DocumentViewer_GotFocus);
            thisView.DocumentViewer.LostFocus += new RoutedEventHandler(DocumentViewer_LostFocus);
            thisView.DocumentViewer.LayoutUpdated += new EventHandler(DocumentViewer_LayoutUpdated);
            thisView.DocumentViewer.SelectionChanged += new RoutedEventHandler(DocumentViewer_SelectionChanged);

            //find out what language we are using from the file extension
            ILanguage language = LanguageFactory.LanguageFromFileExtension(FileExtension);

            //check to see if we are using a none language
            if (!(language is NullLanguage))
            {
                //we are using a language we know about so use the SyntaxHighlighter to make the content for the RichTextBox
                SyntaxHighlighting sh = new SyntaxHighlighting(streamReader, language);

                ContentBlock = sh.Highlight();
            }
            else
            {
                ContentBlock = InitializeDocumentViewer(streamReader);
            }

            //since we cannot get this information until the ContentBlock as actually happened which wont be until
            //after the UI thread notices for now we will return dummy data for lines and children and we will
            //set it for real when it gets updated
            lines.Add(new RowDefinition());
            children.Add(new StackPanel());
        }

        /// <summary>
        /// This initializes the RichTextBox with the information from the fileStream, this should only be called from the
        /// constructor as the content is not allowed to change after the class has been made
        /// </summary>
        /// <param name="fileStream"></param>
        private Paragraph InitializeDocumentViewer(StreamReader fileStream)
        {
            string s;
            Run run;
            Paragraph para = new Paragraph();
            this.thisView.DocumentViewer.IsReadOnly = true;
            thisView.DocumentViewer.Blocks.Clear();

            while (!fileStream.EndOfStream)
            {
                s = fileStream.ReadLine();
                run = new Run();
                run.Text = s;
                para.Inlines.Add(run);
                para.Inlines.Add(new LineBreak());
            }

            fileStream.Close();

            return para;
        }

        #endregion Constructor

        #region Public Methods

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("ID", documentID.ToString());
        }

        /// <summary>
        /// This returns a reference to the View
        /// </summary>
        /// <returns>The View</returns>
        public IDocumentView GetView()
        {
            return (thisView);
        }

        public object FindNext(object foundLast, FindWindowOptions options)
        {
            int index = 0;

            if (foundLast as Run != null)
            {
                //plus one because we want to start looking at the next run
                index = ContentBlock.Inlines.IndexOf(foundLast as Run) + 1;
            }

            while (index < ContentBlock.Inlines.Count)
            {
                Inline line = ContentBlock.Inlines[index];
                if (line is Run)
                {
                    Run run = line as Run;
                    if (run.Text.Contains(options.LookingFor))
                    {
                        string text = run.Text;
                        int indexOfStartOfString = run.Text.IndexOf(options.LookingFor);

                        /*
                        //remove everything after the start of the string we are looking for
                        run.Text = run.Text.Remove(indexOfStartOfString);
                        Run containsString = new Run();
                        containsString.Text = text;

                        //remove everything before the string we are looking for
                        containsString.Text = containsString.Text.Remove(0, indexOfStartOfString);

                        //remove everything after the string we are looking for
                        containsString.Text = containsString.Text.Remove(indexOfStartOfString + options.LookingFor.Length);

                        Run afterString = new Run();

                        //remove everything before the end of  the string
                        afterString.Text = text.Remove(0, indexOfStartOfString + options.LookingFor.Length);

                        //now our string is in three parts run, containsString, afterString
                        //run is always in the RTB so got to add the other two in order

                        TextPointer start = run.ElementStart.GetPositionAtOffset(run.Text.IndexOf(options.LookingFor) + 1, LogicalDirection.Forward);
                        TextPointer end = run.ElementStart.GetPositionAtOffset(run.Text.IndexOf(options.LookingFor) + options.LookingFor.Length + 1, LogicalDirection.Forward);

                        selectionHighlighting.TextStart = start;
                        selectionHighlighting.TextEnd = end;
                        selectionHighlighting.Opacity = 1;

                        //TO DO: save the textboxes it gives us so we can get rid of it later
                        selectionHighlighting.Update();*/
                        return line;
                    }
                }
                index++;
            }
            return null;
        }

        #endregion Public Methods

        /// <summary>
        /// This is fired whenever the RichTextBoxs Selection has been changed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void DocumentViewer_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //This creates our own highlighting for the DocumentView Selected text using selectionHighlighting
            if (currentSelection != null)
            {
                selectionHighlighting.ClearHighlighting(currentSelection);
            }
            selectionHighlighting.TextStart = thisView.DocumentViewer.Selection.Start;
            selectionHighlighting.TextEnd = thisView.DocumentViewer.Selection.End;
            selectionHighlighting.ContentStart = thisView.DocumentViewer.ContentStart;
            selectionHighlighting.Canvas = HighlightOverlay;
            //selectionHighlighting.FillBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            selectionHighlighting.FillBrush = new SolidColorBrush(Color.FromArgb(100, 100, 175, 200));

            selectionHighlighting.StrokeBrush = new SolidColorBrush(Colors.Transparent);
            selectionHighlighting.Opacity = 0;
            try
            {
                currentSelection = selectionHighlighting.Update();
            }
            catch
            {
                currentSelection = null;
            }

            if (currentSelection == null || currentSelection.Count == 0)
            {
                reviewItemSelected = false;
            }
            else
            {
                reviewItemSelected = true;
            }

            ReviewItemSelectedChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// This fires whenever the layout has been updated although probably not needed anymore
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DocumentViewer_LayoutUpdated(object sender, EventArgs e)
        {
            HighlightOverlay = thisView.DocumentViewer.Descendents().OfType<Canvas>().FirstOrDefault(elem => elem.Name == "HighlightOverlay");
        }

        /// <summary>
        /// Given a textPointer this finds the line number
        /// </summary>
        /// <param name="contentPtr"></param>
        /// <returns></returns>
        private Tuple<int, int> findContentLineNumber(TextPointer contentPtr)
        {
            int lineNumber = 0;
            
            // vars for compensating for multiple run Lines
            Inline firstRunOfLine = null;
            bool isNewline = true;

            foreach (Inline inline in ContentBlock.Inlines)
            { 
                if (inline is Run)
                {
                    // checking to see if it is the first run of a line
                    if (isNewline)
                    {
                        firstRunOfLine = inline;
                        isNewline = false;
                    }

                    if (inline == contentPtr.Parent)
                    {
                        break;
                    }
                }
                else if (inline is LineBreak)
                {
                    lineNumber++;
                    isNewline = true;
                }
            }
           

            Rect contentRect = contentPtr.GetCharacterRect(LogicalDirection.Forward);

            if (contentPtr.Parent is Run)
            {
                TextPointer tp = (firstRunOfLine as Run).ContentStart;

                int i = 0;
                while (contentRect.X != tp.GetCharacterRect(LogicalDirection.Forward).X)
                {
                    tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
                    i++;
                }

                return new Tuple<int, int>(lineNumber, i);
            }

            return new Tuple<int, int>(lineNumber, 0);
        }

        /// <summary>
        /// This gets the location at which the content starts which is 0,0
        /// </summary>
        /// <returns></returns>
        public Point GetContentStart()
        {
            return new Point(0, 0);
        }

        private void DocumentViewer_LostFocus(object sender, RoutedEventArgs e)
        {
            //this sets the Opacity of our selectionHighlighting because the RichTextBox's Highlighting
            //is removed when it losses focus
            selectionHighlighting.Opacity = 1;
            selectionHighlighting.UpdateOpacity(currentSelection);
        }

        private void DocumentViewer_GotFocus(object sender, RoutedEventArgs e)
        {
            //this sets the Opacity of our selectionHighlighting to 0 because it has focus
            //so we will just use its own highlighting
            selectionHighlighting.Opacity = 0;
            selectionHighlighting.UpdateOpacity(currentSelection);
        }

        /// <summary>
        /// This gets the location at which the content ends which is the width and height of the
        /// </summary>
        /// <returns></returns>
        public Size GetContentSize()
        {
            return new Size(thisView.DocumentViewer.Width, thisView.DocumentViewer.Height);
        }

        /// <summary>
        /// This loads comments from an XML file
        /// </summary>
        /// <param name="xmlComments">This should be pointing to the XElement TExt</param>
        public ISpatialLocation GetReferenceLocationFromXml(XElement location)
        {
            int textStartLine = int.Parse(location.Attribute("LocationStartLineNumber").Value);
            int textStartIndex = int.Parse(location.Attribute("LocationStartIndex").Value);
            int textEndLine = int.Parse(location.Attribute("LocationEndLineNumber").Value);
            int textEndIndex = int.Parse(location.Attribute("LocationEndIndex").Value);

            TextPointer textStartPtr = TextPointerAtLocation(textStartLine, textStartIndex);
            TextPointer textEndPtr = TextPointerAtLocation(textEndLine, textEndIndex);

            return new TextLocation(textStartPtr, textEndPtr, new Tuple<int, int>(textStartLine, textStartIndex), new Tuple<int, int>(textEndLine, textEndIndex));
        }

        public TextPointer TextPointerAtLocation(int lineNumber, int index)
        {
            var newLines = from c in ContentBlock.Inlines where c is LineBreak select c;

            TextPointer tp;
            if (lineNumber == 0)
            {
                tp = ContentBlock.ContentStart;
                tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
            }
            else
            {
                tp = newLines.ElementAt(lineNumber - 1).ElementEnd;
                tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
            }

            int count = 0;
            while (count < index)
            {
                tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
                count++;
            }
            return tp;
        }

        public IEnumerable<FrameworkElement> CreateReferenceLocationHighlighting(ILocation referenceLocation, Brush highlightColor)
        {
            selectionHighlighting.TextStart = (referenceLocation as TextLocation).LocationStart;
            selectionHighlighting.TextEnd = (referenceLocation as TextLocation).LocationEnd;
            selectionHighlighting.ContentStart = thisView.DocumentViewer.ContentStart;
            selectionHighlighting.Canvas = HighlightOverlay;
            selectionHighlighting.FillBrush = highlightColor;
            selectionHighlighting.StrokeBrush = highlightColor;
            selectionHighlighting.Opacity = 1;
            return from c
                in selectionHighlighting.Update()
                   where c is FrameworkElement
                   select c as FrameworkElement;
        }

        public void SetReferenceLocationHighlightingToFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush highlightColor)
        {
            selectionHighlighting.FillBrush = highlightColor;
            var rectangles = from c
                             in toBeHighlighted
                             where c is Rectangle
                             select c as Rectangle;
            selectionHighlighting.SolidHighlighting(rectangles);
        }

        public void SetReferenceLocationHighlightingToNotFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush outlineColor)
        {
            selectionHighlighting.StrokeBrush = outlineColor;
            var rectangles = from c
                 in toBeHighlighted
                             where c is Rectangle
                             select c as Rectangle;
            selectionHighlighting.OutLineHighLighting(rectangles);
        }

        public void RemoveReferenceLocationHighlighting(IEnumerable<FrameworkElement> toBeRemoved)
        {
            var rectangles = from c
                                     in toBeRemoved
                             where c is Rectangle
                             select c as Rectangle;
            thisView.DocumentViewer.Selection.Select(thisView.DocumentViewer.ContentStart, thisView.DocumentViewer.ContentStart);
            selectionHighlighting.ClearHighlighting(rectangles);
        }

        public ISpatialLocation GetReferenceLocation()
        {
            if (thisView.DocumentViewer.Selection != null && thisView.DocumentViewer.Selection.Text != "")
            {
                return new TextLocation(thisView.DocumentViewer.Selection, findContentLineNumber(thisView.DocumentViewer.Selection.Start), findContentLineNumber(thisView.DocumentViewer.Selection.End));
            }
            else
            {
                return null;
            }
        }
    }
}
