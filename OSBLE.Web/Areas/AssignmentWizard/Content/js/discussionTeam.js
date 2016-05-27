$(document).ready(documentReady);

//Called when the document has finished loading and is safe to make DOM calls
function documentReady() {

    //radio button event
    $('input[name=teamRadio]').change(function () {
        if ($('#radioNo').attr('checked')) {
            $('#teamDiv').show();
        }
        else {
            $('#teamDiv').hide();
        }
    });


    //Setting up Moderators (the list items of Moderators/TAs) as draggable. Note: This property is not carried to the cloned moderators (intentional)
    //$(".Moderator").draggable(
    $("#AvailableModerators li").draggable(
    {
        connectToSortable: ".TeamSortable",
        forcePlaceholderSize: true,
        helper: "clone",
        start: hideErrors,
    }).disableSelection();

    //Setting up TeamSortables (teams) sorting between each other. Note: Other TeamSortables are created below in createTeam(). Modifications here should also
    //be upkept in that function
    $(".TeamSortable").sortable(
    {
        connectWith: ".TeamSortable",
        forcePlaceholderSize: true,
        receive: teamSortableComplete,
        start: hideErrors
    }).disableSelection();

    //Setting up AvilableStudent (unassigned students) sorting into TeamSortable (teams)
    $("#AvailableStudent").sortable(
    {
        connectWith: ".TeamSortable",
        forcePlaceholderSize: true,
        start: hideErrors
    }).disableSelection();
    
    
    //various event listeners
    $("#WizardForm").submit(processForm);
    $("#CreateTeamLink").click(createTeam);

    //turn off selection as it ruins the UI experience
    $("#AvailableStudentList").disableSelection();
    $("#TeamsDiv").disableSelection();
}

function teamSortableComplete(event, ui) {

    var OrigLIElement = ui.item.context;
    var myDataId = $(OrigLIElement).attr('data-id');
    var list = $(OrigLIElement).parent().contents();
    var ULElement = $(this);

    if(typeof myDataId != 'undefined')
    {
        var counter = 0;
        $.each($(this).find('[data-id=\"' + myDataId + '\"]'), function () {
            counter++;
            if(counter > 1)
            {
                var parentDiv = $(ULElement).parent().find('.TeamNameTextBox');
                var teamName = $(parentDiv).attr("value");
                var duplicateMemberName = $(OrigLIElement).attr('text');
                alreadyOnTeamError($(this).text(), teamName);
                $(this).remove();
                
                
            }
        });
    }

    var parentid = $($(OrigLIElement).parent().parent()).attr("id");
    if (!document.getElementById('allow_cross_section').checked && parentid != "AvailableStudentList") //if no cross teams are allowed, check for adding a team member to a different section
    {
        if (list.length > 1) //make sure there is one other student in the team, or it doesn't matter
        {
            var firstTeamMember = list[0];
            if (firstTeamMember === $(OrigLIElement).context) {
                firstTeamMember = list[1];
            }

            var FirstTeamMembAttr = $(firstTeamMember).attr("section");
            var ElementSection = $(OrigLIElement).attr("section");

            if (!(FirstTeamMembAttr === ElementSection)) {
                alert("You must checkmark 'Allow Cross_Section teams' to have teams with members across sections.");
                var availables = document.getElementById("AvailableStudent");
                availables.appendChild($(OrigLIElement).context);
                //dragElement.remove();
                //re add drag element to where it was.....
            }
        }
    }

}

function hideErrors() {
    $('#ErrorBox').promise().done(function () {
        $('#ErrorBox').animate({ opacity: 0.0 }, 60, "easeOutExpo");
    });
}

//changes the error box text
function displayError(text) {
    $('#ErrorBox').promise().done(function () {
        $('#ErrorBox').text(text);
        $('#ErrorBox').animate({ opacity: 1.0 }, 80, "easeOutExpo");
    });
}

