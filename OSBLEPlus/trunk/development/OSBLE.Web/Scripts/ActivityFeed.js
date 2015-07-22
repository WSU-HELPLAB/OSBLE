

function decreaseConversationsNumber(id) {
    if (id === null) return;
    var elem = document.getElementById("number-of-comments-for-" + id);
    if (elem != null) {
        elem.innerHTML = parseInt(elem.innerHTML) - 1;
    }
}

function increaseConversationsNumber(id) {
    if (id === null) return;
    var elem = document.getElementById("number-of-comments-for-" + id);
    if (elem != null) {
        elem.innerHTML = parseInt(elem.innerHTML) + 1;
    }
}

function ShowReplyBox(lastLogID) {
    $("#btn-reply-" + lastLogID).hide();
    $("#feed-reply-" + lastLogID).show('blind');
    return false;
}

function HideReplyBox(lastLogID) {
    $("#btn-reply-" + lastLogID).show('highlight');
    $("#feed-reply-" + lastLogID).hide('blind');
    return false;
}

function expandComments(lastLogID) {
    var commentsTextSpan = "#expand-comments-text-" + lastLogID;
    var replies = "#feed-item-comments-" + lastLogID;

    if ($(replies).css('display') == 'none') {
        $(replies).show('blind');
        $(commentsTextSpan).text("Hide");
    }
    else {
        var height = (window.innerHeight > 0) ? window.innerHeight : screen.height;
        var post = "#feed-item-" + lastLogID;

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

function PostReplyComplete(logID) {
    var replies = $("#feed-item-comments-" + logID);

    // make sure reply block is visible
    replies.css('display', 'block');
    $("#expand-comments-text-" + logID).text("Hide");

    // highlight the new reply
    replies.children().last().addClass('just-posted-reply');

    // scroll to new reply
    var post = $("#feed-item-" + logID);
    var height = (window.innerHeight > 0) ? window.innerHeight : screen.height;
    $('html, body').animate({
        scrollTop: post.offset().top + post.height() - height + 5
    }, 500);
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

//Updates a KO view model using the supplied JS object
function updateFeedItemViewModel(jsObject) {

    $.each(jsObject.Data, function (index, value) {

        //bind to new view model
        var model = {
            Comments: value.Comments,
            NumberOfComments: ko.observable(value.Comments.length),
            LastUpdated: ko.observable(new Date())
        };

        //model mapping
        var mapping =
            {
                'Comments': {
                    key: function (item) {
                        return ko.utils.unwrapObservable(item.CommentId);
                    }
                }
            };

        //compute local time
        $(model.Comments).each(function (index) {

            var milliseconds = model.Comments[index].UtcUnixDate + "";
            var formatString = "MM/DD/YYYY hh:mm A";
            var currentDate = moment.utc(milliseconds, 'X');
            var localDate = new Date();
            var localOffset = localDate.getTimezoneOffset();
            currentDate = currentDate.subtract('minutes', localOffset);
            model.Comments[index]['LocalDate'] = currentDate.format(formatString);
        });

        var toBind = "feed-item-" + value.OriginalLogId;

        //view model doesn't exist, create one
        if (!AllComments[value.ActualLogId]) {

            AllComments[value.ActualLogId] = ko.mapping.fromJS(model, mapping);
        }

        //update view model with server data
        ko.mapping.fromJS(model, AllComments[value.ActualLogId]);

        //apply binding if one doesn't already exist
        if (!ko.dataFor(document.getElementById(toBind))) {
            ko.applyBindings(AllComments[value.ActualLogId], document.getElementById(toBind));
        }
    });
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

function editText(id, bool) {
    var editForm = $("#edit-form-items-" + id);
    var originalText = $('#content-comment-' + id);
    var textFromInput = $('#edit-form-textarea-' + id);

    if (bool === true) {
        originalText.html(textFromInput.val());
    }
    else {
        originalText.html(text);
    }

    editForm.hide("blind");
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
