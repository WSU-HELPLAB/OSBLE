$(document).ready(function () {

    //need to change the text because this page is not a suggestion, it's a link on the dashboard.
    if ($("#iid").val() < 1) {
        $(".feedback-prompt").text("Was this page helpful?");
    }

    $(".clear-default-text").on("click", function () {
        $("textarea[name='ask-a-question']").val('');
        LogClick("Clear-Template-Text");
    });

    $("#availability-dropdown").on("change", function () {
        var selected = $('#availability-dropdown').val();
        if (selected == 0) {
            //show custom input box
            $("#custom-availability").removeClass("hidden");
        }
        else {
            $("#custom-availability").addClass("hidden");
        }
    });

    $('#custom-availability-input').keyup(function () {
        if (this.value != this.value.replace(/[^0-9]/g, '')) {
            this.value = this.value.replace(/[^0-9]/g, '');
        }
    });

    $(".hover-link-edit").on("click", function () {
        $("#code-display-box").fadeOut(300);
        $("#code-edit-box").fadeIn(1000);
        $(".hover-link-edit").fadeOut(300);
        $(".hover-link-save").fadeIn(300);
    });

    $("#reverse-sort-a, #reverse-sort-d").on("click", function () {
        var id = $(this).prop('id');
        if (id == "reverse-sort-a") {
            $("#reverse-sort-a").hide();
            $("#reverse-sort-d").show();
        }
        else {
            $("#reverse-sort-d").hide();
            $("#reverse-sort-a").show();
        }

        vm.ReverseFeedSortOrder();
    });

    $("#profile-url, #help-url").on("click", function () {        
        LogClick($(this).prop('id') + " Clicked");
    });

    $(".hover-link-save").on("click", function () {

        var interventionId = $("#iid").val();
        var suggestedCode = $("#code-edit-box").children()[0].value;
        //save suggested code to the db
        $.ajax({
            url: "/Intervention/UpdateSuggestedCode",
            data: { interventionId: interventionId, suggestedCode: suggestedCode },
            method: "POST",
            success: function (result) {
                //dismiss if db change successful                        
                if (result == "True") {

                    $("#code-display-box").empty();

                    //move code over to the code box
                    $("#code-display-box").append("<pre><code>" + suggestedCode + "</code></pre>");
                    //process changes - highlight code syntax
                    $('pre code').each(function (i, block) {
                        hljs.highlightBlock(block);
                    });

                    $("#code-display-box").fadeIn(1000);
                    $("#code-edit-box").fadeOut(300);
                    $(".hover-link-edit").fadeIn(1000);
                    $(".hover-link-save").fadeOut(300);
                }
            },
            error: function (result) {
            }
        });
    });

    $("#update-status").on("click", function () {
        UpdateStatus();
    });

    $(".checkbox").on("click", function () {
        //checkbox        
        if ($(this).get(0).nodeName == "INPUT") {
            if ($(this).is(':checked')) {
                $(this).prop("checked", false);
            }
            else {
                $(this).prop("checked", true);
            }
        }
        else {
            //div
            if ($(this).find("input[name='user-selected']").val() == -1) {
                if ($(this).find("input[name='user-selected']").is(':checked')) {
                    $("input[name='user-selected']").each(function () {
                        $(this).prop("checked", false);
                    });
                }
                else {
                    $("input[name='user-selected']").each(function () {
                        $(this).prop("checked", true);
                    });
                }
            }
            else {
                if ($(this).find("input[name='user-selected']").is(':checked')) {
                    $(this).find("input[name='user-selected']").prop("checked", false);
                }
                else {
                    $(this).find("input[name='user-selected']").prop("checked", true);
                }
            }
        }
    });

    $("#post-selected").on("click", function () {

        if ($('#post-selected').hasClass('disabled')) {
            //do nothing
        }
        else {
            //clear feedback
            $("#post-success").text("");
            $("#post-error").text("");

            var postContent = $("textarea[name='ask-a-question']").val();

            var selectedSelf = false;
            var selectedUserProfileIds = [];
            $('.available-users-details input:checked').each(function () {
                if ($(this).val() != -1) {
                    selectedUserProfileIds.push($(this).val());
                    if ($(this).val() == $("#cupid").val()) {
                        selectedSelf = true;
                    }
                }
            });

            if (!selectedSelf) { //if they have, for whatever reason, deselected themselves we need to make sure they're on the visibility list.
                selectedUserProfileIds.push($("#cupid").val());
            }

            if (postContent == "") { //they did not post anything            
                $("#post-error").text("Post not submitted. Please enter a message first!");
            }
            else if (selectedUserProfileIds.length == 0) {
                $("#post-error").text("Post not submitted. At least 1 user must be selected!");
            }
            else if (selectedUserProfileIds.length == 1) {
                $("#post-error").text("Post not submitted. At least 1 user (besides yourself) must be selected!");
            }
            else { //continue
                //push post to server                
                var visibleTo = "";
                for (var i = 0; i < selectedUserProfileIds.length; i++) {
                    if (i != selectedUserProfileIds.length - 1) {
                        visibleTo += selectedUserProfileIds[i] + ",";
                    }
                    else {
                        visibleTo += selectedUserProfileIds[i];
                    }
                }

                $.ajax({
                    type: "POST",
                    url: "/Feed/PostFeedItem",
                    dataType: "json",
                    data: { text: postContent, emailToClass: false, postVisibilityGroups: "Selected Users", eventVisibleTo: visibleTo, notifyHub: true },
                    success: function (data) {
                        //disable spam posting
                        $('#post-selected').css('opacity', '0.65');
                        $('#post-selected').css('cursor', 'not-allowed');
                        $('#post-selected').addClass('disabled');
                        //success message
                        $("#post-success").text("Question successfully posted to the activity feed!");
                    },
                    error: function (data, textStatus, jqXHR) {
                        $("#post-error").text("There was a problem submitting your post. Post not submitted.");
                    }
                });
            }
        }
    });

    $("#post-class-feed").on("click", function () {
        PostToClass();
    });

    $("#post-class-feed-anon").on("click", function () {
        PostToClass(true);
    });

    $("[id^=thumbs-up-feedback], [id^=thumbs-down-feedback]").on("click", function () {

        var lastInterventionId = $("#intervention-clicked").val();
        var currentUrl = window.location.href;

        var id = $(this).prop("id");
        var idParts = id.split("-");
        var interventionId = "";
        if (idParts.length == 3) { //not an intervention, a page
            interventionId = $("#iid").val();
        } else if (idParts.length == 4) { //an intervention
            interventionId = idParts[3]; //last element is the id integer
        }

        $("#mark-clicked").val(id);
        var feedbackDetails = $("#mark-clicked").val();
        
        if (lastInterventionId != interventionId) { //re-enable submit button and reset textarea
            $('#submit-feedback').css('opacity', '1.0');
            $('#submit-feedback').css('cursor', 'pointer');
            $('#submit-feedback').removeClass('disabled');
            $("#feedback-textarea").val("");
        }
        $("#intervention-clicked").val(interventionId);
        
        if ($("#feedback-prompt-text-" + interventionId).length) {
            $("#modal-feedback-suggestion-details").empty();
            $("#modal-feedback-suggestion-details").append("<h5>" + $("#feedback-prompt-text-prefix-" + interventionId).val() + $("#feedback-prompt-text-" + interventionId).val() + "</h5>");
        }
        else
        {            
            $("#modal-feedback-suggestion-details").empty();
            $("#modal-feedback-suggestion-details").append("<h5>" + $("#default-feedback-prompt").val() + "</h5>");
        }

        //send off the up/down helpful mark in case they do not want to elaborate and just close the box.                
        SubmitFeedbackVote(interventionId, feedbackDetails, currentUrl);
    });
   
    $('#submit-feedback').on("click", function () {

        var id = $("#mark-clicked").val();
        var idParts = id.split("-");
        var interventionId = "";
        if (idParts.length == 3) { //not an intervention, a page
            interventionId = $("#iid").val();
        } else if (idParts.length == 4) { //an intervention
            interventionId = idParts[3]; //last element is the id integer
        }
        
        var currentUrl = window.location.href;
        var feedbackComment = $("#feedback-textarea").val();
        var feedbackDetails = $("#mark-clicked").val();

        SubmitFeedback(interventionId, feedbackDetails, feedbackComment, currentUrl);
    });

    $('#submit-user-feedback').on("click", function () {
        var feedbackComment = $("#feedback-textarea").val();
        var currentUrl = window.location.href;
        //using -1 to indicate the feedback is not tied to an intervention.
        SubmitFeedback(-1, "user-feedback", feedbackComment, currentUrl);
    });

    $("#update-status-post-class").on("click", function () {

        $("#update-post-result").text("");
        $("#update-post-result-error").text("");
        $("#post-result").text("");
        $("#post-success").text("");
        $("#post-error").text("");

        if ($('#update-status-post-class').hasClass('disabled')) {
            //do nothing
        }
        else {
            UpdateStatus();
            PostToClass();
        }
    });

    $("#post-to-class").on("click", function () {

        if ($('#post-to-class-anon').hasClass('disabled') || $('#post-to-class').hasClass('disabled')) {
            //do nothing
        }
        else {
            $("#post-success").text("");
            $("#post-error").text("");

            var postContent = $("textarea[name='ask-a-question']").val();
            var codeSnippet = $("#code-edit-box").children()[0].value;

            if (postContent == "") { //they did not post anything
                $("#post-error").text("Post not submitted. Please enter a message first.");
            }
            else //continue processing post
            {
                var success = false;
                //push post to server
                $.ajax({
                    url: "/Intervention/PostToActivityFeed",
                    data: { postContent: postContent, codeSnippet: codeSnippet },
                    method: "POST",
                    success: function (result) {
                        //dismiss if db change successful                        
                        if (result == "True") {
                            $('#post-to-class').css('opacity', '0.65');
                            $('#post-to-class').css('cursor', 'not-allowed');
                            $('#post-to-class').addClass('disabled');
                            $('#post-to-class-anon').css('opacity', '0.65');
                            $('#post-to-class-anon').css('cursor', 'not-allowed');
                            $('#post-to-class-anon').addClass('disabled');
                            $("#post-success").text("Question successfully posted to the activity feed!");
                        }
                    },
                    error: function (result) {
                        $("#post-error").text("There was a problem submitting your post. Post not submitted.");
                    }
                });
            }
        }
    });

    $("#post-to-class-anon").on("click", function () {
        
        if ($('#post-to-class-anon').hasClass('disabled') || $('#post-to-class').hasClass('disabled')) {
            //do nothing
        }
        else {
            $("#post-success").text("");
            $("#post-error").text("");

            var postContent = $("textarea[name='ask-a-question']").val();
            var codeSnippet = $("#code-edit-box").children()[0].value;

            if (postContent == "") { //they did not post anything
                $("#post-error").text("Post not submitted. Please enter a message first.");
            }
            else //continue processing post
            {
                var success = false;
                //push post to server
                $.ajax({
                    url: "/Intervention/PostToActivityFeed",
                    data: { postContent: postContent, codeSnippet: codeSnippet, isAnonymous: true },
                    method: "POST",
                    success: function (result) {
                        //dismiss if db change successful                        
                        if (result == "True") {
                            $('#post-to-class').css('opacity', '0.65');
                            $('#post-to-class').css('cursor', 'not-allowed');
                            $('#post-to-class').addClass('disabled');
                            $('#post-to-class-anon').css('opacity', '0.65');
                            $('#post-to-class-anon').css('cursor', 'not-allowed');
                            $('#post-to-class-anon').addClass('disabled');
                            $("#post-success").text("Question successfully posted to the activity feed!");
                        }
                    },
                    error: function (result) {
                        $("#post-error").text("There was a problem submitting your post. Post not submitted.");
                    }
                });
            }
        }
    });

    $("#view-other-page").on("click", function () {
        UpdatePreviousUrl();
        LogClick("View-All-Feed-Posts");
    });
});

