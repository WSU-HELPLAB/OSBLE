using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

/*
 * JQUERY TOOL TIP
 * 
 * USAGE:
 * place 
 *   @Helpers.CreateToolTip(ToolTips.[variableName])
 * wherever you want a tooltip icon. You must place
 *   @using OSBLE.Models
 * once in the cshtml file you are using to get access
 * to this class and intellisense. HTML is supported.
 * 
 * 
 * jquery-tooltip.css and jquery-tooltip.js are included 
 *   in the /Views/Shared/_Layout.cshtml file
 * 
 * See /App_Code/Helpers.cshtml for code
 * 
 * KNOWN BUGS:
 *   in IE, if an individual item is wider than 312 px (such as a string with no spaces), the sides are not visible 
 * 
 */

namespace OSBLE.Models
{
    public static class ToolTips
    {

        #region CourseToolTips

        public static string abc = "<p>this is the first thing</p><p>this <a href='http://xkcd.com/627/'>is the</a> second thing</p> " +
            "<p>kahdsfjjdaadsfdshskfjhksajhflkjsahlfdkjsahkjhdflfkjfads</p> abcdef ghijkl <em>m no p asdf</em> asdf asdf <strong>asdf " +
            "</strong>asdf asdf asdf  asdf sadf <u>sadf <i>asdf <b>asdf</b> sadf</i> asd</u> fasdf <br />asdf asdf asdf asd fasdf asdf";
        
        public static string asdf = " This is just a little bit of text, as a tool tip should be. ";

        #endregion

        

    }
}