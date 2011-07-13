using System.Xml;

namespace ReviewInterfaceBase.ViewModel.Comment.Location
{
    public enum LocationType
    {
        Text,
        ChemProV,
        Xps,
        Video
    }

    public interface ILocation
    {
        void WriteXml(XmlWriter writer);
    }
}