
window.onbeforeunload = function () {
    $.connection.hub.stop();
};

//KnockoutJS Objects
function FeedItem(data) {
    // Fields
    var self = this;
    self.eventId = data.EventId;
    self.parentEventId = data.ParentEventId;
    self.senderName = data.SenderName;
    self.senderId = data.SenderId;
    self.eventLogId = data.EventLogId;
    self.timeString = ko.observable(data.TimeString);
    self.eventDate = data.EventDate;
    self.options = new FeedItemOptions(data.CanMail, data.CanDelete, data.CanEdit, data.ShowPicture, data.CanVote);
    self.show = true;
    self.isComment = self.parentEventId != -1;
    self.isHelpfulMark = data.IsHelpfulMark;
    self.highlightMark = ko.observable(data.HighlightMark);
    self.htmlContent = ko.observable(data.HTMLContent);
    self.numberHelpfulMarks = ko.observable(data.NumberHelpfulMarks);
    self.idString = data.IdString; // used for items with multiple ids
    self.activeCourseUserId = data.ActiveCourseUserId;

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
        var text = $("#feed-edit-textbox-" + self.eventId).val();

        // disable posting so user can't "spam" post
        $("#feed-edit-textbox-" + self.eventId).attr("disabled", "disabled");
        $("#feed-edit-submit-" + self.eventId).attr("disabled", "disabled");
        $("#feed-edit-cancel-" + self.eventId).attr("disabled", "disabled");

        $.ajax({
            url: self.isComment? "/Feed/EditLogComment" : "/Feed/EditFeedPost",
            data: { id: self.eventId, newText: text },
            dataType: "json",
            method: "POST",
            success: function (dataObj) {
                //self.htmlContent(dataObj.HTMLContent);
                //self.timeString(dataObj.TimeString);

                // re-enable buttons and textbox
                $("#feed-edit-textbox-" + self.eventId).removeAttr("disabled");
                $("#feed-edit-submit-" + self.eventId).removeAttr("disabled");
                $("#feed-edit-cancel-" + self.eventId).removeAttr("disabled");

                EditSucceeded(self, dataObj.HTMLContent, dataObj.TimeString);
            },
            error: function () {
                EditFailed(self);
            }
        });
    };

    self.MarkCommentHelpful = function () {
        $.ajax({
            url: "/Feed/MarkHelpfulComment",
            data: { eventLogToMark: self.eventId, markerId: self.activeCourseUserId },
            dataType: "json",
            method: "GET",
            success: function (data) {
                self.highlightMark(data.isMarker);
                MarkHelpfulSucceeded(self, data.helpfulMarks);
            }
        });
    };

    self.AddComment = function () {
        if (self.isComment)
            return;

        // Get text from the textarea
        var text = $('#feed-reply-textbox-' + self.eventId).val();

        // Disable buttons and textbox so the user cannot "spam" replies
        $("#feed-reply-textbox-" + self.eventId).attr("disabled", "disabled");
        $("#feed-reply-submit-" + self.eventId).attr("disabled", "disabled");
        $("#feed-reply-cancel-" + self.eventId).attr("disabled", "disabled");

        // Make sure the user submited something
        if (text == "") {
            return;
        }

        $.ajax({
            url: "/Feed/PostComment",
            data: { id: self.eventId, content: text },
            dataType: "json",
            method: "POST",
            success: function (dataList) {
                PostReplySucceeded(self, dataList);
            },
            error: function() {
                PostReplyFailed(self);
            }
        });
    };

    self.GetComment = function (id) {
        var post = null;
        $.each(self.comments(), function (index, value) {
            if (value.eventId == id)
                post = value;
        });
        return post;
    }
}

function FeedItemOptions(canMail, canDelete, canEdit, showPicture, canVote)
{
    var self = this;
    self.canMail = canMail;
    self.canDelete = canDelete;
    self.canEdit = canEdit;
    self.showPicture = showPicture;
    self.canVote = canVote;
}

