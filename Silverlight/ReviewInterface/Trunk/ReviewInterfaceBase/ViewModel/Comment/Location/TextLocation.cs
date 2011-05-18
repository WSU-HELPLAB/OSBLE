using System;
using System.Windows.Documents;
using System.Xml;

namespace ReviewInterfaceBase.ViewModel.Comment.Location
{
    public class DocumentLocation : ISpatialLocation
    {
        TextPointer locationStart;

        public TextPointer LocationStart
        {
            get { return locationStart; }
        }

        TextPointer locationEnd;

        public TextPointer LocationEnd
        {
            get { return locationEnd; }
        }

        Tuple<int, int> endLineAndIndex;

        public Tuple<int, int> EndLineAndIndex
        {
            get { return endLineAndIndex; }
        }

        Tuple<int, int> startLineAndIndex;

        public Tuple<int, int> StartLineAndIndex
        {
            get { return startLineAndIndex; }
        }

        public DocumentLocation(TextSelection selection, Tuple<int, int> startLineAndIndex, Tuple<int, int> endLineAndIndex)
        {
            if (selection == null)
            {
                throw new ArgumentNullException("selection");
            }
            locationStart = selection.Start;
            locationEnd = selection.End;
            this.startLineAndIndex = startLineAndIndex;
            this.endLineAndIndex = endLineAndIndex;
        }

        public DocumentLocation(TextPointer locationStart, TextPointer locationEnd, Tuple<int, int> startLineAndIndex, Tuple<int, int> endLineAndIndex)
        {
            if (locationStart == null)
            {
                throw new ArgumentNullException("locationStart");
            }
            else if (locationEnd == null)
            {
                throw new ArgumentNullException("locationEnd");
            }
            this.locationStart = locationStart;
            this.locationEnd = locationEnd;
            this.startLineAndIndex = startLineAndIndex;
            this.endLineAndIndex = endLineAndIndex;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Location");

            writer.WriteAttributeString("LocationStartLineNumber", StartLineAndIndex.Item1.ToString());
            writer.WriteAttributeString("LocationStartIndex", StartLineAndIndex.Item2.ToString());
            writer.WriteAttributeString("LocationEndLineNumber", EndLineAndIndex.Item1.ToString());
            writer.WriteAttributeString("LocationEndIndex", EndLineAndIndex.Item2.ToString());
            writer.WriteEndElement();
        }
    }
}