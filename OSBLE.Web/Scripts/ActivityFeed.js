﻿//Updates a KO view model using the supplied JS object
//function updateFeedItemViewModel(jsObject) {

//    $.each(jsObject.Data, function (index, value) {

//        //bind to new view model
//        var model = {
//            Comments: value.Comments,
//            NumberOfComments: ko.observable(value.Comments.length),
//            LastUpdated: ko.observable(new Date())
//        };

//        //model mapping
//        var mapping =
//            {
//                'Comments': {
//                    key: function (item) {
//                        return ko.utils.unwrapObservable(item.CommentId);
//                    }
//                }
//            };

//        //compute local time
//        $(model.Comments).each(function (index) {

//            var milliseconds = model.Comments[index].UtcUnixDate + "";
//            var formatString = "MM/DD/YYYY hh:mm A";
//            var currentDate = moment.utc(milliseconds, 'X');
//            var localDate = new Date();
//            var localOffset = localDate.getTimezoneOffset();
//            currentDate = currentDate.subtract('minutes', localOffset);
//            model.Comments[index]['LocalDate'] = currentDate.format(formatString);
//        });

//        var toBind = "feed-item-" + value.OriginalLogId;

//        //view model doesn't exist, create one
//        if (!AllComments[value.ActualLogId]) {

//            AllComments[value.ActualLogId] = ko.mapping.fromJS(model, mapping);
//        }

//        //update view model with server data
//        ko.mapping.fromJS(model, AllComments[value.ActualLogId]);

//        //apply binding if one doesn't already exist
//        if (!ko.dataFor(document.getElementById(toBind))) {
//            ko.applyBindings(AllComments[value.ActualLogId], document.getElementById(toBind));
//        }
//    });
//}

//KnockoutJS Objects
function FeedItem(data) {
    // Fields
    var self = this;
    self.eventId = data.EventId;
    self.parentEventId = data.ParentEventId;
    self.senderName = data.SenderName;
    self.senderId = data.SenderId;
    self.time = ko.observable(data.TimeString);
    self.options = new FeedItemOptions(data.CanMail, data.CanDelete, data.CanEdit);
    self.show = true;
    self.isComment = self.parentEventId != -1;
    self.content = ko.observable(data.Content); // used for editing posts
    self.htmlContent = ko.observable(data.HTMLContent);
    self.idString = data.IdString; // used for items with multiple ids

    // load Comments
    self.comments = ko.observableArray([]);
    if (!self.isComment && data.Comments != null && data.Comments.length > 0) {
        var commentList = $.map(data.Comments, function (item) { return new FeedItem(item) });
        self.comments(commentList);
    }
    
    /**** Methods ****/
    // Delete: called when user clicks the trashcan icon on a post or comment, uses an ajax call to the 
    // server for delete, after prompting the user to confirm.
    self.Delete = function () {
        $.ajax({
            url: self.isComment ? "/Feed/DeleteLogComment" : "/Feed/DeleteFeedPost",
            data: {id: self.eventId},
            method: "POST",
            beforeSend: function (jqXHR, settings) {
                // will cancel the request if the user does not ok
                return confirm(self.isComment ? 
                    "Are you sure you want to delete this reply?" : 
                    "Are you sure you want to delete this post and all its replies?");
            },
            success: function () {
                self.show = false;
                onDeleteSuccess(self);
            }
        });
    };

    // Edit: called when the user clicks the send button after clicking the pencil symbol on a post to reveal
    // the edit form. (Note: not called if the user cancels their edit)
    self.Edit = function () {

        $.ajax({
            url: self.isComment? "/Feed/EditLogComment" : "/Feed/EditFeedPost",
            data: { id: self.eventId, newText: self.content() },
            dataType: "json",
            method: "POST",
            success: function (dataObj) {
                self.htmlContent(dataObj.HTMLContent);
                self.time(dataObj.TimeString);
                EditSucceeded(self);
            },
            error: function () {
                EditFailed(self);
            }
        });
    };

    self.AddComment = function () {
        // Get text from the textarea
        var text = $('#feed-reply-textbox-' + self.eventId).val();

        // Make sure the user submited something
        if (text == "") {
            return;
        }

        $.ajax({
            url: "/Feed/PostComment",
            data: { id: self.eventId, content: text},
            dataType: "json",
            method: "POST",
            success: function (dataList) {
                var commentList = $.map(dataList, function (item) { return new FeedItem(item) });
                self.comments(commentList);
                PostReplySucceeded(self);
            },
            error: function () {
                PostReplyFailed(self);
            }
        });
    }
}