function LogClick(action) {
    var interventionId = $("#iid").val();
    var currentpage = window.location.href;
    
    $.ajax({
        url: "/Intervention/LogClick",
        data: { interventionId: interventionId, currentpage: currentpage, action: action },
        method: "POST",
        success: function (result) {
            //do nothing
        },
        error: function (result) {
            //do nothing
        }
    });
}

function UpdateStatus() {
    //clear post update status text
    $("#update-post-result").text("");
    $("#update-post-result-error").text("");

    var currentUserName = $("#cu-name").val();
    var currentUserProfileId = $("#cupid").val();
    var userStatus = $("#status-message-placeholder").val();
    var availableStartTime = Date.now();
    var availableEndTime = $(".timepicker").val();
    var isAvailableToHelp = $("#availability-check").hasClass("active");

    var endDate = $("#datepicker").datepicker('getDate');
    var month = endDate.getMonth() + 1;
    var day = endDate.getDate();
    var year = endDate.getFullYear();

    //used to detect which page we're on
    var url = window.location.href;

    if (availableEndTime === "" && isAvailableToHelp) {
        //they didn't select a time.            
        $("#update-post-result-error").text("Status NOT Updated! Please select a time you are available until.");
        return false;
    }
    else {
        //update user status in the db
        $.ajax({
            url: "/Intervention/UpdateUserStatus",
            data: { userStatus: userStatus, availableStartTime: availableStartTime, availableEndTime: availableEndTime, isAvailableToHelp: isAvailableToHelp, month: month, day: day },
            method: "POST",
            success: function (result) {
                //dismiss if db change successful                        
                if (result == "True") {
                    //show success message
                    $("#update-post-result").text("Status Updated!");

                    //add list item if it does not currently exist
                    if (!$("#user-checkbox-" + currentUserProfileId).length) {
                        /* it doesn't exist, add it to the list */
                        var listItem = "<div class=\"checkbox\"> " +
                                       "<h5> " +
                                       "<span id=\"user-checkbox-" + currentUserProfileId + "\"></span> " +
                                       "<span id=\"user-image-" + currentUserProfileId + "\"></span> " +
                                       "<span id=\"user-name-" + currentUserProfileId + "\"></span> " +
                                       "<span id=\"user-status-" + currentUserProfileId + "\"></span> " +
                                       "<span id=\"user-available-time-" + currentUserProfileId + "\"></span> " +
                                       "</h5> " +
                                       "</div> ";

                        $("#available-user-list").append('<li  class="user-availability-row">' + listItem + '</li>');

                        $("#available-select-all").css("display", "inline");
                        $("#no-users-available").css("display", "none");                        

                        if (url.indexOf("OfferHelp") >= 0) {
                            $("#status-header").text("Update Your Status");
                        }
                        else {
                            $("#status-header").text("Modify Your Status");
                        }
                    }

                    //empty
                    $("#user-checkbox-" + currentUserProfileId).empty();
                    $("#user-image-" + currentUserProfileId).empty();
                    $("#user-name-" + currentUserProfileId).empty();
                    $("#user-status-" + currentUserProfileId).empty();
                    $("#user-available-time-" + currentUserProfileId).empty();
                    //rebuild
                    $("#user-checkbox-" + currentUserProfileId).append("<input type=\"checkbox\" class=\"checkbox\" name=\"user-selected\" value=\"" + currentUserProfileId + "\" checked=\"checked\">");
                    $("#user-image-" + currentUserProfileId).append("<img src=\"/User/" + currentUserProfileId + "/Picture?size=32\" class=\"small_profile_picture\" alt=\"Profile Picture\" />");
                    $("#user-name-" + currentUserProfileId).append("<strong>" + currentUserName + ":</strong>");
                    $("#user-status-" + currentUserProfileId).append(" \"" + userStatus + " \"");
                    $("#user-available-time-" + currentUserProfileId).append("<em>until </em>" + month + "/" + day + "/" + year + " " + availableEndTime);

                    //also need to update the user listing if they are being added or removed
                    if (!isAvailableToHelp) {                        
                        //Remove them from the list
                        $("#user-checkbox-" + currentUserProfileId).closest("li").remove();
                        if (url.indexOf("OfferHelp") >= 0) {
                            $("#status-header").text("Update Your Status (Your current status is 'unavailable')");                            
                        }
                        else {
                            $("#status-header").text("Modify Your Status (Your current status is 'unavailable')");
                        }
                    }
                    
                    //clean up list
                    if ($(".user-availability-row").size() == 1) {
                        $("#no-users-available").css("display", "inline");
                        $("#available-select-all").css("display", "none");
                    }

                    return true;
                }
            },
            error: function (result) {
                $("#update-post-result-error").text("Status NOT Updated! Unknown Error.");
                return false;
            }
        });
    }
}

