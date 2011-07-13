using System;
using System.Collections.Generic;

namespace ReviewInterfaceBase.Model.Rubric
{
    public class CriterionModel : IModel
    {
        private string description;
        private double criterionWeight;
        private List<string> levelDescription;
        private int score = -1;
        private string comment = "";

        public string Description
        {
            get { return description; }
        }

        public string CriterionWeight
        {
            get { return criterionWeight.ToString(); }
        }

        public List<string> LevelDescription
        {
            get { return levelDescription; }
        }

        public int Score
        {
            get { return score; }
            set { score = value; }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        public CriterionModel(string description, double criterionWeightAsPercent, List<string> levelDescription)
        {
            this.description = description;
            this.criterionWeight = criterionWeightAsPercent;
            this.levelDescription = levelDescription;
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }
}