function FeedItemOptions(canMail, canDelete, canEdit)
{
    var self = this;
    self.canMail = canMail;
    self.canDelete = canDelete;
    self.canEdit = canEdit;
}

function FeedViewModel(userName, userId) {
    var self = this;
    self.userName = userName;
    self.userId = userId;
    self.items = ko.observableArray();

    self.keywords = ko.observable("");
    self.keywords.subscribe(function (newValue) {
        self.keywords(newValue);
        self.RequestUpdate();
    });

    self.MakePost = function () {
        var text = $("#feed-post-textbox").val();

        if (text == "")
            return;

        // Disable buttons while waiting for server response
        $('#feed-post-textbox').attr('disabled', 'disabled');
        $('#btn_post_active').attr('disabled', 'disabled');

        $.ajax({
            type: "POST",
            url: "/Feed/PostFeedItem",
            dataType: "json",
            data: { text: text },
            success: function (data) {
                self.items.unshift(new FeedItem(data)); // unshift puts object at beginning of array
                MakePostSucceeded(data.EventId);
            },
            error: function() {
                MakePostFailed();
            },
            complete: function () {
                $('#feed-post-textbox').removeAttr('disabled');
                $('#btn_post_active').removeAttr('disabled');
            }
        })
    }

    self.RequestUpdate = function () {
        $.ajax({
            type: "POST",
            url: "/Feed/GetFeed",
            dataType: "json",
            data: { keywords: self.keywords(), events: GetCheckedEvents() },
            cache: false,
            success: function (data, textStatus, jqXHR) {
                var mappedItems = $.map(data.Feed, function (item) { return new FeedItem(item) });
                self.items(mappedItems);
            }
        });
    };

    // load initial state from server
    self.RequestUpdate();
}

function DetailsViewModel(userName, userId, rootId)
{
    var self = this;
    self.userName = userName;
    self.userId = userId;
    self.rootId = rootId;
    self.items = ko.observableArray([]);

    self.RequestUpdate = function () {
        $.ajax({
            type: "POST",
            url: "/Feed/GetDetails",
            data: { id: self.rootId },
            dataType: "json",
            async: false,
            cache: false,
            success: function (data, textStatus, jqXHR) {
                var itemAsList = [new FeedItem(data.Item)];
                self.items(itemAsList);
            }
        });
    };

    self.RequestUpdate();
}


/* Regular JS Functions */

function CheckEvents(events)
{
    var elist = events.split(',');
    $.each(elist, function (index, value) {
        $("#event_" + value).prop("checked", true);
    });
}

function GetCheckedEvents()
{
    var str = "";
    $('.event_checkbox').each(function (index, value) {
        if (value.checked == true) {
            str += $(this).data('type') + ',';
        }
    });
    return str;
}

function onDeleteSuccess(item)
{
    $('#feed-item-' + item.eventId).hide('blind', {}, 'slow', function () { $(this).remove(); });
}

function ShowEditBox(item)
{
    $('#feed-edit-' + item.eventId).show('fade');
    $('#feed-item-content-' + item.eventId).hide();
    $('#btn-edit-' + item.eventId).hide('highlight');
}

function HideEditBox(item)
{
    $('#feed-edit-' + item.eventId).hide();
    $('#feed-item-content-' + item.eventId).show('fade');
    $('#btn-edit-' + item.eventId).show('highlight');
}

