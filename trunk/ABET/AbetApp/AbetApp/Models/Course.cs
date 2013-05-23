using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AbetApp.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string CourseNum { get; set; }
        public List<Course> PreReq { get; set; }
        public string Description { get; set; }
        public string Outcomes { get; set; }
        public string YearSemester { get; set; }
        public void ParsePreReq()
        {
            char[] delimiterChars = { ' ', ',', '.', ':', '\t' };
            string[] values = this.Description.Split(delimiterChars);
            for(int i = 0; i <= values.Count(); i++)
            {
                if (values[i] == "ACCTG")
                {
                    i++;
                    string tmp = values[i];
                    bool isLetter = !string.IsNullOrEmpty(tmp) && char.IsLetter(tmp[0]);
                    while (isLetter != true)
                    {
                        Course tmpCourse = new Course();
                        tmpCourse.Title = "ACCTG " + tmp + " ";
                        tmpCourse.CourseNum = tmp;
                        this.PreReq.Add(tmpCourse);
                        i++;
                        tmp = values[i];
                        isLetter = !string.IsNullOrEmpty(tmp) && char.IsLetter(tmp[0]);
                    }
                }
            }
        }
    }
}