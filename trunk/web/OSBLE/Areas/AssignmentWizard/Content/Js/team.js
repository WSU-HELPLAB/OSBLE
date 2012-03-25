$(document).ready(documentReady);

//Called when the document has finished loading and is safe to make DOM calls
function documentReady() {

    //set up sortable lists
    $(".TeamSortable").sortable(
            {
                connectWith: ".TeamSortable",
                forcePlaceholderSize: true
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

//Used to give each new team a unique name (see createTeam function)
var teamCounter = 1;

//Creates a new team div and places it on the 
function createTeam(evt, teamName) {
    var divId = "teamDiv_" + teamCounter + "_0";
    var listId = 'team_' + teamCounter + "_0";
    if (teamName == undefined) {
        teamName = "Team ";

        //Add leading zero as requested in CodePlex ticket #695
        if (teamCounter < 10) {
            teamName += "0" + teamCounter;
        }
        else {
            teamName += teamCounter;
        }
    }
    var newContent = '<div style="display:none;" id="' + divId + '" class="TeamDiv">' +
                            '<input type="text" class="TeamNameTextBox" value="' + teamName + '" />' +
                            '<img class="RemoveTeamIcon" src="/Content/images/delete_up.png" alt="remove team" title="remove team" onclick="removeTeam(\'' + divId + '\')" />' +
                            '<ul id="' + listId + '" class="TeamSortable"></ul>' +
                         '</div>';
    if (teamCounter % 3 == 0) {
        newContent += '<div style="clear:both;"></div>';
    }
    $("#TeamsDiv").append(newContent);
    $("#" + listId).sortable({ connectWith: ".TeamSortable" }).disableSelection();
    $("#" + divId).fadeIn('slow');
    teamCounter++;
    return divId;
}

//Resets the page to its null state:
//      clears all teams that have been created
//      returns all students to the available students pool
//      resets the teamCounter variable back to 1
//      removes all HTML in TeamsDiv
function clearAllTeams() {

    //find all teams and remove them
    var allTeams = $("#TeamsDiv").find('div');
    var numberOfTeams = allTeams.length;
    allTeams.each(function (index) {
        var id = $(this).context.id;
        if (id != undefined && id != "") {
            removeTeam(id);
        }
        else {
            $(this).remove();
        }
    });

    //reset the team counter
    teamCounter = 1;

}

//Creates the supplied number of teams and randomly assigns students
//to these teams
function buildTeams(numberOfTeams) {

    //clear out any pre-existing layout
    clearAllTeams();

    //wait for all teams to be removed before continuing
    $(".TeamDiv").promise().done(function () {

        for (var i = 0; i < numberOfTeams; i++) {
            createTeam(undefined, undefined);
        }

        //again, we need to wait for team creation to finish before we continue
        $(".TeamDiv").promise().done(function () {

            //build a team and student array
            var unassignedStudents = Array();
            var teamCounter = Array();
            var allStudents = $.find('.Student');
            var numStudents = allStudents.length;
            var allTeams = $.find('.TeamDiv');

            //prime the arrays
            for (var i = 0; i < numberOfTeams; i++) {
                teamCounter[i] = { id: allTeams[i].id, count: 0 };
            }
            for (var i = 0; i < numStudents; i++) {
                unassignedStudents[i] = allStudents[i].id;
            }

            //assign students to teams
            while (unassignedStudents.length > 0) {

                //pick a random student
                var studentIndex = Math.floor(Math.random() * unassignedStudents.length);
                var student = unassignedStudents[studentIndex];

                //pick the smallest team
                var smallestTeamId = 0;
                var smallestTeamSize = numStudents + 1;
                var smallestTeamIndex = 0;
                for (var i = 0; i < teamCounter.length; i++) {
                    if (teamCounter[i].count <= smallestTeamSize) {
                        smallestTeamSize = teamCounter[i].count;
                        smallestTeamId = teamCounter[i].id;
                        smallestTeamIndex = i;
                    }
                }

                //increase the size of the selected team by 1
                teamCounter[smallestTeamIndex].count++;

                //assign the student to the team
                addStudentToTeam(student, smallestTeamId);

                //remove students from the list of possible students
                unassignedStudents = unassignedStudents.slice(0, studentIndex).concat(unassignedStudents.slice(studentIndex + 1, unassignedStudents.length));
            }
        });

    });
}

//Auto generates a team configuration doing its best to create teams using
//the size stored inside the "AutoGenByStudentTextBox"    
function generateTeamsByNumberOfStudents() {
    var studentsPerTeam = $('#AutoGenByStudentTextBox').val();
    if (studentsPerTeam == undefined || studentsPerTeam == 0) {
        alert("The number of students per team needs to be greater than 0");
        return;
    }

    //create the appropriate number of teams needed
    var numStudents = $.find('.Student').length;
    var numTeams = Math.floor(numStudents / studentsPerTeam);
    buildTeams(numTeams);
}

//Auto generates a team configuration based on the number of teams entered inside the
//AutoGenByteamTextBox text box
function generateTeamsByNumberOfTeams() {
    var totalTeams = $("#AutoGenByteamTextBox").val();
    if (totalTeams == undefined || totalTeams == 0) {
        alert("The number of teams to be generated must be greater than 0");
        return;
    }
    buildTeams(totalTeams);
}

//Adds a student (li element's ID, ex: "cu_1") 
//to the supplied team list (ul element's ID, ex "team_1_0")
function addStudentToTeam(studentId, teamId) {
    $("#" + studentId).slideUp('slow', function () {
        $('#' + teamId).find('.TeamSortable').append($("#" + studentId));
        $("#" + studentId).slideDown('slow');
    });
}

//Called when the user clicks the remove button on a particular team div
function removeTeam(teamId) {
    $('#' + teamId).fadeOut('slow', hideTeamComplete);
}

//Called after removeTeam().  Places any students that were on the removed team
//back into the pool of available students.
function hideTeamComplete() {
    jQuery(this).find("li").css("display", "none");
    $("#AvailableStudent").append(jQuery(this).find("li"));
    $("#AvailableStudent").find(':hidden').slideDown('slow');
    $(this).remove();
}