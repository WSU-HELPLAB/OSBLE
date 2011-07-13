using System;
using System.Xml;

namespace ReviewInterfaceBase.ViewModel.Comment.Location
{
    public class VideoLocation : ILocation
    {
        TimeSpan location;

        public TimeSpan Location
        {
            get { return location; }
            set { location = value; }
        }

        public VideoLocation(TimeSpan location)
        {
            Location = location;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Location");
            writer.WriteAttributeString("TimeIndex", location.ToString());
            writer.WriteEndElement();
        }
    }
}