function UpdatePreviousUrl() {
    localStorage.setItem('prevUrl', window.location.href);
}

function GetPreviousUrl() {
    return localStorage.prevUrl;
}

function PostToClass(isAnonymous) {
    if ($('#post-class-feed').hasClass('disabled') || $('#post-class-feed-anon').hasClass('disabled')) {
        //do nothing
    }
    else {
        //clear feedback
        $("#post-success").text("");
        $("#post-error").text("");

        var postContent = $("textarea[name='ask-a-question']").val();

        if (postContent == "") { //they did not post anything            
            $("#post-error").text("Post not submitted. Please enter a message first!");
            return false;
        }
        else { //continue
            //push post to server                
            var visibleTo = "";

            if (isAnonymous == undefined) {
                isAnonymous = false;
            }            

            $.ajax({
                type: "POST",
                url: "/Feed/PostFeedItem",
                dataType: "json",
                data: { text: postContent, emailToClass: false, postVisibilityGroups: "class", eventVisibleTo: visibleTo, notifyHub: true, isAnonymous: isAnonymous },
                success: function (data) {
                    //disable spam posting
                    $('#post-class-feed').css('opacity', '0.65');
                    $('#post-class-feed').css('cursor', 'not-allowed');
                    $('#post-class-feed').addClass('disabled');
                    $('#post-class-feed-anon').css('opacity', '0.65');
                    $('#post-class-feed-anon').css('cursor', 'not-allowed');
                    $('#post-class-feed-anon').addClass('disabled');
                    $('#update-status-post-class').css('opacity', '0.65');
                    $('#update-status-post-class').css('cursor', 'not-allowed');
                    $('#update-status-post-class').addClass('disabled');
                    //success message
                    $("#post-success").text("Question successfully posted to the activity feed!");
                    return true;
                },
                error: function () {
                    $("#post-error").text("There was a problem submitting your post. Post not submitted.");
                    return false;
                }
            });
        }
    }
}

