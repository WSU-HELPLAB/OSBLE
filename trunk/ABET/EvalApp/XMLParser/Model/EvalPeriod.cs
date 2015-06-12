using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.ObjectModel;
namespace EvalApp.Model
{
    public class EvalPeriod
    {
        private ObservableCollection<Course> Courses;

        public EvalPeriod ()
        {
        }

        public void addCourse(Course _course)
        {
            Courses.Add(_course);
        }

        public void removeCourse(Course _course)
        {
            Courses.Remove(_course);
        }

        public void editCourse(int index, string attribute, string value)
        {
            if (attribute == "descrip")
                this.Courses[index].setDescription(value);
            else if (attribute == "title")
                this.Courses[index].setTitle(value);
            else if (attribute == "prereq")
                this.Courses[index].setPreReqs(value);
            else
                return;
        }
    }
}