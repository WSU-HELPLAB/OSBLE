using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ReviewInterfaceBase.Model.Rubric
{
    public class RubicModel : IModel
    {
        private ObservableCollection<CriterionModel> criterions = new ObservableCollection<CriterionModel>();

        public ObservableCollection<CriterionModel> Criterions
        {
            get { return criterions; }
            set { criterions = value; }
        }

        private RubricHeaderModel header = new RubricHeaderModel(new List<string>(new string[] { "F-D Level: Emerging (0-6 pts)", "C-B Level: Developing (7-8 pts)", "A Level: Mastering (9-10 pts)" }));

        public RubricHeaderModel Header
        {
            get { return header; }
            set { header = value; }
        }

        private int highestScore = 10;

        public int HighestScore
        {
            get { return highestScore; }
            set { highestScore = value; }
        }
    }
}