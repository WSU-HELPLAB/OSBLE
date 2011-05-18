using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using ChemProV.PFD;
using ChemProV.PFD.EquationEditor;
using ChemProV.PFD.Streams.PropertiesWindow;
using ChemProV.UI.DrawingCanvas;
using ChemProV.Validation.Feedback;
using ReviewInterfaceBase.View.Document;
using ReviewInterfaceBase.ViewModel.Comment.Location;
using ReviewInterfaceBase.ViewModel.FindWindow;
using ChemProV.PFD.Streams;

namespace ReviewInterfaceBase.ViewModel.Document.ChemProVDocument
{
    public class ChemProVDocumentViewModel : ISpatialDocumentViewModel
    {
        ChemProVDocumentView thisView;

        private int documentID;

        private bool reviewItemSelected = false;

        private object selectedReviewItem;

        private bool isDisplayed = false;

        public bool IsDisplayed
        {
            get { return isDisplayed; }
            set { isDisplayed = value; }
        }

        public int DocumentID
        {
            get { return documentID; }
        }

        public object SelectedReviewItem
        {
            get { return selectedReviewItem; }
            set
            {
                if (selectedReviewItem != null && (selectedReviewItem is IPropertiesWindow || selectedReviewItem is Feedback || selectedReviewItem is Equation))
                {
                    Border border = (selectedReviewItem as UserControl).Content as Border;
                    border.BorderThickness = new Thickness(0);
                    border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }
                selectedReviewItem = value;
                if (selectedReviewItem != null)
                {
                    reviewItemSelected = true;
                    Border border = null;
                    if (selectedReviewItem is IPropertiesWindow || selectedReviewItem is Feedback || selectedReviewItem is Equation)
                    {
                        border = (selectedReviewItem as UserControl).Content as Border;
                        border.BorderThickness = new Thickness(2);
                        border.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    }
                }
                else
                {
                    reviewItemSelected = false;
                }
                ReviewItemSelectedChanged(this, EventArgs.Empty);
            }
        }

        public ChemProVDocumentViewModel(int documentID, StreamReader streamReader, string fileExtension)
        {
            if (streamReader == null)
            {
                throw new ArgumentNullException("streamReader");
            }

            if (fileExtension != ".cpml")
            {
                throw new Exception("File Extension must be .cpml");
            }

            this.documentID = documentID;

            thisView = new ChemProVDocumentView(this);

            DrawingCanvas drawingCanvas = thisView.WorkSpace.DrawingCanvasReference;
            FeedbackWindow feedbackWindow = thisView.WorkSpace.FeedbackWindowReference;
            EquationEditor equationEditor = thisView.WorkSpace.EquationEditorReference;

            loadfile(streamReader);

            drawingCanvas.UpdateCanvasSize();
            this.thisView.WorkSpace.RemoveScrollViewerFromDrawingCanvas();
            foreach (IPfdElement pfdElement in drawingCanvas.ChildIPfdElements)
            {
                (pfdElement as UserControl).Content.MouseLeftButtonDown += new MouseButtonEventHandler(PfdElement_MouseLeftButtonDown);
                //we attach it to their content as they all stop the bubbling of mouse events at the UserControl level
            }

            foreach (Feedback feedback in feedbackWindow.ListOfFeedback)
            {
                feedback.MouseLeftButtonDown += new MouseButtonEventHandler(Feedback_MouseLeftButtonDown);
            }

            foreach (Equation equation in equationEditor.Equations)
            {
                equation.MouseLeftButtonDown += new MouseButtonEventHandler(Equation_MouseLeftButtonDown);
            }

            ContentUpdated(this, EventArgs.Empty);
        }

        private void PfdElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedReviewItem = (sender as FrameworkElement).Parent as IPfdElement;
        }

