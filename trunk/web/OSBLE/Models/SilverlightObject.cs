using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Text;

// Thanks to Steven Hook (http://stevenhook.blogspot.com/) for much of the Silverlight embed code.

namespace OSBLE.Models
{
    public class SilverlightObject
    {
        public string XapName { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string OnSilverlightError { get; set; }
        public Color BackgroundColor { get; set; }
        public string MinimumRuntimeVersion { get; set; }
        public bool AutoUpgrade { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
        public string ParameterString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var parameter in Parameters)
                {
                    sb.Append(parameter.Key);
                    sb.Append("=");
                    sb.Append(parameter.Value);
                    if (Parameters.Count > 1)
                        sb.Append(",");
                }

                return sb.ToString();
            }
        }

        public SilverlightObject()
        {
            OnSilverlightError = "onSilverlightError";
            BackgroundColor = Color.White;
            MinimumRuntimeVersion = "4.0.50524.0";
            AutoUpgrade = true;
        }
    }
}