using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace AbetApp.Models
{
    static public class ParseData
    {
        public static string ParseTitle(string data)
        {
            Regex parentheses = new Regex(@" ?\(.*?\)");
            Regex brackets = new Regex(@" ?\[.*?\]");
            Regex nums = new Regex(@"\d");

            Match mParentheses = parentheses.Match(data);
            if (mParentheses.Success)
            {
                data = Regex.Replace(data, @" ?\(.*?\)", string.Empty);
            }

            Match mNums = nums.Match(data);
            if (mNums.Success)
            {
                data = Regex.Replace(data, @"\d", string.Empty);
            }

            Match mBrackets = brackets.Match(data);
            if (mBrackets.Success)
            {
                char[] delimiterChars = { ' ', ',', '.', ':', '\t' };
                string[] values = data.Split(delimiterChars);
                string newValues = "";
                for (int i = 0; i < values.Count(); i++)
                {
                    if (i >= 3)
                    {
                        newValues += (" " + values[i]);
                    }
                }
                data = newValues;
            }


            return data;

        }

        public static int ParseCourseNum(string data)
        {
            int coursenum = 0;
            char[] delimiterChars = { ' ', ',', '.', ':', '\t' };
            string[] values = data.Split(delimiterChars);
            coursenum = Convert.ToInt32(values[0]);
            return coursenum;
        }

        public static void ParsePreReq(Dictionary<int, Course> CourseDict, List<Course> CourseList)
        {
            char[] delimiterChars = { '.' };
            char[] delimiterChars2 = { ' ', ',', '.', ':', '\t' };
            string data = "";

            using (LocalContext db = new LocalContext())
            {
                foreach (Course course in CourseList)
                {
                    data = course.Data;
                    string[] values = data.Split(delimiterChars);
                    string preReqs = values[0].ToString();
                    string[] preReqValues = preReqs.Split(delimiterChars2);
                    for (int i = 0; i < preReqValues.Count(); i++)
                    {
                        if ((preReqValues[i] == "CPT") || (preReqValues[i] == "CptS"))
                        {
                            CourseRelation courseRelation = new CourseRelation();
                            int ParentCourseNum = Convert.ToInt32(preReqValues[i + 1]);
                            Course parentCourse = CourseDict[ParentCourseNum];
                            int parentId = parentCourse.Id;
                            courseRelation.ParentCourseId = parentId;
                            courseRelation.ChildCourseId = course.Id;
                            db.CourseRelations.Add(courseRelation);
                        }
                    }
                }
                db.SaveChanges();
            }
        }
    }
}