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
            Assignment assignment = vm.CurrentAssignment;
            vm.HeaderBuilder = new DefaultBuilder();

            //teacher views
            if (vm.Client.AbstractRole.CanGrade)
            {
                //add deliverable information if needed
                if (assignment.HasDeliverables)
                {
                    vm.HeaderBuilder = new TeacherDeliverablesHeaderDecorator(vm.HeaderBuilder);
                    vm.HeaderViews.Add("TeacherDeliverablesHeaderDecorator");
                }
            }
            return vm;
        }

        private AssignmentDetailsViewModel BuildTable(AssignmentDetailsViewModel vm)
        {
            Assignment assignment = vm.CurrentAssignment;
            List<IAssignmentTeam> teams = GetTeams(assignment);

            //create a builder for each team
            foreach (IAssignmentTeam assignmentTeam in teams)
            {
                Team team = assignmentTeam.Team;
                vm.TeamTableBuilders[team] = new DefaultBuilder();
                if (assignment.HasDeliverables)
                {
                    vm.TeamTableBuilders[team] = new DeliverablesTableDecorator(vm.TeamTableBuilders[team]);
                    vm.TableColumnHeaders["DeliverablesTableDecorator"] = "Submission";
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