function ShowReplyBox(item) {
    $("#btn-reply-" + item.eventId).hide();
    $("#feed-reply-" + item.eventId).show('blind');
    $("#feed-reply-textbox-" + item.eventId).val('');
}

function HideReplyBox(item) {
    $("#btn-reply-" + item.eventId).show('highlight');
    $("#feed-reply-" + item.eventId).hide('blind');
}

function expandComments(item) {
    var commentsTextSpan = "#expand-comments-text-" + item.eventId;
    var replies = "#feed-item-comments-" + item.eventId;

    if ($(replies).css('display') == 'none') {
        $(replies).show('blind');
        $(commentsTextSpan).text("Hide");
    }
    else {
        var height = (window.innerHeight > 0) ? window.innerHeight : screen.height;
        var post = "#feed-item-" + item.eventId;

        // scroll helps if reply box was really big
        if ($(post).height() > height) {
            $('html, body').animate({
                scrollTop: $(post).offset().top - 5
            }, 200);
        }
        $(replies).hide('blind');
        $(commentsTextSpan).text("View");
    }
}

function MakePostSucceeded(newPostId)
{
    // Clear the textbox
    $('#feed-post-textbox').val('');

    // Show a nifty animation for the new post
    $('#feed-item-' + newPostId).hide().show('easeInBounce');
}

function MakePostFailed()
{
    ShowError('#feed-post-form', 'Unable to create post, check internet connection.', false);
}

function PostReplySucceeded(item) {
    var replies = $("#feed-item-comments-" + item.eventId);

    // make sure comments block is visible
    if (replies.css('display') == 'none') {
        expandComments(item);
    }

    // clear textbox
    $("#feed-reply-textbox-" + item.eventId).val('');

    // hide the reply form
    $("#btn-reply-" + item.eventId).show();
    $("#feed-reply-" + item.eventId).hide();

    // highlight the new reply
    replies.children().last().hide().show('easeInBounce');
}

function PostReplyFailed(item)
{
    // Set the error message text and display it for 4 seconds
    ShowError('#feed-reply-' + item.eventId, 'Unable to submit reply. Check internet connection.', true);
}

function EditSucceeded(item)
{
    // clear textbox
    $("#feed-edit-textbox-" + item.eventId).val('');

    // hide the edit form & display regular text
    HideEditBox(item);
}

function EditFailed(item)
{
    // Set the error message text and display it for 4 seconds
    ShowError('#feed-edit-' + item.eventId, 'Unable to edit post. Check internet connection.', true);    
}

function LoadOldPosts()
{

}

//called when the user clicks on the "Load Earlier Posts..." link at the bottom of the page
function getOldPostsSuccess(html) {
    $("#load-old-posts").text("Load earlier posts...");
    var trimmed = $.trim(html);
    if (trimmed.length > 0) {
        $("#feed-items").append(html);
        parseDates();
        $(".feed-item-ajax:hidden.OldFeedItems").each(function (e) {
            $(this).removeClass("feed-item-ajax");
        });
    }
    else {
        $("#load-old-posts").text("No earlier posts available.");
    }
}

//Called after successful AJAX call to get feed item comments
function expandCommentsSuccess(result) {

    //update view model
    updateFeedItemViewModel(result);

    //display comments
    var commentsDiv = "#feed-item-comments-" + result.Data[0].OriginalLogId;
    $(commentsDiv).css('display', 'block');
}

// global for updating text back to the correct text if canceled/changed when editing
var text = '';

function updateText(id) {
    // get the div we're working with
    var originalText = $("#content-comment-" + id);
    var editForm = $("#edit-form-items-" + id);

    // make sure we're not already editing
    if (originalText.html() !== "Editing...") {
        text = originalText.html();
        originalText.html("Editing...");
        editForm.show("blind");
    }
}

