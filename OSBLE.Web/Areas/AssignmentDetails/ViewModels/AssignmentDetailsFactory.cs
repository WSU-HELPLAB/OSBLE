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
using OSBLE.Models.AbstractCourses.Course;
using OSBLEPlus.Services.Controllers;
using OSBLE.Controllers;

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
            if (assignment.Type != AssignmentTypes.TeamEvaluation && assignment.Type != AssignmentTypes.AnchoredDiscussion)
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
                if (assignment.HasDeliverables && assignment.Type != AssignmentTypes.AnchoredDiscussion)
                {
                    //list deliverables add download link
                    vm.HeaderBuilder = new TeacherDeliverablesHeaderDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeacherDeliverablesHeaderDecorator");
                }

                //is a discussion assignment
                if (assignment.Type == AssignmentTypes.DiscussionAssignment && !assignment.HasDiscussionTeams)
                {
                    //link to classwide discussion
                    vm.HeaderBuilder = new TeacherDiscussionLinkDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("TeacherDiscussionLinkDecorator");
                }

                if (assignment.HasRubric)
                {
                    //add link to rubric ViewAsUneditable mode
                    vm.HeaderBuilder = new RubricDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("RubricDecorator");

                    //Show rubric grading progress for assignments with rubrics
                    vm.HeaderBuilder = new RubricGradingProgressDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("RubricGradingProgressDecorator");
                }

               

                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    //Show progress of TeamEvaluation, such as "X of Y Team Evaluations completed"
                    vm.HeaderBuilder = new TeamEvalProgressDecorator(vm.HeaderBuilder, teamEvaluations);
                    vm.HeaderViews.Add("TeamEvalProgressDecorator");
                }
                else if (assignment.Type == AssignmentTypes.CriticalReview)
                {
                    vm.HeaderBuilder = new PublishCriticalReviewDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("PublishCriticalReviewDecorator");
                }
                else if (assignment.Type == AssignmentTypes.AnchoredDiscussion)
                {
                    //commented out: for now we don't need anything here for the instructor
                    //in future implementations we may want to let the instructor also review documents with the group, but 
                    //how the teams are set up currently it wont work (no team set up for instructor)

                    //link for critical review submission document
                    //vm.HeaderBuilder = new AnchoredDiscussionSubmitDecorator(vm.HeaderBuilder, vm.Client);
                    //vm.HeaderViews.Add("AnchoredDiscussionSubmitDecorator");
                }


                // ABET outcomes - the ABETDepartment property being non-null indicates that 
                // this assignment was labeled for ABET outcomes and assessment.
                if (null != assignment.ABETDepartment)
                {
                    vm.HeaderBuilder = new ABETOutcomesDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("ABETOutcomesDecorator");
                }
            }
            else if (vm.Client.AbstractRoleID == (int)OSBLE.Models.Courses.CourseRole.CourseRoles.Observer)
            {
                //has discussion teams?
                if (assignment.HasDiscussionTeams)
                {
                    vm.HeaderBuilder = new DiscussionTeamMemberDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("DiscussionTeamMemberDecorator");
                }
                

                if (assignment.HasRubric)
                {
                    //add link to rubric ViewAsUneditable mode
                    vm.HeaderBuilder = new RubricDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("RubricDecorator");
                }
                if (assignment.HasDeliverables && assignment.Type != AssignmentTypes.AnchoredDiscussion)
                {
                    //list deliverables add download link
                    vm.HeaderBuilder = new TeacherDeliverablesHeaderDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeacherDeliverablesHeaderDecorator");
                }
                else if (assignment.Type == AssignmentTypes.DiscussionAssignment && !assignment.HasDiscussionTeams)
                {
                    //link to classwide discussion
                    vm.HeaderBuilder = new StudentDiscussionLinkDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("StudentDiscussionLinkDecorator");
                }
                else if (assignment.Type == AssignmentTypes.CriticalReview)
                {
                    vm.HeaderBuilder = new TeacherDeliverablesHeaderDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeacherDeliverablesHeaderDecorator");
                }
                
                

            }
            else if (vm.Client.AbstractRole.CanSubmit) //students
            {
                DateTime due = assignment.DueDate.AddHours(assignment.HoursLateWindow);
                due = due.UTCToCourse(vm.Client.AbstractCourseID);
                DateTime now = DateTime.UtcNow;
                now = now.UTCToCourse(vm.Client.AbstractCourseID);
                bool canSub = (now < due);

                //has discussion teams?
                if (assignment.HasDiscussionTeams)
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
                    if (assignment.Type == AssignmentTypes.AnchoredDiscussion)
                    {
                        //link for critical review submission document
                        vm.HeaderBuilder = new AnchoredDiscussionSubmissionDecorator(vm.HeaderBuilder, vm.Client);
                        vm.HeaderViews.Add("AnchoredDiscussionSubmissionDecorator");
                    }
                    else
                    {
                        //add student submission link
                        vm.HeaderBuilder = new StudentSubmissionDecorator(vm.HeaderBuilder, vm.Client);
                        vm.HeaderViews.Add("StudentSubmissionDecorator");
                    }
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
                else if (assignment.Type == AssignmentTypes.AnchoredDiscussion)
                {
                    //link for critical review submission document
                    vm.HeaderBuilder = new AnchoredDiscussionSubmissionDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("AnchoredDiscussionSubmissionDecorator");
                }
                else if (assignment.Type == AssignmentTypes.DiscussionAssignment && !assignment.HasDiscussionTeams)
                {
                    //link to classwide discussion
                    vm.HeaderBuilder = new StudentDiscussionLinkDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("StudentDiscussionLinkDecorator");
                }
                else if (assignment.Type == AssignmentTypes.TeamEvaluation && canSub)
                {
                    vm.HeaderBuilder = new StudentTeamEvalSubmissionDecorator(vm.HeaderBuilder, teamEvaluations, vm.Client);
                    vm.HeaderViews.Add("StudentTeamEvalSubmissionDecorator");

                }

                //rubric?
                if (assignment.HasRubric)
                {



                    RubricEvaluation rubricEvaluation = null;



                    //Getting the assignment team for Student, and if its non-null then we take that team ID and find the RubricEvaluation
                    //that they were the recipient of. 
                    AssignmentTeam at = OSBLEController.GetAssignmentTeam(assignment, vm.Client);
                    int teamId = 0;
                    if (at != null)
                    {
                        teamId = at.TeamID;

                        using (OSBLEContext db = new OSBLEContext())
                        {
                            //Only want to look at evaluations where Evaluator.AbstractRole.CanGrade is true, otherwise
                            //the rubric evaluation is a  student rubric (not interested in them here)
                            rubricEvaluation = (from re in db.RubricEvaluations
                                                where re.AssignmentID == assignment.ID &&
                                                re.Evaluator.AbstractRole.CanGrade &&
                                                re.RecipientID == teamId
                                                select re).FirstOrDefault();
                        }
                    }
                    //add rubric link
                    if (rubricEvaluation == null)
                    {
                        vm.HeaderBuilder = new RubricDecorator(vm.HeaderBuilder);
                        vm.HeaderViews.Add("RubricDecorator");
                    }
                    //add link to graded rubric link
                    else
                    {
                        vm.HeaderBuilder = new RubricGradeDecorator(vm.HeaderBuilder, vm.Client);
                        vm.HeaderViews.Add("RubricGradeDecorator");
                    }
                }
               



            }

            else if (vm.Client.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator) //Moderator decorators
            {
                //has discussion teams?
                if (assignment.HasDiscussionTeams)
                {
                    vm.HeaderBuilder = new DiscussionTeamMemberDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("DiscussionTeamMemberDecorator");
                }
                else if (assignment.Type == AssignmentTypes.DiscussionAssignment && !assignment.HasDiscussionTeams)
                {
                    //link to classwide discussion
                    vm.HeaderBuilder = new StudentDiscussionLinkDecorator(vm.HeaderBuilder, vm.Client);
                    vm.HeaderViews.Add("StudentDiscussionLinkDecorator");
                }
            }

            if (assignment.Type == AssignmentTypes.CriticalReview ||
                assignment.Type == AssignmentTypes.CriticalReviewDiscussion ||
                assignment.Type == AssignmentTypes.TeamEvaluation)
            {
                vm.HeaderBuilder = new PreviousAssignmentDecorator(vm.HeaderBuilder);
                vm.HeaderViews.Add("PreviousAssignmentDecorator");
            }

            if (assignment.Type == AssignmentTypes.CriticalReviewDiscussion && vm.Client.AbstractRole.CanGrade)
            {
                vm.HeaderBuilder = new DownloadDiscussionItemsDecorator(vm.HeaderBuilder);
                vm.HeaderViews.Add("DownloadDiscussionItemsDecorator");
            }

            return vm;
        }

        private AssignmentDetailsViewModel BuildTable(AssignmentDetailsViewModel vm)
        {
            //AC NOTE: Items will be displayed in the order in which they are added
            // to the ViewModel's TableColumnHeaders dictionary.  Organize accordingly.

            Assignment assignment = vm.CurrentAssignment;
            List<IAssignmentTeam> teams = GetTeams(assignment);
            List<TeamEvaluation> teamEvaluations = null;
            List<DiscussionPost> allUserPosts = null;
            List<ReviewTeam> criticalReviewsPerformedTeams = new List<ReviewTeam>();
            List<CriticalReviewsReceivedTeam> criticalReviewsReceivedTeams = new List<CriticalReviewsReceivedTeam>();
            using (OSBLEContext db = new OSBLEContext())
            {
                //only need get these when they are needed
                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    teamEvaluations = db.TeamEvaluations.Where(te => te.TeamEvaluationAssignmentID == assignment.ID).ToList();
                }

                if (assignment.Type == AssignmentTypes.DiscussionAssignment ||
                    assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
                    allUserPosts = (from a in db.DiscussionPosts
                                    where a.AssignmentID == assignment.ID
                                    select a).ToList();

                //run a common query outside of the loop for efficiency
                if (assignment.Type == AssignmentTypes.CriticalReview)
                {
                    criticalReviewsPerformedTeams = (from rt in db.ReviewTeams
                                                        .Include("ReviewingTeam")
                                                        .Include("AuthorTeam")
                                                        .Include("AuthorTeam.TeamMembers")
                                                     where rt.AssignmentID == assignment.ID
                                                     select rt).ToList();

                    criticalReviewsReceivedTeams = (from rt in db.ReviewTeams
                                                    join t in db.Teams on rt.ReviewTeamID equals t.ID
                                                    join tm in db.TeamMembers on rt.AuthorTeamID equals tm.TeamID
                                                    where rt.AssignmentID == assignment.ID
                                                    select new CriticalReviewsReceivedTeam() { CourseUser = tm.CourseUser, TeamName = t.Name, UserProfile = tm.CourseUser.UserProfile }).ToList();
                }
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
                    CriticalReviewsReceivedDecorator crit = new CriticalReviewsReceivedDecorator(vm.TeamTableBuilders[assignmentTeam]);
                    crit.ReviewTeams = criticalReviewsReceivedTeams;
                    vm.TeamTableBuilders[assignmentTeam] = crit;
                    vm.TableColumnHeaders["CriticalReviewsReceivedDecorator"] = "Reviews Received";

                    if (assignment.HasStudentRubric)
                    {
                        vm.TeamTableBuilders[assignmentTeam] = new StudentRubricsReceivedDecorator(vm.TeamTableBuilders[assignmentTeam]);
                        vm.TableColumnHeaders["StudentRubricsReceivedDecorator"] = "Rubrics Received";
                    }

                    CriticalReviewsPerformedDecorator performed = new CriticalReviewsPerformedDecorator(vm.TeamTableBuilders[assignmentTeam]);
                    performed.ReviewTeams = criticalReviewsPerformedTeams;
                    vm.TeamTableBuilders[assignmentTeam] = performed;
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

                    //add team evaluation multiplier 
                    vm.TeamTableBuilders[assignmentTeam] = new TeamEvaluationMultiplierTableDecorator(
                                                                vm.TeamTableBuilders[assignmentTeam],
                                                                evaluations
                                                                );
                    vm.TableColumnHeaders["TeamEvaluationMultiplierTableDecorator"] = "Multiplier";

                }
                else
                {
                    if (assignment.HasRubric)
                    {
                        //add rubric grade info
                        vm.TeamTableBuilders[assignmentTeam] = new RubricTableDecorator(
                                                                    vm.TeamTableBuilders[assignmentTeam]
                                                                    );
                        vm.TableColumnHeaders["RubricTableDecorator"] = "Rubric Grade";

                        vm.TableColumnHeaders["LateRubricGradeTableDecorator"] = "Late Rubric Grade";

                        
                    }

                    if (assignment.HasDeliverables || (assignment.Type == AssignmentTypes.DiscussionAssignment || (assignment.Type == AssignmentTypes.CriticalReviewDiscussion)))
                    {
                        //add late penalty info
                        vm.TeamTableBuilders[assignmentTeam] = new LatePenaltyTableDecorator(
                                                                    vm.TeamTableBuilders[assignmentTeam]
                                                                    );
                        vm.TableColumnHeaders["LatePenaltyTableDecorator"] = "Late Penalty";
                    }
                }

                // ABET stuff
                if (assignment.HasDeliverables && null != assignment.ABETDepartment)
                {
                    vm.TeamTableBuilders[assignmentTeam] = new ABETProficiencyDecorator(
                        vm.TeamTableBuilders[assignmentTeam]);
                    vm.TableColumnHeaders["ABETProficiencyDecorator"] = "ABET Proficiency";
                }
            }
            return vm;
        }

        private List<IAssignmentTeam> GetTeams(Assignment assignment)
        {
            List<IAssignmentTeam> teams = new List<IAssignmentTeam>();

            //TeamEvaluation assignments with Discussion Assignments as preceding assignments 
            //cannot use this simple switch to determine which teams to return. so adding a preceding if check
            if (assignment.Type == AssignmentTypes.TeamEvaluation && assignment.PreceedingAssignment.Type == AssignmentTypes.DiscussionAssignment)
            {
                teams = assignment.PreceedingAssignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList();
            }
            else
            {
                switch (assignment.Type)
                {
                    case AssignmentTypes.TeamEvaluation:
                        teams = assignment.PreceedingAssignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList();
                        break;

                    case AssignmentTypes.Basic:
                    case AssignmentTypes.CriticalReview:
                        teams = assignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList();
                        List<IAssignmentTeam> teamsTemp = new List<IAssignmentTeam>(teams);
                        foreach (var team in teams)
                        {
                            if (team.Team.TeamMembers.Count() == 0)
                            {
                                teamsTemp.Remove(team);
                            }
                        }
                        teams = teamsTemp;
                        break;

                    case AssignmentTypes.CriticalReviewDiscussion:
                    case AssignmentTypes.DiscussionAssignment:
                        teams = assignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList();
                        break;
                }
            }
            return teams;
        }
    }
}