function SubmitFeedbackVote(interventionId, feedbackDetails, currentUrl) {
    $("#feedback-post-success").text("");
    $("#feedback-post-error").text("");

    if ($('#submit-feedback').hasClass('disabled')) {
        //do nothing
    }
    else {
        //send feedback to the db
        $.ajax({
            type: "POST",
            url: "/Intervention/SubmitFeedbackVote",
            data: { interventionId: interventionId, feedbackDetails: feedbackDetails, currentUrl: currentUrl },
            success: function (data) {
                //do nothing
            },
            error: function () {
                //do nothing
            }
        });
    }
}

function SubmitFeedback(interventionId, feedbackDetails, feedbackComment, currentUrl) {

    if (feedbackComment == "") {
        $("#feedback-post-error").text("Please type a comment before submitting feedback!");
    }

    if ($('#submit-feedback').hasClass('disabled') || $('#submit-user-feedback').hasClass('disabled') || feedbackComment == "") {
        //do nothing
    }
    else {
        $("#feedback-post-success").text("");
        $("#feedback-post-error").text("");
        //send feedback to the db
        $.ajax({
            type: "POST",
            url: "/Intervention/SubmitFeedback",
            data: { interventionId: interventionId, feedbackDetails: feedbackDetails, feedbackComment: feedbackComment, currentUrl: currentUrl },
            success: function (result) {
                if (result == "True") {
                    //disable spam posting
                    $('#submit-feedback').css('opacity', '0.65');
                    $('#submit-feedback').css('cursor', 'not-allowed');
                    $('#submit-feedback').addClass('disabled');
                    $('#submit-user-feedback').css('opacity', '0.65');
                    $('#submit-user-feedback').css('cursor', 'not-allowed');
                    $('#submit-user-feedback').addClass('disabled');
                    //success - close the modal dialog
                    $("#feedback-post-success").text("Feedback submitted successfully!");
                    //$('#feedback-modal').modal('toggle');
                    window.setTimeout(function () {
                        $('#feedback-modal').modal('toggle');
                    }, 1000); // delay 1 second then dismiss the modal
                }
                else {
                    $("#feedback-post-error").text("There was a problem submitting your feedback, please try again. Feedback not submitted.");
                }
            },
            error: function () {
                $("#feedback-post-error").text("There was a problem submitting your feedback, please try again. Feedback not submitted.");
            }
        });
    }
}

