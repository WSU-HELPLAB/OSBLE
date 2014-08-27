$(document).ready(documentReady);

//Called when the document has finished loading and is safe to make DOM calls
function documentReady() {

    //set up sortable lists
    $(".TeamSortable").sortable(
            {
                connectWith: ".TeamSortable",
                forcePlaceholderSize: true,
                update: dragComplete
            }).disableSelection();

    //$(".StudentListItem").draggable(
    $("#AvailableStudent li").draggable(
    {
        connectToSortable: ".TeamSortable",
        forcePlaceholderSize: true,
        helper: "clone",
        start: hideErrors
    }).disableSelection();


    //various event listeners
    $("#WizardForm").submit(processForm);

    //turn off selection as it ruins the UI experience
    $("#AvailableStudentList").disableSelection();
    $("#TeamsDiv").disableSelection();
}

//Adds the reviewer to the specified team
function addReviewer(reviewerId, teamId) {
    
    var reviewer = $("#AvailableStudent").children('li').filter(function (index) {
        var dataId = $(this).attr("data-id");
        return reviewerId == dataId;
    }).first();
    var itemClone = reviewer.clone().hide();
    $('#' + teamId).append(itemClone);
    itemClone.slideDown("slow");
}

//formats a message to be displayed in the error box
function alreadyOnTeamError(reviewer, reviewItem) {
    var text = reviewer + " is already reviewing " + reviewItem + ".";
    displayError(text);
}

function buildTeams() {

    //find the total number of review items per entity
    var itemsToReview = $("#ReviewItemsText").val();

    //holds the list of reviewers and review teams
    var reviewTeams = Array();
    var activeReviewers = Array();

    var reviewers = $('#AvailableStudent').children('li');
    var reviewItems = $('.selectorCheckBox:checked');

    var numReviewTeams = reviewItems.length;
    var numReviewers = reviewers.length;

    //prime arrays
    for (var i = 0; i < numReviewTeams; i++) {

        //we need the ID of the div that contains the ID in the format of "team_ID".  This belongs to the 
        //UL with the class 'TeamSortable'.
        reviewTeams[i] = {
            id: $(reviewItems[i]).parent().children('.TeamSortable')[0].id,
            count: 0,
            members: Array(),
            author: $(reviewItems[i]).parent().children('.TeamSortable').first().prev().text()
        };
    }
    for (var i = 0; i < numReviewers; i++) {
        activeReviewers[i] = {
            id: $(reviewers[i]).attr("data-id"),
            count: 0,
            name: $(reviewers[i]).text().trim()
        };
    }

    //hide any visible errors
    hideErrors();

    //don't continue if we didn't get a valid number
    itemsToReview = parseInt(itemsToReview);
    if (itemsToReview == undefined || itemsToReview == 0 || isNaN(itemsToReview)) {
        displayError("The number of review items must be greater than zero.");
        return;
    }

    //don't allow more review instances than reviewers (will cause an infinte loop)
    if (itemsToReview > numReviewers) {
        displayError("The number of review items cannot exceed the number of reviewers.");
        return;
    }

    if (itemsToReview > numReviewTeams) {
        displayError("The number of reviewers cannot exceed the number of review items.");
        return;
    }

    //clear out any existing configuration
    clearReviewTeams();

    //wait for configuration to be removed before continuing
    $(".StudentListItem").promise().done(function () {
        
        //assign reviewers to teams
        while (activeReviewers.length > 0) {

            //pick a random reviewer
            var reviewerIndex = Math.floor(Math.random() * activeReviewers.length);
            var reviewer = activeReviewers[reviewerIndex];

            //pick the smallest team
            var smallestTeamId = 0;
            var smallestTeamSize = numReviewers + 1;
            var smallestTeamIndex = 0;
            for (var i = 0; i < reviewTeams.length; i++) {

                //make sure that the reviewer isn't already in the team
                if ($.inArray(reviewer.id, reviewTeams[i].members) == -1) {

                    //make sure that the reviewer isn't the author
                    if (reviewTeams[i].author != reviewer.name) {
                        if (reviewTeams[i].count <= smallestTeamSize) {
                            smallestTeamSize = reviewTeams[i].count;
                            smallestTeamId = reviewTeams[i].id;
                            smallestTeamIndex = i;
                        }
                    }
                }
            }

            //increase the size of the selected team by 1
            reviewTeams[smallestTeamIndex].count++;

            //assign the student to the team
            addReviewer(reviewer.id, smallestTeamId);
            reviewTeams[smallestTeamIndex].members.push(reviewer.id);

            //increase reviewer team count
            reviewer.count++;

            //if the reviewer has reached his maximum number of review items, remove him from the list of
            //active reviewers
            if (reviewer.count >= itemsToReview) {
                activeReviewers = activeReviewers.slice(0, reviewerIndex).concat(activeReviewers.slice(reviewerIndex + 1, activeReviewers.length));
            }
        }
    });
}

