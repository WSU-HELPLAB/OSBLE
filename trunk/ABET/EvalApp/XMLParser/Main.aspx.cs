using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using HtmlAgilityPack;


namespace EvalApp
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void BuildCourses(object sender, EventArgs e)
        {
            //Create new plan
            EvalApp.Model.EvalPeriod newEvalPlan = new EvalApp.Model.EvalPeriod();

            //Create new HtmlWeb item
            HtmlWeb hw = new HtmlWeb();

            //Create html doc from url provided
            HtmlDocument doc = hw.Load(webPage_Courses.Value.ToString());

            //Parse the course titles
            foreach (HtmlNode course in doc.DocumentNode.SelectNodes("//span[@class='course_header']"))
            {
                //Get text
                EvalApp.Model.Course newCourse = new EvalApp.Model.Course();
                string courseTitle = course.InnerText;

                //Set new course to the text and add to eval plan
                newCourse.setTitle(courseTitle);
                newEvalPlan.addCourse(newCourse);
            }

            //Index counter
            int evalPlanIndex = 0;

            //Parse the course descriptions
            foreach (HtmlNode courseDiscrip in doc.DocumentNode.SelectNodes("//span[@class='course_data']"))
            {
                //Get the descriptions
                string courseDescrip = courseDiscrip.InnerText;
                newEvalPlan.editCourse(evalPlanIndex, "descrip", courseDescrip);
                evalPlanIndex++;
            }


        }
    }
}
