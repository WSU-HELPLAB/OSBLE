using System;
using System.Windows;
using System.Xml;
using ChemProV.PFD.Streams;
using ChemProV.PFD.Streams.PropertiesWindow;

namespace ReviewInterfaceBase.ViewModel.Comment.Location
{
    public class ChemProVLocation : ISpatialLocation
    {
        private FrameworkElement fwElement;
        private string id;

        public FrameworkElement FWElement
        {
            get { return fwElement; }
        }

        public string ID
        {
            get
            {
                return id;
            }
        }

        public ChemProVLocation(FrameworkElement fwElement, string ID)
        {
            if (fwElement == null)
            {
                throw new ArgumentNullException("fwElement");
            }
            this.fwElement = fwElement;
            this.id = ID;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Location");

            //Properties Windows are not guaranteed to have unique ids so we will use its parentStream since each
            //stream must have exactly one properties table
            if (fwElement is IPropertiesWindow)
            {
                IStream stream = (fwElement as IPropertiesWindow).ParentStream;
                writer.WriteAttributeString("id", stream.Id.ToString());
            }
            else
            {
                writer.WriteAttributeString("id", id.ToString());
            }
            writer.WriteEndElement();
        }
    }
}