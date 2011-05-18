using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace ReviewInterfaceBase.ViewModel.Comment.Location
{
    public class XpsLocation : ISpatialLocation
    {
        Rectangle rectangle;

        public Rectangle Rectangle
        {
            get { return rectangle; }
        }

        public XpsLocation(Rectangle rectangle)
        {
            this.rectangle = rectangle;
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("Location");

            Point topLeft = new Point((double)rectangle.GetValue(Canvas.LeftProperty), (double)rectangle.GetValue(Canvas.TopProperty));
            Size size = new Size(rectangle.Width, rectangle.Height);

            writer.WriteStartElement("TopLeft");
            writer.WriteAttributeString("Left", topLeft.X.ToString());
            writer.WriteAttributeString("Top", topLeft.Y.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("Size");
            writer.WriteAttributeString("Width", size.Width.ToString());
            writer.WriteAttributeString("Height", size.Height.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}