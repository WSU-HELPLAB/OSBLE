using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.View.Document;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Document
{
    /// <summary>
    /// This is an interface for anything that has a spatial layout such as text, or ChemProV.  Video is not spatial it is a chronological
    /// </summary>
    interface ISpatialDocumentViewModel : IDocumentViewModel
    {
        event EventHandler ReviewItemSelectedChanged;
        event EventHandler ContentUpdated;

        List<StackPanel> Children { get; }

        List<RowDefinition> Lines { get; }

        bool IsDisplayed { get; set; }

        bool ReviewItemSelected { get; set; }

        IDocumentView GetView();

        Size GetContentSize();

        ISpatialLocation GetReferenceLocation();

        ISpatialLocation GetReferenceLocationFromXml(XElement Comment);

        IEnumerable<FrameworkElement> CreateReferenceLocationHighlighting(ILocation referenceLocation, Brush highlightColor);

        void RemoveReferenceLocationHighlighting(IEnumerable<FrameworkElement> toBeRemoved);

        void SetReferenceLocationHighlightingToFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush highlightColor);

        void SetReferenceLocationHighlightingToNotFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush outlineColor);

        void WriteXml(XmlWriter writer);
    }
}