function alreadyOnTeamError(reviewer, reviewItem) {
    var text = reviewer + " is already reviewing " + reviewItem + ".";
    displayError(text);
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

        //find all moderators on this team
        $(teams[i]).find(".Moderator").each(function (index) {
            //find the moderators course user id
            var myLi = $(this).context;
            var rawId = $(myLi).attr("data-id");
            var courseUserId = rawId.split("_")[1];

            //append the team to form value (this is different than how students are handled, as moderators can be on multiple teams)
            var oldVal = $("#moderator_" + courseUserId).val();
            var newVal = oldVal + teamName + ",";
            $("#moderator_" + courseUserId).val(newVal);
        });
    }
}

//Used to give each new team a unique name (see createTeam function)
var teamCounter = 1;

//Creates a new team div and places it on the 
function createTeam(evt, teamName) {
    teamCounter = document.getElementsByClassName("TeamDiv").length + 1;
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
    $("#" + listId).sortable({ connectWith: ".TeamSortable", forcePlaceholderSize: true,
        receive: teamSortableComplete, start: hideErrors
    }).disableSelection();
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
function buildTeamsCross(numberOfTeams) {

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

//Builds teams based on section
function buildTeams(numTeamsArray, studentArrays) { //TODO:: MAYBE HAVE TO CHANGE THE TWO GENERATE BY # OF TEAMS AND GENERATE BY # OF STUDENTS
    //clear out any pre-existing layout
    clearAllTeams();

    //wait for all teams to be removed before continuing
    $(".TeamDiv").promise().done(function () {
        //var allSections = Array();
        //var unassignedStudents = Array();
        var allStudents = $.find('.Student');
        var numStudents = allStudents.length;

        for (var x = 0; x < numTeamsArray.length; x++) {
            for (var i = 0; i < numTeamsArray[x]; i++) {
                createTeam(undefined, undefined);
            }
        }

        //again, we need to wait for team creation to finish before we continue
        $(".TeamDiv").promise().done(function () {
            //build a team and student array            
            var teamCounter = Array();
            var allTeams = $.find('.TeamDiv');

            var teamArrayIndex = 0;

            for (var x = 0; x < numTeamsArray.length; x++) {
                //prime the arrays
                for (var i = 0; i < numTeamsArray[x]; i++) {
                    teamCounter[i + teamArrayIndex] = { id: allTeams[i + teamArrayIndex].id, count: 0 };
                }

                //assign students to teams
                while (studentArrays[x].length > 0) {

                    //pick a random student
                    var studentIndex = Math.floor(Math.random() * studentArrays[x].length);
                    var student = studentArrays[x][studentIndex];

                    //pick the smallest team
                    var smallestTeamId;
                    var smallestTeamSize;
                    var smallestTeamIndex;

                    smallestTeamId = 0;
                    smallestTeamSize = numStudents + 1;
                    var smallestTeamIndex = 0;

                    //This loop needs to start from teamArrayIndex in order to skip all filled teams
                    for (var i = teamArrayIndex; i < teamCounter.length; i++) {
                        if (teamCounter[i].count <= smallestTeamSize) {
                            smallestTeamSize = teamCounter[i].count;
                            smallestTeamId = teamCounter[i].id;
                            smallestTeamIndex = i;
                        }
                    }

                    //increase the size of the selected team by 1
                    teamCounter[smallestTeamIndex].count++;


                    //assign the student to the team
                    addStudentToTeam(student.id, smallestTeamId);

                    //remove students from the list of possible students
                    studentArrays[x] = studentArrays[x].slice(0, studentIndex).concat(studentArrays[x].slice(studentIndex + 1, studentArrays[x].length));
                }

                //teamArrayIndex will be a placeholder to skip all used up teams
                teamArrayIndex += (teamCounter.length - teamArrayIndex);
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
    var allStudents = $.find('.Student');
    var numStudents = allStudents.length;
    var studentArrays = Array() //array of student arrays grouped by section
    var numTeamsArray = Array() //array of each sections team ammount
    var allSections = Array()


    //grab the checkmark box, see if checked.
    if (document.getElementById("allow_cross_section").checked) //cross section teams allowed
    {
        var numTeams = Math.floor(numStudents / studentsPerTeam);
        buildTeamsCross(numTeams);
    }

    else //cross section teams not allowed
    {
        for (var i = 0; i < numStudents; i++) //This populates the allSections
        {
            var temp = String(allStudents[i].getAttribute('SECTION'));

            if ($.inArray(temp, allSections) === -1) { //check if this section is already in the sections array
                allSections.push(allStudents[i].getAttribute('SECTION'));
            }
        }

        allSections.sort(); //sort the sections so they're in order.

        for (var x = 0; x < allSections.length; x++) {
            studentArrays.push(Array())
            for (var i = 0; i < numStudents; i++) {
                if (allStudents[i].getAttribute('SECTION') === allSections[x]) //if this student belongs to this section, add them to this section
                {
                    studentArrays[x].push(allStudents[i]);
                }
            }
        }

        for (var x = 0; x < allSections.length; x++) //populate the ammount of teams per section
        {
            numTeamsArray.push(Math.floor(studentArrays[x].length / studentsPerTeam));
        }


        buildTeams(numTeamsArray, studentArrays);
    }
}

//Auto generates a team configuration based on the number of teams entered inside the
//AutoGenByteamTextBox text box
function generateTeamsByNumberOfTeams() {
    var totalTeams = $("#AutoGenByteamTextBox").val();
    if (totalTeams == undefined || totalTeams == 0) {
        alert("The number of teams to be generated must be greater than 0");
        return;
    }

    var allStudents = $.find('.Student');
    var numStudents = allStudents.length;
    var studentArrays = Array() //array of student arrays grouped by section
    var numTeamsArray = Array() //array of each sections team ammount
    var allSections = Array()

    if (document.getElementById('allow_cross_section').checked) //if cross section teams are allowed
    {
        buildTeamsCross(totalTeams);
    }

    else //if cross section teams are not allowed
    {

        for (var i = 0; i < numStudents; i++) //This populates the allSections
        {
            var temp = String(allStudents[i].getAttribute('SECTION'));

            if ($.inArray(temp, allSections) === -1) { //check if this section is already in the sections array
                allSections.push(allStudents[i].getAttribute('SECTION'));
            }
        }

        allSections.sort(); //sort the sections so they're in order.

        for (var x = 0; x < allSections.length; x++) //populate the studentsArray. Add students to studentArrays index if section matches
        {
            studentArrays.push(Array())
            for (var i = 0; i < numStudents; i++) {
                if (allStudents[i].getAttribute('SECTION') === allSections[x]) //if this student belongs to this section, add them to this section
                {
                    studentArrays[x].push(allStudents[i]);
                }
            }
        }

        for (var x = 0; x < allSections.length; x++) //fill every slot of team size, with the specified team size.
        {
            numTeamsArray.push(totalTeams);
        }

        buildTeams(numTeamsArray, studentArrays);
    }
}

//removes the selected item from the review team
function removeFromTeam(element) {
    var liElement = $(element).parent();
    if($(liElement).hasClass('Student'))
    {
        $(liElement).slideUp('slow', removeStudentFromTeamComplete);
        
    }
    else
    {
        $(liElement).slideUp('slow', removeModeratorFromTeamComplete);
    }
}

function removeStudentFromTeamComplete()
{
    $("#AvailableStudent").append($(this));
    $(this).slideDown('slow');
}
function removeModeratorFromTeamComplete() {
    $(this).remove();
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

//Called after removeTeam().  Removes all TA/moderators from the team
//and places any students that were on the removed team
//back into the pool of available students. 
function hideTeamComplete() {
    
    jQuery(this).find(".Moderator").remove();
    jQuery(this).find("li").css("display", "none");
    $("#AvailableStudent").append(jQuery(this).find("li"));
    $("#AvailableStudent").find(':hidden').not('img').slideDown('slow');
    $(this).remove();
}