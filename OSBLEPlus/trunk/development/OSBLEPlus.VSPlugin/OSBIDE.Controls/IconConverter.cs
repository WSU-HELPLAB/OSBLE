using System.Drawing;
using System.Windows.Forms;
using stdole;

namespace OSBIDE.Controls
{
    public class IconConverter : AxHost
    {
        private IconConverter()
            : base(string.Empty)
        {
        }

        public static IPictureDisp GetIPictureDispFromImage(Image image)
        {

            return (IPictureDisp)GetIPictureDispFromPicture(image);
        }
    }
}
