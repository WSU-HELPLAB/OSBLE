using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentDetails.ViewModels;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Areas.AssignmentDetails.Models;
using OSBLE.Models.Courses;
using OSBLE.Areas.AssignmentDetails.Models.TableBuilder;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models;

namespace OSBLE.Areas.AssignmentDetails.ViewModels
{
    public class AssignmentDetailsFactory
    {
        public AssignmentDetailsViewModel Bake(Assignment assignment, CourseUser client)
        {
            AssignmentDetailsViewModel viewModel = new AssignmentDetailsViewModel();
            viewModel.CurrentAssignment = assignment;
            viewModel.Client = client;

            //Build our header
            BuildHeader(viewModel);

            //build table
            BuildTable(viewModel);

            return viewModel;
        }

        private AssignmentDetailsViewModel BuildHeader(AssignmentDetailsViewModel vm)
        {
            //AC NOTE: Items will be displayed in the order in which they are added
            // to the ViewModel's HeaderViews list.  Organize accordingly.

            Assignment assignment = vm.CurrentAssignment;
            vm.HeaderBuilder = new DefaultBuilder();

            //views common to both students and teachers:
            if (assignment.Type == AssignmentTypes.DiscussionAssignment)
            {
                //add initial / final post due date information
                //TODO: this will replace the "posts due" row
                vm.HeaderBuilder = new InitialFinalDueDecorator(vm.HeaderBuilder);
                vm.HeaderViews.Add("InitialFinalDueDecorator");
            }

            //not a team evaluation assignment
            if (assignment.Type != AssignmentTypes.TeamEvaluation)
            {
                //add late policy
                vm.HeaderBuilder = new LatePolicyHeaderDecorator(vm.HeaderBuilder);
                vm.HeaderViews.Add("LatePolicyHeaderDecorator");
            }

            //teacher views
            //AC NOTE: Investigate the differences between CanGrade and CanModify
            if (vm.Client.AbstractRole.CanGrade || vm.Client.AbstractRole.CanModify)
            {

                //add deliverable information if needed
                if (assignment.HasDeliverables)
                {
                    //list deliverables add download link
                    vm.HeaderBuilder = new TeacherDeliverablesHeaderDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeacherDeliverablesHeaderDecorator");
                }

                //is a discussion assignment
                if (assignment.Type == AssignmentTypes.DiscussionAssignment)
                {
                    if (!assignment.HasDiscussionTeams)
                    {
                        //link to classwide discussion
                        vm.HeaderBuilder = new DiscussionLinkDecorator(vm.HeaderBuilder);
                        vm.HeaderViews.Add("DiscussionLinkDecorator");
                    }
                }

                if (assignment.HasRubric)
                {
                    //add link to rubric ViewAsUneditable mode
                    vm.HeaderBuilder = new RubricDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("RubricDecorator");
                }

                //Show grading progress for all teacher views
                //add "x of y" have been published
                //if drafts exist: add "z saved as draft (publish all)
                vm.HeaderBuilder = new TeacherGradingProgressDecorator(vm.HeaderBuilder);
                vm.HeaderViews.Add("TeacherGradingProgressDecorator");

                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    //add publish all multipliers button
                    vm.HeaderBuilder = new TeamEvalGradingProgressDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeamEvalGradingProgressDecorator");
                }
            }
            else if (vm.Client.AbstractRole.CanSubmit) //students
            {
                //has teams?
                if (assignment.HasTeams)
                {
                    // add team name and list of members
                    vm.HeaderBuilder = new TeamMembersDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("TeamMembersDecorator");
                }

                //needs to submit?
                if (assignment.HasDeliverables)
                {
                    //add student submission link
                    vm.HeaderBuilder = new StudentSubmissionDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("StudentSubmissionDecorator");
                }

                //rubric?
                if (assignment.HasRubric)
                {
                    //add rubric link
                    vm.HeaderBuilder = new RubricDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("RubricDecorator");
                }
                //else
                {
                    //add grade link
                    vm.HeaderBuilder = new StudentGradeDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("StudentGradeDecorator");
                }
            }
            return vm;
        }

