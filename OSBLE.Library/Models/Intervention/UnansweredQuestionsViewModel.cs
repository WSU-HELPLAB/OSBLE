using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OSBLE.Models.Intervention
{
    public class UnansweredQuestionsViewModel
    {        
        public string UnansweredPostIds { get; set; }
        public InterventionItem Intervention { get; set; }

        public UnansweredQuestionsViewModel()
        {            
            UnansweredPostIds = "";
            Intervention = new InterventionItem();
        }
    }
}
