using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace XMLParser
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            string url = Text1.Value;
          /*  WebRequest request = System.Net.HttpWebRequest.Create(url);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string result = reader.ReadToEnd();
            reader.Close();

            
            result = Regex.Replace(result, "<script. *? </script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "<style.*? </style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "</?[a-z][a-z0-9]*[^<>]*>", "");
            result = Regex.Replace(result, "<!--(.|\\s)*?-->", "");
            result = Regex.Replace(result, "<!(.|\\s)*?>", "");
            result = Regex.Replace(result, "[\t\r\n]", " ");

            Label1.Text = result;
            */

            XmlDocument xdoc = new XmlDocument();//xml doc used for xml parsing

            xdoc.Load(
                url
                );//loading XML in xml doc

            XmlNodeList xNodelst = xdoc.DocumentElement.SelectNodes("entry");//reading node so that we can traverse thorugh the XML

      


        }
    }
}
