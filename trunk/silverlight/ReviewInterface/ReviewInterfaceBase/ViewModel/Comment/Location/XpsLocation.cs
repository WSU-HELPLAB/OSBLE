using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace ReviewInterfaceBase.ViewModel.Comment.Location
{
    public class XpsLocation : ISpatialLocation
    {
        private List<Rectangle> rectangles;

        private int pageNumber;

        public int PageNumber
        {
            get { return pageNumber; }
        }

        public List<Rectangle> Rectangles
        {
            get { return rectangles; }
        }

        public XpsLocation(List<Rectangle> rectangles, int pageNumber)
        {
            this.rectangles = rectangles;
            this.pageNumber = pageNumber;
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("Location");

            writer.WriteAttributeString("PageNumber", pageNumber.ToString());

            foreach (Rectangle rectangle in rectangles)
            {
                writer.WriteStartElement("Rectangle");

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
            writer.WriteEndElement();
        }
    }
}