        private void Feedback_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedReviewItem = sender as Feedback;
        }

        private void Equation_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedReviewItem = (sender as Equation);
        }

        private void WorkSpace_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void loadfile(StreamReader streamReader)
        {
            try
            {
                XDocument xdoc = XDocument.Load(streamReader);
                thisView.WorkSpace.IsReadOnly = true;
                thisView.WorkSpace.LoadXmlElements(xdoc);
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.ToString());
            }
        }

        public ISpatialLocation GetReferenceLocationFromXml(XElement xmlLocation)
        {
            string id = xmlLocation.Attribute("id").Value;

            string[] idSplit = id.Split('_');

            FrameworkElement fwe = null;
            //Ids are in the form ?_# where ? represents the type and the # is a number
            //? that is S = stream, GPU is process unit, Fb is feedback, Eq is equation
            switch (idSplit[0])
            {
                case "S":
                //This falls threw on purpose they both have to look in the same place
                case "GPU":
                    foreach (IPfdElement pfdElement in thisView.WorkSpace.DrawingCanvasReference.ChildIPfdElements)
                    {
                        if (id == pfdElement.Id)
                        {
                            if (idSplit[0] == "S")
                            {
                                fwe = (pfdElement as IStream).Table as FrameworkElement;
                                break;
                            }
                            else
                            {
                                fwe = pfdElement as FrameworkElement;
                                break;
                            }
                        }
                    }
                    break;

                case "Fb":
                    foreach (Feedback fb in thisView.WorkSpace.FeedbackWindowReference.ListOfFeedback)
                    {
                        if (id == fb.Id)
                        {
                            fwe = fb as FrameworkElement;
                            break;
                        }
                    }

                    break;

                case "Eq":
                    foreach (Equation eq in thisView.WorkSpace.EquationEditorReference.Equations)
                    {
                        if (id == eq.Id)
                        {
                            fwe = eq as FrameworkElement;
                            break;
                        }
                    }
                    break;
            }

            if (fwe != null)
            {
                ChemProVLocation location = new ChemProVLocation(fwe, id);
                return location;
            }

            return null;
        }

        public IDocumentView GetView()
        {
            return thisView;
        }

        public void ChemProVLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        public void ChemProVLayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        #region ISpatialDocumentViewModel Members

        public event EventHandler ReviewItemSelectedChanged = delegate { };

        public event EventHandler ContentUpdated = delegate { };

        public List<StackPanel> Children
        {
            get
            {
                List<StackPanel> list = new List<StackPanel>();
                list.Add(new StackPanel());
                return list;
            }
        }

        public List<RowDefinition> Lines
        {
            get
            {
                List<RowDefinition> list = new List<RowDefinition>();
                list.Add(new RowDefinition());
                return list;
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("ID", documentID.ToString());
        }

        public object FindNext(object lastFound, FindWindowOptions options)
        {
            return null;
        }

        public bool ReviewItemSelected
        {
            get
            {
                return reviewItemSelected;
            }
            set
            {
                if (value == true)
                {
                    throw new MemberAccessException("This can not be set to true can only be set to false");
                }
                else
                {
                    reviewItemSelected = value;
                    thisView.WorkSpace.DrawingCanvasReference.SelectedElement = null;
                }
            }
        }

        public Size GetContentSize()
        {
            return new Size(thisView.WorkSpace.Width, thisView.WorkSpace.Height);
        }

        public ISpatialLocation GetReferenceLocation()
        {
            if (reviewItemSelected == true)
            {
                string id = "";
                if (SelectedReviewItem is IPfdElement)
                {
                    id = (SelectedReviewItem as IPfdElement).Id;
                }
                else if (SelectedReviewItem is Feedback)
                {
                    id = (SelectedReviewItem as Feedback).Id;
                }
                else
                {
                    id = (SelectedReviewItem as Equation).Id;
                }

                return new ChemProVLocation(SelectedReviewItem as FrameworkElement, id);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<FrameworkElement> CreateReferenceLocationHighlighting(ILocation referenceLocation, Brush highlightColor)
        {
            Border br = (((referenceLocation as ChemProVLocation).FWElement as UserControl).Content as Border);
            //br.BorderBrush = new SolidColorBrush(Colors.Blue);
            //br.BorderThickness = new Thickness(3);
            List<FrameworkElement> borders = new List<FrameworkElement>();
            borders.Add(br);
            return borders;
        }

        public void RemoveReferenceLocationHighlighting(IEnumerable<FrameworkElement> toBeRemoved)
        {
            //(toBeRemoved.First() as Border).BorderBrush = new SolidColorBrush(Colors.Transparent);
        }

        public void SetReferenceLocationHighlightingToFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush highlightColor)
        {
            /*
            Border br = (toBeHighlighted.First() as Border);
            br.BorderThickness = new Thickness(3);
            br.Background = highlightColor;
            br.BorderBrush = highlightColor;*/
        }

        public void SetReferenceLocationHighlightingToNotFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush outlineColor)
        {
            /*
            Border br = (toBeHighlighted.First() as Border);
            br.BorderThickness = new Thickness(1);
            br.Background = new SolidColorBrush(Colors.Transparent);
            br.BorderBrush = outlineColor;*/
        }

        #endregion ISpatialDocumentViewModel Members
    }
}