        private AssignmentDetailsViewModel BuildTable(AssignmentDetailsViewModel vm)
        {
            //AC NOTE: Items will be displayed in the order in which they are added
            // to the ViewModel's TableColumnHeaders dictionary.  Organize accordingly.

            Assignment assignment = vm.CurrentAssignment;
            List<IAssignmentTeam> teams = GetTeams(assignment);
            List<RubricEvaluation> rubricEvaluations = new List<RubricEvaluation>();
            List<TeamEvaluation> teamEvaluations = new List<TeamEvaluation>();
            using (OSBLEContext db = new OSBLEContext())
            {
                rubricEvaluations = db.RubricEvaluations.Where(re => re.AssignmentID == assignment.ID).ToList();
                
                //AC: waiting on Mario's checkin.
                //teamEvaluations = db.TeamEvaluations.Where(te => te.AssignmentID == assignment.ID).ToList();
            }

            //create a builder for each team
            foreach (IAssignmentTeam assignmentTeam in teams)
            {
                Team team = assignmentTeam.Team;
                vm.TeamTableBuilders[assignmentTeam] = new DefaultBuilder();

                if (assignment.HasDeliverables)
                {
                    //display submission information
                    vm.TeamTableBuilders[assignmentTeam] = new DeliverablesTableDecorator(vm.TeamTableBuilders[assignmentTeam]);
                    vm.TableColumnHeaders["DeliverablesTableDecorator"] = "Submission";
                }

                if (assignment.HasCommentCategories)
                {
                    //link to inline review
                    vm.TeamTableBuilders[assignmentTeam] = new InlineReviewTableDecorator(vm.TeamTableBuilders[assignmentTeam]);
                    vm.TableColumnHeaders["InlineReviewTableDecorator"] = "Inline Review";
                }

                if (assignment.Type == AssignmentTypes.DiscussionAssignment)
                {
                    List<DiscussionPost> allUserPosts;
                    using (OSBLEContext db = new OSBLEContext())
                    {
                        allUserPosts = (from a in db.DiscussionPosts
                                        where a.AssignmentID == assignmentTeam.Assignment.ID
                                        select a).ToList();
                    }

                    //add post count
                    vm.TeamTableBuilders[assignmentTeam] = new DiscussionPostsTableDecorator(vm.TeamTableBuilders[assignmentTeam], allUserPosts);
                    vm.TableColumnHeaders["DiscussionPostsTableDecorator"] = "Posts";

                    //add reply count
                    vm.TeamTableBuilders[assignmentTeam] = new DiscussionRepliesTableDecorator(vm.TeamTableBuilders[assignmentTeam], allUserPosts);
                    vm.TableColumnHeaders["DiscussionRepliesTableDecorator"] = "Replies";

                    //add total count
                    vm.TeamTableBuilders[assignmentTeam] = new DiscussionTotalTableDecorator(vm.TeamTableBuilders[assignmentTeam], allUserPosts);
                    vm.TableColumnHeaders["DiscussionTotalTableDecorator"] = "Total";
                 
                }

                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    //AC TODO: Rewrite after I get new team evaluation model info.
                    /*
                    List<TeamEvaluation> evaluations = teamEvaluations.Where(te => te.TeamID == te.TeamID).ToList();

                    //add team evaluation progress
                    vm.TeamTableBuilders[assignmentTeam] = new TeamEvaluationProgressTableDecorator(
                                                                vm.TeamTableBuilders[assignmentTeam],
                                                                evaluations
                                                                );
                    vm.TableColumnHeaders["TeamEvaluationProgressTableDecorator"] = "Evaluations Completed";
                    
                    //add largest discrepency info
                    vm.TeamTableBuilders[assignmentTeam] = new TeamEvaluationDiscrepancyTableDecorator(
                                                                vm.TeamTableBuilders[assignmentTeam],
                                                                new List<TeamMemberEvaluation>()
                                                                );
                    vm.TableColumnHeaders["TeamEvaluationDiscrepancyTableDecorator"] = "Largest Discrepancy";
                     * */
                }
                else
                {
                    //add grade info
                    vm.TeamTableBuilders[assignmentTeam] = new GradeTableDecorator(
                                                                vm.TeamTableBuilders[assignmentTeam],
                                                                rubricEvaluations
                                                                );
                    vm.TableColumnHeaders["GradeTableDecorator"] = "Grade";

                    //add late penalty info
                    vm.TeamTableBuilders[assignmentTeam] = new LatePenaltyTableDecorator(
                                                                vm.TeamTableBuilders[assignmentTeam]
                                                                );
                    vm.TableColumnHeaders["LatePenaltyTableDecorator"] = "Late Penalty";
                }
            }
            
            return vm;
        }

        private List<IAssignmentTeam> GetTeams(Assignment assignment)
        {
            List<IAssignmentTeam> teams = new List<IAssignmentTeam>();

            switch (assignment.Type)
            {
                case AssignmentTypes.TeamEvaluation:
                    teams = assignment.PreceedingAssignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList();
                    break;

                case AssignmentTypes.DiscussionAssignment:

                    //AC TODO: this isn't right.  change
                    teams = assignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList();
                    break;

                case AssignmentTypes.Basic:
                case AssignmentTypes.CriticalReview:
                case AssignmentTypes.CriticalReviewDiscussion:
                    teams = assignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList();
                    break;
            }
            return teams;
        }
    }
}
