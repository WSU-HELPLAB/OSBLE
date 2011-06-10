using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

/*
 * jquery-tooltip.css and jquery-tooltip.js are included 
 *   in the /Views/Shared/_Layout.cshtml file
 * 
 * 
 * 
 * USE AS FOLLOWS:
 * @Html.Partial
 * 
 * See /Views/Shared/_Popup.cshtml for code
 * 
 * 
 * KNOWN BUGS:
 *   doesn't work in IE
 *   doesn't show sides when width is wider than declared
 *     (if width isn't declared, it is as close together as possible, even one word per line)
 *   location of the popup (last to change for debugging)
 * 
 */

namespace OSBLE.Models
{
    public class ToolTips
    {
        #region CourseToolTips

        private IDictionary<String,String> tips = new Dictionary<String,String>();
        

        public static string abc = "qwertyuiop";
        
        #endregion

        public ToolTips(){
            tips.Add("qaz", "<p>this is the first thing</p><p>this is the second thing</p><p>kahdsfjhdsakjfhlsakjdhfkljsahdflkjhadslkjfhlakjsdhflkjsahdflkjhsaldkjfhlsakjdhflkjashdflkjdsahlfkjfads</p>");
            tips.Add("qwerty", "abcdef");
        }


        
        public string GetTip(string id)
        {
            return "<div class='popup-bubbleInfo'>" +
                "<img class='popup-trigger' src='../../Content/images/tooltip/109_AllAnnotations_Help_16x16_72.png' alt='(?)' height='16px' width='16px' />" +
                "" +
                    "<table class='popup'> <tbody> <tr>" +
                    "<td class='topleft'> </td> <td class='top'> </td> <td class='topright'> </td>" +
                    "</tr> <tr>" +
                    "<td class='left'> </td> <td class='popup-contents'>" +
                tips[id] +
                    "</td> <td class='right'> </td> </tr> <tr>" +
                    "<td class='bottomleft'> </td> <td class='bottom'>" +
                    "<img src='../../Content/images/tooltip/bubble-tail2.png' width='30' height='29' alt='popup tail' /> </td>" +
                    "<td class='bottomright' > </td> </tr> </tbody> </table>" + 
                "" +
                "</div>";



        }
        

    }
}