function ShowError(containerID, text, insertAbove)
{
    var errBox = $("#errMsgPanel");

    // Set the text
    $("#errText").text(text);

    // Put into container
    if (insertAbove)
        $(containerID).prepend(errBox);
    else
        $(containerID).append(errBox);

    // Show the errBox
    errBox.show('fade');
    setTimeout(function () { errBox.hide('fade'); }, 4000);
}

/*
//Periodically updates view models for feed items.  Useful for displaying an updated count
//of comments for a given feed item.
function getCommentUpdates() {

    var ModelIds = Array();

    //request comment updates for all feed items
    $(".feed-item-single").each(function (index) {
        var log_id = $(this).attr("data-id");
        ModelIds.push(log_id);
    });

    //make the ajax call
    $.ajax(
        {
            url: "/Feed/GetComments",
            data: { logIds: JSON.stringify(ModelIds) },
            dataType: "json",
            type: "POST",
            success: updateFeedItemViewModel
        });

    //call ourselves again in 20 seconds
    setTimeout(getCommentUpdates, 20000)
}

//called when the user clicks the "Send" button
function sendResponse(elementId) {
    var textAreaId = '#feed-item-respond-' + elementId;
    var textArea = $(textAreaId);

    //get and clear user response
    var userResponse = textArea.val();
    textArea.val("");

    //submit response using AJAX
    $.ajax(
        {
            url: "@Url.Action("PostCommentAsync", "Feed")",
            data: { logId: elementId, comment: userResponse },
            dataType: "json",
            type: "POST",
            success: function (data) {
                if (!$.isEmptyObject(data)) {
                    var commentsTextSpan = "#expand-comments-text-" + data.OriginalLogId;
                    $(commentsTextSpan).text("Hide");
                    expandCommentsSuccess(data);
                }
            }
        });
}*@

function showLastHidden() {
    var initialScrollHeight = $(document).scrollTop();
    var item = $(".feed-item-ajax:hidden").last();

    //if we're loading old items,
    if (item.hasClass("OldFeedItems")) {
        item = $(".feed-item-ajax:hidden.OldFeedItems").first();
    }
    item.slideDown(
            {
                duration: 600,
                easing: "linear",
                progress: function (animation, progress, remainingMs) {
                    if (initialScrollHeight > 50) {
                        $(document).scrollTop($(this).height() + initialScrollHeight);
                    }
                }
            });
    parseDates();
    setTimeout(showLastHidden, 4000);
}

function getOldestFeedId() {
    var lastId = $(".feed-item-single").last().attr("data-id");
    if (lastId == undefined) {
        lastId = "@(Model.LastLogId + 1)";
    }
    return lastId;
}

function getMostRecentFeedId() {
    var lastId = $(".feed-item-single").first().attr("data-id");
    if (lastId == undefined) {
        lastId = "@Model.LastLogId";
    }
    return lastId;
}

function getRecentFeedItems() {

    //find most recent log id
    var lastId = getMostRecentFeedId();
    var urlParamTokens = window.location.search.substring(1).split('&');
    var hashVal = urlParamTokens.length > 1 && urlParamTokens[1].substring(0, 4) == "hash" ? 1 : 0;
    $.ajax(
        {
            url: "@Url.Action("RecentFeedItems", "Feed")",
            @*data: { id: lastId, userId: "@Model.SingleUserId", errorType: "@Model.SelectedErrorType.Id", keyword:"@Model.Keyword", hash:hashVal},*@
            dataType: "html",
            type: "GET",
            success: getRecentFeedItemsSuccess,
            complete: function () { setTimeout(getRecentFeedItems, 30000); }
        }
        );
}

function getRecentFeedItemsSuccess(html) {
    var trimmed = $.trim(html);
    if (trimmed.length > 0) {
        $("#hidden-workspace").append(html);
        parseDates();
        if ($("#hidden-workspace").find('.feed-item-single').length > 0) {
            $("#feed-items").prepend(html);
        }
        $("#hidden-workspace").empty();
    }
}
*/