function FeedViewModel(userName, userId, current) {
    var self = this;
    self.userName = userName;
    self.userId = userId;
    self.items = ko.observableArray();
    self.keywords = ko.observable("");
    //self.keywords.subscribe(function (newValue) {
    //    self.RequestUpdate();
    //});


    // *** AUTO-UPDATE WEB SOCKET STUFF ***
    self.hub = $.connection.activityFeedHub;

    self.hub.client.addNewReply = function (postID, dataList) {
        // Add the reply to the page
        var post = self.GetPost(postID);
        if (post != null) {
            var commentList = $.map(dataList, function (item) {
                SetPermissions(item);
                return new FeedItem(item)
            });
            post.comments(commentList);

            HighlightNewReply(postID);
        }
    };

    self.hub.client.addNewPost = function (courseID, postData) {
        if (courseID == GetSelectedCourseID()) {
            SetPermissions(postData);
            self.items.unshift(new FeedItem(postData)); // unshift puts object at beginning of array
            HighlightNewPost(postData.EventId);
        }
    }

    self.hub.client.addMarkHelpful = function (postID, replyPostID, numHelpfulMarks) {
        var post = self.GetPost(postID);
        if (post != null) {
            var comment = post.GetComment(replyPostID);
            if (comment != null) {
                comment.numberHelpfulMarks(numHelpfulMarks);
            }
        }
    }

    self.hub.client.editReply = function (postID, replyPostID, content, timeString) {
        var post = self.GetPost(postID);
        if (post != null) {
            var comment = post.GetComment(replyPostID);
            if (comment != null) {
                comment.htmlContent(content);
                comment.timeString(timeString);
            }
        }
    }

    self.hub.client.editPost = function (postID, content, timeString) {
        var post = self.GetPost(postID);
        if (post != null) {
            post.htmlContent(content);
            post.timeString(timeString);
        }
    }

    // Start the connection
    $.connection.hub.qs = { "userID": self.userId, "courseID": GetSelectedCourseID() };
    $.connection.hub.start();
    // *************************************


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
                MakePostSucceeded(data);
            },
            error: function () {
                MakePostFailed();
            }
        });
    };

    self.LoadMorePosts = function () {
        if (self.items().length == 0)
            return self.RequestUpdate();

        // set the end date to the current oldest item
        var lastDate = self.items()[self.items().length - 1].eventDate;
        ShowMoreLoading();
        $.ajax({
            type: "POST",
            url: "/Feed/GetMorePosts",
            dataType: "json",
            data: { endDate: lastDate },
            success: function (data) {
                $.each(data.Feed, function (index, value) {
                    self.items.push(new FeedItem(value));
                });
                if (data.HasLastPost) {
                    $('#load-old-posts').addClass('disabled');;
                }
                else {
                    $('#load-old-posts').removeClass('disabled');
                }
            },
            error: function () {
                ShowError("#feed-footer", "Cannot load posts. Check internet connection.", false);
            },
            complete: function() {
                HideMoreLoading();
            }
        });
    };

    self.RequestUpdate = function () {
        ShowLoading();
        $.ajax({
            type: "GET",
            url: "/Feed/GetFeed",
            dataType: "json",
            data: { keywords: self.keywords(), events: GetCheckedEvents() },
            cache: false,
            success: function (data, textStatus, jqXHR) {
                var mappedItems = $.map(data.Feed, function (item) { return new FeedItem(item) });
                self.items(mappedItems);

                if ($('#load-old-posts').hasClass('disabled')) {
                    $('#load-old-posts').removeClass('disabled');
                }
            },
            complete: function() {
                HideLoading();
            }
        });
    };

    self.GetPost = function (id) {
        var post = null;
        $.each(self.items(), function (index, value) {
            if (value.eventId == id)
                post = value;
        });
        return post;
    };

    $("#activity-feed-filters").submit(function (e) {
        self.RequestUpdate();
        return false; // prevent page refresh
    });
}

function DetailsViewModel(userName, userId, rootId)
{
    var self = this;
    self.userName = userName;
    self.userId = userId;
    self.rootId = rootId;
    self.items = ko.observableArray([]);


    // *** AUTO-UPDATE WEB SOCKET STUFF ***
    self.hub = $.connection.activityFeedHub;

    self.hub.client.addNewReply = function (postID, dataList) {
        if (postID == self.rootId) {
            var commentList = $.map(dataList, function (item) {
                SetPermissions(item);
                return new FeedItem(item);
            });
            self.items()[0].comments(commentList);

            HighlightNewReply(postID);
        }
    };

    self.hub.client.addMarkHelpful = function (postID, replyPostID, numHelpfulMarks) {
        if (postID == self.rootId) {
            var comment = self.items()[0].GetComment(replyPostID);
            if (comment != null) {
                comment.numberHelpfulMarks(numHelpfulMarks);
            }
        }
    }

    self.hub.client.editReply = function (postID, replyPostID, content, timeString) {
        if (postID == self.rootId) {
            var comment = self.items()[0].GetComment(replyPostID);
            if (comment != null) {
                comment.htmlContent(content);
                comment.timeString(timeString);
            }
        }
    }

    self.hub.client.editPost = function (postID, content, timeString) {
        if (postID == self.rootId) {
            var post = self.items()[0];
            post.htmlContent(content);
            post.timeString(timeString);
        }
    }

    // Start the connection
    $.connection.hub.qs = { "userID": self.userId, "courseID": GetSelectedCourseID() };
    $.connection.hub.start();
    // *************************************


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

function ProfileViewModel(userName, userId, profileUserId) {
    // userName and userId refer to the user who is viewing the feed
    // profileUserId is the id of the user who's profile is being viewed.
    var self = this;
    self.userName = userName;
    self.userId = userId;
    self.profileUserId = profileUserId;
    self.items = ko.observableArray([]);

    // *** AUTO-UPDATE WEB SOCKET STUFF ***
    self.hub = $.connection.activityFeedHub;

    //TODO: add methods for update

    // Start the connection
    $.connection.hub.qs = { "userID": self.userId, "courseID": GetSelectedCourseID() };
    $.connection.hub.start();
    // *************************************

    self.RequestUpdate = function () {
        $.ajax({
            type: "POST",
            url: "/Feed/GetProfileFeed",
            data: { userId: profileUserId },
            dataType: "json",
            async: false,
            cache: false,
            success: function (data, textStatus, jqXHR) {
                var itemAsList = [new FeedItem(data.Item)];
                self.items(itemAsList);
            }
        });
    };
}




/* Regular JS Functions */

function SetPermissions(post)
{
    // don't bother reseting permissions if we were the poster 
    // (only if post, we do need to worry about this with replies)
    if (post.ParentEventId == -1 && post.SenderId == vm.userId)
        return;

    $.ajax({
        type: "POST",
        async: false,
        url: "/Feed/GetPermissions",
        datatype: "json",
        data: { eventId: post.EventId },
        success: function (data) {
            post.CanDelete = data.canDelete;
            post.CanEdit = data.canEdit;
            post.CanMail = data.canMail;
            post.CanVote = data.canVote;
            post.ShowPicture = data.showPicture;
        },
        error: function () {
            post.CanDelete = false;
            post.CanEdit = false;
            post.CanMail = false;
            post.CanVote = false;
            post.ShowPicture = false;
        }
    });
}

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

    var length = str.length;
    var stringWithoutComma = str.substr(0, length - 1);
    
    return stringWithoutComma;
}

