using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentDetails.ViewModels;
using OSBLE.Areas.AssignmentDetails.Models;
using OSBLE.Models.Courses;
using OSBLE.Areas.AssignmentDetails.Models.TableBuilder;

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
                    //add initial / final post due date information

                    if (!assignment.HasDiscussionTeams)
                    {
                        //link to classwide discussion
                    }
                }
                else
                {
                    //add normal due date
                }

                if (assignment.HasRubric)
                {
                    //add link to rubric ViewAsUneditable mode

                    //add "x of y" have been published

                    //add "z saved as draft (publish all) info
                }

                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    //add publish all multipliers button
                }
            }
            else if (vm.Client.AbstractRole.CanSubmit) //students
            {
                //has teams?
                if (assignment.HasTeams)
                {
                    //get the right kind of teams
                    List<IAssignmentTeam> teams = GetTeams(assignment);

                    //display profile picture
                }

                //needs to submit?
                if (assignment.HasDeliverables)
                {
                    //add student submission link
                }

                //rubric?
                if (assignment.HasRubric)
                {
                    //add rubric link
                }
                else
                {
                    //add grade link
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

            //create a builder for each team
            foreach (IAssignmentTeam assignmentTeam in teams)
            {
                Team team = assignmentTeam.Team;
                vm.TeamTableBuilders[team] = new DefaultBuilder();

                if (assignment.HasDeliverables)
                {
                    //display submission information
                    vm.TeamTableBuilders[team] = new DeliverablesTableDecorator(vm.TeamTableBuilders[team]);
                    vm.TableColumnHeaders["DeliverablesTableDecorator"] = "Submission";
                }

                if (assignment.Type == AssignmentTypes.DiscussionAssignment)
                {
                    //add posts / replies / all info
                }

                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    //add team evaluation info

                    //add largest discrepency info
                }
                else
                {
                    //add grade info

                    //add late penalty info
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