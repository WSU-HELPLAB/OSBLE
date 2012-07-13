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

            List<TeamEvaluation> teamEvaluations = null;
            using (OSBLEContext db = new OSBLEContext())
            {
                //only need get these when they are needed
                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    teamEvaluations = db.TeamEvaluations.Where(te => te.TeamEvaluationAssignmentID == assignment.ID).ToList();
                }
            }


            //views common to both students and teachers:
            if (assignment.Type == AssignmentTypes.DiscussionAssignment ||
                assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
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
                if (assignment.Type == AssignmentTypes.DiscussionAssignment &&  !assignment.HasDiscussionTeams)
                {
                    //link to classwide discussion
                    vm.HeaderBuilder = new TeacherDiscussionLinkDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeacherDiscussionLinkDecorator");
                }

                if (assignment.HasRubric)
                {
                    //add link to rubric ViewAsUneditable mode
                    vm.HeaderBuilder = new RubricDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("RubricDecorator");
                }

                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    //add publish all multipliers button
                    vm.HeaderBuilder = new TeamEvalGradingProgressDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeamEvalGradingProgressDecorator");
                }
                else if (assignment.Type == AssignmentTypes.CriticalReview)
                {
                    vm.HeaderBuilder = new PublishCriticalReviewDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("PublishCriticalReviewDecorator");
                }
                else
                {
                    //Show grading progress for all teacher views
                    //add "x of y" have been published
                    //if drafts exist: add "z saved as draft (publish all)
                    vm.HeaderBuilder = new TeacherGradingProgressDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeacherGradingProgressDecorator");
                }


            }
            else if (vm.Client.AbstractRole.CanSubmit) //students
            {
                //has discussion teams?
                if (assignment.HasDiscussionTeams && assignment.DiscussionSettings!= null && !assignment.DiscussionSettings.HasAnonymousStudentsToStudents)
                {
                    vm.HeaderBuilder = new DiscussionTeamMemberDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("DiscussionTeamMemberDecorator");
                }
                else if (assignment.HasTeams)//else has teams?
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
                else if (assignment.Type == AssignmentTypes.CriticalReview)
                {
                    //critical review submission link
                    vm.HeaderBuilder = new CriticalReviewSubmissionDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("CriticalReviewSubmissionDecorator");
                    
                    //link for student to download their reviewed assignment
                    vm.HeaderBuilder = new CriticalReviewStudentDownloadDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("CriticalReviewStudentDownloadDecorator");
                }
                else if (assignment.Type == AssignmentTypes.CriticalReviewDiscussion || 
                    assignment.Type == AssignmentTypes.DiscussionAssignment)
                {
                    vm.HeaderBuilder = new StudentDiscussionLinkDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("StudentDiscussionLinkDecorator");
                }
                else if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    vm.HeaderBuilder = new StudentTeamEvalSubmissionDecorator(vm.HeaderBuilder, teamEvaluations, vm.Client);
                    vm.HeaderViews.Add("StudentTeamEvalSubmissionDecorator");

                }

                //rubric?
                if (assignment.HasRubric)
                {
                    //add rubric link
                    vm.HeaderBuilder = new RubricDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("RubricDecorator");
                }
                    //add grade link
                    vm.HeaderBuilder = new StudentGradeDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("StudentGradeDecorator");
            }

            if (assignment.Type == AssignmentTypes.CriticalReview || 
                assignment.Type == AssignmentTypes.CriticalReviewDiscussion ||
                assignment.Type == AssignmentTypes.TeamEvaluation)
            {
                vm.HeaderBuilder = new PreviousAssignmentDecorator(vm.HeaderBuilder);
                vm.HeaderViews.Add("PreviousAssignmentDecorator");
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
            List<TeamEvaluation> teamEvaluations = null;
            List<DiscussionPost> allUserPosts= null;
            using (OSBLEContext db = new OSBLEContext())
            {
                rubricEvaluations = db.RubricEvaluations.Where(re => re.AssignmentID == assignment.ID).ToList();
                //only need get these when they are needed
                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    teamEvaluations = db.TeamEvaluations.Where(te => te.TeamEvaluationAssignmentID == assignment.ID).ToList();
                }

                if(assignment.Type == AssignmentTypes.DiscussionAssignment ||
                    assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
                allUserPosts = (from a in db.DiscussionPosts
                                where a.AssignmentID == assignment.ID
                                select a).ToList();
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

                if (assignment.Type == AssignmentTypes.DiscussionAssignment ||
                    assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
                {
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

                if (assignment.Type == AssignmentTypes.CriticalReview)
                {
                    vm.TeamTableBuilders[assignmentTeam] = new CriticalReviewsReceivedDecorator(vm.TeamTableBuilders[assignmentTeam]);
                    vm.TableColumnHeaders["CriticalReviewsReceivedDecorator"] = "Reviews Received";

                    vm.TeamTableBuilders[assignmentTeam] = new CriticalReviewsPerformedDecorator(vm.TeamTableBuilders[assignmentTeam]);
                    vm.TableColumnHeaders["CriticalReviewsPerformedDecorator"] = "Reviews Performed";

                    
                }

                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    //AC TODO: Rewrite after I get new team evaluation model info.
                    
                    //Grabbing all the evaluations for the TeamEvaluation assignment
                    List<TeamEvaluation> evaluations = teamEvaluations.Where(te => te.TeamEvaluationAssignmentID == assignment.ID).ToList();

                    //add team evaluation progress
                    vm.TeamTableBuilders[assignmentTeam] = new TeamEvaluationProgressTableDecorator(
                                                                vm.TeamTableBuilders[assignmentTeam],
                                                                evaluations, 
                                                                assignment
                                                                );
                    vm.TableColumnHeaders["TeamEvaluationProgressTableDecorator"] = "Evaluation Status";
                    
                    //add largest discrepency info
                    vm.TeamTableBuilders[assignmentTeam] = new TeamEvaluationDiscrepancyTableDecorator(
                                                                vm.TeamTableBuilders[assignmentTeam],
                                                                evaluations
                                                                );
                    vm.TableColumnHeaders["TeamEvaluationDiscrepancyTableDecorator"] = "Multiplier";
                   
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

                case AssignmentTypes.Basic:
                case AssignmentTypes.CriticalReview:
                    teams = assignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList();
                    break;

                case AssignmentTypes.CriticalReviewDiscussion:
                case AssignmentTypes.DiscussionAssignment:
                    teams = assignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList();
                    break;
            }
            return teams;
        }
    }
}
