$(document).ready(documentReady);

//Called when the document has finished loading and is safe to make DOM calls
function documentReady() {

    //set up sortable lists
    $(".TeamSortable").sortable(
            {
                connectWith: ".StudentListItem",
                forcePlaceholderSize: true
            }).disableSelection();
    $(".StudentListItem").draggable(
            {
                connectToSortable: ".TeamSortable",
                forcePlaceholderSize: true,
                helper: "clone"
            }).disableSelection();

    //various event listeners
    $("#WizardForm").submit(processForm);
    $("#CreateTeamLink").click(createTeam);

    //turn off selection as it ruins the UI experience
    $("#AvailableStudentList").disableSelection();
    $("#TeamsDiv").disableSelection();
}

//Before we can postback, we need to inject students into teams for the controller to process
function processForm(evt) {

    //get all teams
    var teams = $.find('.TeamDiv');
    for (var i = 0; i < teams.length; i++) {

        //find the team's name
        var teamName = $(teams[i]).find('.TeamNameTextBox').val();

        //find the team's DB ID
        var teamId = parseInt($(teams[i]).context.id.split('_')[2]);

        //set the team name if the DB ID isn't 0
        if (teamId > 0) {
            $('#team_' + teamId).val(teamName);
        }

        //find all students on this team
        $(teams[i]).find(".Student").each(function (index) {

            //find the student's id
            var rawId = $(this).context.id;
            var studentId = rawId.split("_")[1];

            //set the form value
            $("#student_" + studentId).val(teamName);
        });
    }
}

function buildTeams(reviewItemsPerTeam) {
}

//removes the selected item from the review team
function removeFromTeam(element) {
    $(element).parent().fadeOut('slow', removeFromTeamComplete);
}

function removeFromTeamComplete() {
    $(this).remove();
}