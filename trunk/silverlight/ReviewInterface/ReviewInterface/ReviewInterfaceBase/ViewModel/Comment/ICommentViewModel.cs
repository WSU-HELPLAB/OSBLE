using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using ReviewInterfaceBase.View.Comment;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Comment
{
    public interface ICommentViewModel
    {
        event RoutedEventHandler GotFocus;
        event RoutedEventHandler LostFocus;
        event EventHandler Maximize;
        event MouseButtonEventHandler Minimize;
        event MouseButtonEventHandler Moving;
        event TextChangedEventHandler NoteTextChanged;
        event PropertyChangedEventHandler PropertyChanged;
        event EventHandler Remove;
        event MouseButtonEventHandler Resizing;
        event EventHandler SizeChanged;

        Brush BorderBrush { get; set; }

        string Header { get; set; }

        Brush HeaderBrush { get; set; }

        double Height { get; set; }

        Brush LineBrush { get; set; }

        Point Location { get; set; }

        Brush NoteBrush { get; set; }

        string NoteText { get; set; }

        ILocation referenceLocation { get; }

        Size Size { get; set; }

        List<FrameworkElement> SnippetHighlighting { get; set; }

        List<Line> SnippetToCommentLine { get; set; }

        bool UsingView { get; set; }

        double Width { get; set; }

        Brush TextBrush { get; set; }

        CollapsedCommentView GetCollapsedView();

        Label GetToolTipView();

        AbstractCommentView GetView();

        void XmlWrite(XmlWriter writer);
    }
}