//Removes all members of every review team
function clearReviewTeams() {
    
    //find all teams
    var allTeams = $("#TeamsDiv").find('div');

    //loop through each team, and clear out the list of reviewers
    allTeams.each(function (index) {
        $(this).find("ul").children("li").each(function (index) {
            removeFromTeam($(this).children().first());
        });
    });
}

//changes the error box text
function displayError(text) {
    $('#ErrorBox').promise().done(function () {
        $('#ErrorBox').text(text);
        $('#ErrorBox').animate({ opacity: 1.0 }, 800, "easeOutExpo");
    });
}

//Called whenever a draggable (student / team) is dropped onto a review entity
function dragComplete(event, ui) {
    
    //the element that just got finished being dragged
    var dragElement = $(ui.item.context);

    //the data ID of the element (contains key information that we will need to send back to server)
    var elementDataId = dragElement.attr("data-id");

    //the review list that the element is trying to be added to
    var list = dragElement.parent().contents();

    //the review entity's name
    var reviewerName = dragElement.text().trim();

    //the item being reviewed
    var reviewItem = dragElement
                        .parent()       //parent should be UL
                        .parent()       //UL's parent is a DIV
                        .contents()     //All children (including text) of the DIV
                        .filter(function () { return this.nodeType == 3; }) //Return only TEXT nodes
                        .text().trim(); //Get the text and trim whitespace

    if (hasDataId(list, elementDataId)) {
        alreadyOnTeamError(reviewerName, reviewItem);
        dragElement.remove();
    }
}

//Determines if the provided list already contains the supplied element.
//Used to help restrict a person from reviewing the same document multiple times.
function hasDataId(list, dataId) {
    var count = 0;
    $(list).each(function () {
        if ($(this).attr("data-id") == dataId) {
            count++;
        }
    }
    );
    if (count > 1) {
        return true;
    }
    else {
        return false;
    }
}

function hideErrors() {
    $('#ErrorBox').promise().done(function () {
        $('#ErrorBox').animate({ opacity: 0.0 }, 600, "easeOutExpo");
    });
}

//Before we can postback, we need to inject students into teams for the controller to process
function processForm(evt) {
    
    //get all review items
    var reviewItems = $.find('.TeamDiv');
    for (var i = 0; i < reviewItems.length; i++) {

        //find the team's DB ID
        var itemId = parseInt($(reviewItems[i]).context.id.split('_')[1]);
        //find the assignment course
        var courseId = parseInt($(reviewItems[i]).context.id.split('_')[2]);

        //find all reviewers for this item
        $(reviewItems[i]).find(".StudentListItem").each(function (index) {

            //find the reviewer's id
            var rawId = $(this).attr("data-id");
            var reviewerId = rawId.split("_")[1];

            //set the form value
            var oldVal = $("#reviewTeam_" + reviewerId).val();
            var newVal = "" + oldVal + "_" + itemId; //+ "_" + courseId;
            $("#reviewTeam_" + reviewerId).val(newVal);
        });
    }
}

//removes the selected item from the review team
function removeFromTeam(element) {
    $(element).parent().fadeOut('slow', removeFromTeamComplete);
}

function removeFromTeamComplete() {
    $(this).remove();
}