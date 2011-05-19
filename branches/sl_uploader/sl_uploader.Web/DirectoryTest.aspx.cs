using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

namespace sl_uploader.Web
{
    public partial class DirectoryTest : System.Web.UI.Page
    {
        private string filePath;
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Write("hello");
            filePath = HttpContext.Current.Server.MapPath("Files");
            foreach (string item in Directory.GetFiles(filePath))
            {
                Response.Write(File.GetCreationTimeUtc(item) + "<br />");
            }
        }
    }
}