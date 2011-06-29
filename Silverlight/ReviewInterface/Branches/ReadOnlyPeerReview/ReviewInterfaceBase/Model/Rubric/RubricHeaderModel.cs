using System;
using System.Collections.Generic;

namespace ReviewInterfaceBase.Model.Rubric
{
    public class RubricHeaderModel : IModel
    {
        public event EventHandler LoadCompleted;

        private string description = "Performance Criterion";
        private string criterionWeight = "Criterion Weight  (%)";
        private List<string> levelDescription;
        private string score = "Select Score";

        private string comment = "Comments";

        public string Description
        {
            get { return description; }
        }

        public string CriterionWeight
        {
            get { return criterionWeight; }
        }

        public string Score
        {
            get { return score; }
        }

        public List<string> LevelDescriptions
        {
            get { return levelDescription; }
        }

        public string Comment
        {
            get { return comment; }
        }

        public RubricHeaderModel(List<string> levelDescription)
        {
            if (levelDescription == null)
            {
                throw new ArgumentNullException("levelDescription");
            }
            this.levelDescription = levelDescription;
        }

        public void Load()
        {
            //we dont actually load anything so just fire LoadCompleted
            LoadCompleted(this, EventArgs.Empty);
        }

        public void Save()
        {
            //do nothing
        }
    }
}