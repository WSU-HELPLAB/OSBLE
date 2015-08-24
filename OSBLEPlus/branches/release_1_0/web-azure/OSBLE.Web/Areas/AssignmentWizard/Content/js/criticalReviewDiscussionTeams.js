$(document).ready(documentReady);

function documentReady() {

    //Setting up moderators to initially be draggable to TeamSortables. Once there they should only
    //be able to be moved around the same team, not interchanged amoungst other teams.
    $("#AvailableModerators li").draggable(
        {
            connectToSortable: ".TeamSortable",
            forcePlaceholderSize: true,
            helper: "clone",
            start: hideErrors
        }).disableSelection();

    //Setting up TeamSortables so that they can be sorted within themselves, 
    //but cannot be dragged to other lists.
    $(".TeamSortable").sortable(
        {
            forcePlaceholderSize: true,
            receive: teamSortableComplete,
            start: hideErrors
        }).disableSelection();

    //setting processForm event before going to post
    $("#WizardForm").submit(processForm);
}

function teamSortableComplete(event, ui) {
    var OrigLIElement = ui.item.context;
    var myDataId = $(OrigLIElement).attr('data-id');
    var ULElement = $(this);

    if (typeof myDataId != 'undefined') {
        var counter = 0;
        $.each($(this).find('[data-id=\"' + myDataId + '\"]'), function () {
            counter++;
            if (counter > 1) {
                var parentDiv = $(ULElement).parent().find('.TeamNameTextBox');
                var teamName = $(parentDiv).attr("value");
                var duplicateMemberName = $(OrigLIElement).attr('text');
                alreadyOnTeamError($(this).text(), teamName);
                $(this).remove();
            }
        });
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

//removes the selected item from the review team
function removeFromTeam(element) {
    var liElement = $(element).parent();
    $(liElement).slideUp('slow', removeModeratorFromTeamComplete);

}

function removeModeratorFromTeamComplete() {
    $(this).remove();
}

//Before we can postback, we need to set up moderators
function processForm(evt) {

    //Get all teams ULs
    var discussionTeams = $.find('.TeamSortable');
    for (var i = 0; i < discussionTeams.length; i++) {


        //find the dt's DB ID
        var discussionTeamId = parseInt($(discussionTeams[i]).context.id.split('_')[1]);

        //find all moderators on this team
        $(discussionTeams[i]).find(".Moderator").each(function (index) {
            //find the moderators course user id
            var myLi = $(this).context;
            var rawId = $(myLi).attr("data-id");
            var courseUserId = rawId.split("_")[1];

            //append the discussionTeam id to form value. This will be grabbed in the post and handled appropriately.
            var oldVal = $("#moderator_" + courseUserId).val();
            var newVal = oldVal + discussionTeamId + ",";
            $("#moderator_" + courseUserId).val(newVal);
        });
    }
}