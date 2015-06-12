using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EvalApp.Model
{
    public class Course
    {
        private string Title;
        private string Description;
        private string PreReqs;

        public Course()
        {
            this.Title = "";
            this.Description = "";
            this.PreReqs = "";
        }

        public Course(string title, string description, string prereq)
        {
            this.Title = title;
            this.Description = description;
            this.PreReqs = prereq;
        }

        public void setTitle (string title)
        {
            this.Title = title;
        }

        public void setDescription (string descrip)
        {
            this.Description = descrip;
        }

        public void setPreReqs (string preReq)
        {
            this.PreReqs = preReq;
        }
    }
}