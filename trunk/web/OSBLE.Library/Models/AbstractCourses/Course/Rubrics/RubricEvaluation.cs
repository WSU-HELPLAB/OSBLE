using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;
using OSBLE.Models.Users;
using System.Linq;

namespace OSBLE.Models.Courses.Rubrics
{
    public class RubricEvaluation : IModelBuilderExtender
    {
        public RubricEvaluation()
        {
            CriterionEvaluations = new List<CriterionEvaluation>();
            IsPublished = false;
        }

        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public int EvaluatorID { get; set; }

        public virtual CourseUser Evaluator { get; set; }

        [Required]
        public int RecipientID { get; set; }

        public virtual Team Recipient { get; set; }

        [Required]
        public int AssignmentID { get; set; }

        public virtual Assignment Assignment { get; set; }

        [Required]
        public bool IsPublished { get; set; }

        /// <summary>
        /// This stores the date the Evaluation was published OR daved as draft.
        /// Use IsPublished to check published status.
        /// </summary>
        public DateTime? DatePublished { get; set; }

        [StringLength(4000)]
        public string GlobalComment { get; set; }

        [Required]
        public virtual ICollection<CriterionEvaluation> CriterionEvaluations { get; set; }

        public static double GetStudentGradeAsDouble(int rubricEvaluationId)
        {
            return GetGradeHelper(rubricEvaluationId, true);
        }

        public static double GetGradeAsDouble(int rubricEvaluationId)
        {
            return GetGradeHelper(rubricEvaluationId, false);
        }

        public static string GetStudentGradeAsPercent(int rubricEvaluationId)
        {
            return GetGradeHelper(rubricEvaluationId, true).ToString("P");
        }

        /// <summary>
        /// Returns a string that is ("XX.X %") where XX.X is the rubric grade percent.
        /// </summary>
        /// <returns></returns>
        public static string GetGradeAsPercent(int rubricEvaluationId)
        {
            return GetGradeHelper(rubricEvaluationId, false).ToString("P");
        }

        private static double GetGradeHelper(int rubricEvaluationId, bool isStudent)
        {
            double gradeOnRubric = 0.0;

            using (OSBLEContext db = new OSBLEContext())
            {
                RubricEvaluation re = db.RubricEvaluations.Find(rubricEvaluationId);
                //Calculate the rubric grade as a percent to display
                if (re.CriterionEvaluations.Count > 0)
                {

                    int sumOfPointSpreads;

                    //Below we are calculating the percent received on the rubric. 
                    if (isStudent)
                    {
                        sumOfPointSpreads = (from level in re.Assignment.StudentRubric.Levels
                                                 select level.PointSpread).Sum();
                    }
                    else
                    {
                        sumOfPointSpreads = (from level in re.Assignment.Rubric.Levels
                                                 select level.PointSpread).Sum();
                    }

                    double sumOfWeights = 0;
                    double sumOfWeightedScores = 0;

                    foreach (CriterionEvaluation critEval in re.CriterionEvaluations)
                    {
                        double currentWeight = critEval.Criterion.Weight;
                        sumOfWeights += currentWeight;

                        int? currentScore = critEval.Score;

                        if (currentScore.HasValue)
                        {
                            sumOfWeightedScores += currentWeight * ((double)currentScore / (double)sumOfPointSpreads);
                        }
                    }

                    if (sumOfWeights > 0) //In case there were no weights, we dont want to divide by 0
                    {
                        gradeOnRubric = sumOfWeightedScores / sumOfWeights;
                    }
                }
            }

            return gradeOnRubric;
        }


        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RubricEvaluation>()
               .HasRequired(re => re.Recipient)
               .WithMany()
               .WillCascadeOnDelete(false);

            modelBuilder.Entity<RubricEvaluation>()
                .HasRequired(re => re.Assignment)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<RubricEvaluation>()
                .HasRequired(r => r.Evaluator)
                .WithMany()
                .WillCascadeOnDelete(true);
        }
    }
}