$(function () {
    $('.button-checkbox').each(function () {

        // Settings
        var $widget = $(this),
            $button = $widget.find('button'),
            $checkbox = $widget.find('input:checkbox'),
            color = $button.data('color'),
            settings = {
                on: {
                    icon: 'glyphicon glyphicon-check'
                },
                off: {
                    icon: 'glyphicon glyphicon-unchecked'
                }
            };

        // Event Handlers
        $button.on('click', function () {
            $checkbox.prop('checked', !$checkbox.is(':checked'));
            $checkbox.triggerHandler('change');
            updateDisplay();
        });
        $checkbox.on('change', function () {
            updateDisplay();
        });

        // Actions
        function updateDisplay() {
            var isChecked = $checkbox.is(':checked');

            // Set the button's state
            $button.data('state', (isChecked) ? "on" : "off");

            // Set the button's icon
            $button.find('.state-icon')
                .removeClass()
                .addClass('state-icon ' + settings[$button.data('state')].icon);

            // Update the button's color
            if (isChecked) {
                $button
                    .removeClass('btn-default')
                    .addClass('btn-' + color + ' active');
                $("#availability-text").removeClass("hidden");
            }
            else {
                $button
                    .removeClass('btn-' + color + ' active')
                    .addClass('btn-default');
                $("#availability-text").addClass("hidden");
            }
        }

        // Initialization
        function init() {

            updateDisplay();

            // Inject the icon if applicable
            if ($button.find('.state-icon').length == 0) {
                $button.prepend('<i class="state-icon ' + settings[$button.data('state')].icon + '"></i> ');
            }
        }
        init();
    });
});