function onDeleteSuccess(item)
{
    $('#feed-item-' + item.eventId).hide('blind', {}, 'slow', function () { $(this).remove(); });
}

function ShowEditBox(item)
{
    // populate the box with the correct content
    var text = $("#content-comment-" + item.eventId).text();
    $('#feed-edit-textbox-' + item.eventId).val(text);

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

function HighlightNewPost(postID)
{
    // Show a nifty animation for the new post
    $('#feed-item-' + postID).hide().show('easeInBounce');
}

function MakePostSucceeded(newPost)
{
    // Clear the textbox
    $('#feed-post-textbox').val('');

    // re-enable posting
    $('#feed-post-textbox').removeAttr('disabled');
    $('#btn_post_active').removeAttr('disabled');

    // notify others about the new post
    vm.hub.server.notifyNewPost(newPost);
}

function MakePostFailed()
{
    ShowError('#feed-post-form', 'Unable to create post, check internet connection.', false);
}

function HighlightNewReply(postID)
{
    var replies = $("#feed-item-comments-" + postID);
    replies.children().last().hide().show('easeInBounce');
}

function PostReplySucceeded(item, dataList) {

    // make sure comments block is visible
    var replies = $("#feed-item-comments-" + item.eventId);
    if (replies.css('display') == 'none') {
        expandComments(item);
    }

    // clear textbox
    $("#feed-reply-textbox-" + item.eventId).val('');

    // hide the reply form
    $("#btn-reply-" + item.eventId).show();
    $("#feed-reply-" + item.eventId).hide();

    // notify everyone that a reply was made
    vm.hub.server.notifyNewReply(item.eventId, dataList);

    // re-enabled buttons and textbox
    $("#feed-reply-textbox-" + item.eventId).removeAttr("disabled");
    $("#feed-reply-submit-" + item.eventId).removeAttr("disabled");
    $("#feed-reply-cancel-" + item.eventId).removeAttr("disabled");
}

function PostReplyFailed(item)
{
    // Set the error message text and display it for 4 seconds
    ShowError('#feed-reply-' + item.eventId, 'Unable to submit reply. Check internet connection.', true);
}

function EditSucceeded(item, content, timeString)
{
    //notify everyone that an edit was made
    if (item.isComment)
    {
        vm.hub.server.notifyEditReply(item.parentEventId, item.eventId, content, timeString);
    }
    else
    {
        vm.hub.server.notifyEditPost(item.eventId, content, timeString);
    }

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

function MarkHelpfulSucceeded(item, numMarks)
{
    vm.hub.server.notifyAddMarkHelpful(item.parentEventId, item.eventId, numMarks);
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

function ShowLoading()
{
    if ($('#loadingMsg').css('display') != 'none')
        return;

    $('#loadingMsg').show('fade');
    $('#load-old-posts').hide();
}

function HideLoading()
{
    if ($('#loadingMsg').css('display') == 'none')
        return;

    $('#loadingMsg').hide('fade');
    $('#load-old-posts').show();
}

function ShowMoreLoading()
{
    $('#loadingMoreMsg').show('fade');
    $('#load-old-posts').hide();
}

function HideMoreLoading()
{
    $('#loadingMoreMsg').hide();
    $('#load-old-posts').show();
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
