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
    self.options = new FeedItemOptions(data.CanMail, data.CanDelete, data.CanEdit, data.ShowPicture, data.CanVote, data.HideMail, data.EventVisibilityGroups, data.EventVisibleTo, data.IsAnonymous, data.senderId);
    self.show = true;
    self.isComment = self.parentEventId != -1;
    self.isHelpfulMark = data.IsHelpfulMark;
    self.highlightMark = ko.observable(data.HighlightMark);
    self.htmlContent = ko.observable(data.HTMLContent);
    self.numberHelpfulMarks = ko.observable(data.NumberHelpfulMarks);
    self.idString = data.IdString; // used for items with multiple ids
    self.activeCourseUserId = data.ActiveCourseUserId;
    self.eventType = data.EventType;
    self.role = data.Role;

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
            data: { id: self.eventId },
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

        text = replaceMentionWithId(text);

        // disable posting so user can't "spam" post
        $("#feed-edit-textbox-" + self.eventId).attr("disabled", "disabled");
        $("#feed-edit-submit-" + self.eventId).attr("disabled", "disabled");
        $("#feed-edit-cancel-" + self.eventId).attr("disabled", "disabled");

        $.ajax({
            url: self.isComment ? "/Feed/EditLogComment" : "/Feed/EditFeedPost",
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

    ///summary: Allow other course users to like feed posts
    ///courtney-snyder
    self.LikeFeedPost = function () {
        $.ajax({
            url: "/Feed/IsPostLikedByUser", //Check if the post is already liked by the user
            data: { eventId: self.eventId, senderId: self.activeCourseUserId },
            dataType: "json",
            method: "GET",
            success: function (data) {
                //Note: boolResult is whether the user "liked" the post before the click or not
                LikePostSucceeded(self, !data.boolResult);
            }
        });
    }

    ///summary: Updates the DB that the item has been liked or unliked
    ///courtney-snyder
    function LikePostSucceeded(self, markHighlighted) {
        $.ajax({
            url: "/Feed/UpdatePostItemLikeCount",
            data: { eventId: self.eventId, senderId: self.activeCourseUserId },
            method: "GET"
        });
        //Display number of likes for that post
        GetNumberOfLikes(self, markHighlighted);
    }

    ///summary: Gets the number of likes for that post
    ///courtney-snyder
    function GetNumberOfLikes(self, markHighlighted) {
        $.ajax({
            url: "/Feed/GetPostLikeCount",
            data: { eventId: self.eventId },
            datatype: "json",
            method: "GET",
            success: function (result) {
                var numberOfLikes = 0;
                //Get the number of likes; if eventLogId is not in FeedPostLikes db, result will be null and like count is 0
                if (result.numberOfLikes != null) {
                    numberOfLikes = result.numberOfLikes;
                }
                //Update the highlight on glyphicon
                self.highlightMark(markHighlighted);

                var attributeId = "#feed-post-" + self.eventId + "-likes";
                var name = "#feed-post-" + self.eventId + "-likes";
                $(name).text("+ " + numberOfLikes);
            }
        });
    }

    ///summary: Allow the Original Poster to mark their post as [Resolved] and toggle between [Mark as Resolved] and [Resolved].
    ///courtney-snyder
    self.MarkPostResolved = function () {
        var isResolved;
        //Get the Resolved status of the post
        $.ajax({
            url: "/Feed/IsPostResolved",
            async: false,
            data: { eventId: self.eventId },
            dataType: "json",
            method: "GET",
            success: function (data) {
                //isResolved: The Resolved state of the post before the click
                isResolved = data.boolResult; //Boolean stored in a JSON object because you can't just return a Boolean (too easy)

                //Since the purpose of the click is to toggle the current status of the post, pass in the inverse of isResolved
                MarkResolvedSucceeded(self, !isResolved);
            }
        });
        //If the current value is false, switch to [Resolved] on click
        if (!isResolved) {
            $('#mark-as-resolved-' + self.eventId).val('Resolved');
            var icon = "<span style='color:green' class='glyphicon glyphicon-check'></span>" + '<b> [Resolved] </b>';
            //Clear text
            $('#mark-as-resolved-' + self.eventId).text("");
            //Add the cute lil check
            $('#mark-as-resolved-' + self.eventId).append(icon);
        }

            //And vice versa
        else {
            $('#mark-as-resolved-' + self.eventId).val('Unresolved');
            var icon = "<span class='glyphicon glyphicon-unchecked'></span>" + '<b> [Mark as Resolved] </b>';
            //Clear text
            $('#mark-as-resolved-' + self.eventId).text("");
            //Add the sad lonely box
            $('#mark-as-resolved-' + self.eventId).append(icon);
        }
    };

    ///summary: Updates the DB that the item's resolved status has been changed
    ///courtney-snyder
    function MarkResolvedSucceeded(item, resolved) {
        $.ajax({
            url: "/Feed/MarkResolvedPost", //Goes to MarkResolvedPost in FeedController
            data: { eventLogToMark: item.eventId, isResolved: resolved, markerId: item.activeCourseUserId }, //Input params for MarkResolvedPost
            method: "GET"
        });
    }

    self.AddComment = function () {
        if (self.isComment)
            return;

        //make anonymous post
        var makeAnonymous = $("[name='make_reply_anonymous_" + self.eventId + "']").is(':checked');

        // Get text from the text area
        var text = $('#feed-reply-textbox-' + self.eventId).val();

        // Make sure the user submitted something
        if (text == "") {
            return;
        }

        // Disable buttons and textbox so the user cannot "spam" replies
        $("#feed-reply-textbox-" + self.eventId).attr("disabled", "disabled");
        $("#feed-reply-submit-" + self.eventId).attr("disabled", "disabled");
        $("#feed-reply-cancel-" + self.eventId).attr("disabled", "disabled");

        text = replaceMentionWithId(text);

        $.ajax({
            url: "/Feed/PostComment",
            data: { id: self.eventId, content: text, isAnonymous: makeAnonymous },
            dataType: "json",
            method: "POST",
            success: function (dataList) {
                PostReplySucceeded(self, dataList);
            },
            error: function () {
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

function FeedItemOptions(canMail, canDelete, canEdit, showPicture, canVote, hideMail, eventVisibilityGroups, eventVisibleTo, isAnonymous, senderId) {
    var self = this;
    self.canMail = canMail;
    self.canDelete = canDelete;
    self.canEdit = canEdit;
    self.showPicture = showPicture;
    self.canVote = canVote;
    self.hideMail = hideMail;
    self.eventVisibilityGroups = eventVisibilityGroups;
    self.eventVisibleTo = eventVisibleTo;
    self.isAnonymous = isAnonymous;
    self.senderId = senderId;
}

function FeedViewModel(userName, userId, current) {
    var self = this;
    self.userName = userName;
    self.userId = userId;
    self.items = ko.observableArray();
    self.keywords = ko.observable("");

    var namesAndIds = [];
    $.ajax({
        url: "/Feed/GetProfileNames",
        method: "POST",
        async: false,
        success: function (data) {
            namesAndIds = data.userProfiles;
        }
    })

    self.namesAndIds = namesAndIds;
    //self.keywords.subscribe(function (newValue) {
    //    self.RequestUpdate();
    //});    

    // *** AUTO-UPDATE WEB SOCKET STUFF ***
    self.hub = $.connection.activityFeedHub;

    self.hub.client.notifyNewSuggestion = function (userProfileId) {
        //process new suggestion        
        if (userProfileId == self.userId) { //only update if this notification is relevant to the current user
            //TODO: we should do this by dynamically removing the no longer present and adding only the new ones...
            //clear the old suggestions
            $("#dashboard-items").empty();
            //populate the new suggestions
            PopulateSuggestionList();
            //notify the user
            TitlebarNotification("New Suggestions!");
            NotifyNewSuggestions();
        }
        //else do nothing
    };

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
            TitlebarNotification("New Feed Reply!");

            if (post.SenderId != self.userId)
                ShowNewActivityBadge();
        }
    };

    self.hub.client.addNewPost = function (courseID, postData) {
        //make sure the event is allowed by the filter
        var eventTypeCookie = $.cookie('FilterCookie');
        var cookieFilters = eventTypeCookie.split('&');
        var filterAllowsEvent = false;

        for (var i = 0; i < cookieFilters.length; i++) {
            var filters = cookieFilters[i].split('=');
            if (filters.length > 1 && filters[0] == postData.EventType) {
                if (filters[1] == "True") {
                    filterAllowsEvent = true;
                    break;
                }
            }
        }

        var courseId = GetSelectedCourseID();

        if (courseId == undefined) {
            courseId = $("#courseIdVal").val();
        }

        var privatePage = $("#private-page").val();

        var pushMessage = true;

        if (privatePage == "true" && postData.EventVisibilityGroups == "class") {
            //only forward messages to users on the private message page if it's not a class message
            pushMessage = false;
        }

        if (courseID == courseId && filterAllowsEvent && pushMessage) {
            if (postData.IsAnonymous) {
                postData.SenderId = postData.AnonSenderId;
                postData.ShowPicture = postData.AnonShowPicture;
                postData.HideMail = postData.AnonHideMail;
                postData.ActiveCourseUserId = postData.AnonActiveCourseUserId;
            }
            SetPermissions(postData);
            self.items.unshift(new FeedItem(postData)); // unshift puts object at beginning of array
            HighlightNewPost(postData.EventId, postData.SenderId == self.userId, self.InEventVisibleToList(postData.EventVisibleTo));
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

    var courseId = GetSelectedCourseID();

    if (courseId == undefined) {
        courseId = $("#courseIdVal").val();
    }

    // Start the connection
    $.connection.hub.qs = { "userID": self.userId, "courseID": courseId };
    $.connection.hub.start();
    // *************************************

    self.GetRole = function (id, role) {

        if (role == undefined || role == null) {
            var divId = "feed-item-" + id;
            $("#" + divId).find(".display_name").removeAttr("href");
            return "";
        }
        return " (" + role + ") ";
    }

    self.MakePost = function () {

        if (text == "")
            return;

        //get post visibility checkboxes 
        var postVisibility = $("#visibility-dropdown").val();
        var visibleTo = $('#custom-visibility-selection-id-list').val();

        var text = $("#feed-post-textbox").val();
        var emailToClass = $("[name='send_email']").is(':checked');

        //make anonymous post
        var makeAnonymous = $("[name='make_anonymous']").is(':checked');

        text = replaceMentionWithId(text);

        // Disable buttons while waiting for server response
        $('#feed-post-textbox').attr('disabled', 'disabled');
        $('#btn_post_active').attr('disabled', 'disabled');

        $.ajax({
            type: "POST",
            url: "/Feed/PostFeedItem",
            dataType: "json",
            data: { text: text, emailToClass: emailToClass, postVisibilityGroups: postVisibility, eventVisibleTo: visibleTo, isAnonymous: makeAnonymous },
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
            data: { endDate: lastDate, keywords: self.keywords(), events: GetCheckedEvents() },
            success: function (data) {
                var feedItems = [];
                $.each(data.Feed, function (index, value) {
                    var newFeedItem = new FeedItem(value);
                    var lastIndex = self.items().length - 1;
                    if (self.items()[lastIndex].eventId != newFeedItem.eventId) { //only push if it's not already on the feed
                        self.items.push(newFeedItem);
                        feedItems.push(newFeedItem);
                    }
                });
                //Load Resolved status on visible post
                $.ajax({
                    url: "/Feed/GetResolvedPostIds",
                    data: {},
                    datatype: "JSON",
                    success: function (data) {
                        //Get all the Resolved Post Ids
                        var idList = data.idList;
                        for (var i = 0; i < idList.length; i++) {
                            //Mark Feed Item as *checked box* Resolved
                            $('#mark-as-resolved-' + idList[i]).val("Resolved");
                            var icon = "<span style='color:green' class='glyphicon glyphicon-check'></span>" + ' [Resolved]';
                            $('#mark-as-resolved-' + idList[i]).text("");
                            $('#mark-as-resolved-' + idList[i]).append(icon);
                        }
                        //Display the number of likes for each of the loaded posts
                        LoadLikes(feedItems);
                    }
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
            complete: function () {
                HideMoreLoading();
            }
        });
    };

    self.RequestUpdate = function () {
        ShowLoading();
        var mappedItems;
        $.ajax({
            type: "GET",
            url: "/Feed/GetFeed",
            dataType: "json",
            data: { keywords: self.keywords(), events: GetCheckedEvents() },
            cache: false,
            success: function (data, textStatus, jqXHR) {
                mappedItems = $.map(data.Feed, function (item) { return new FeedItem(item) });
                self.items(mappedItems);

                if ($('#load-old-posts').hasClass('disabled')) {
                    $('#load-old-posts').removeClass('disabled');
                }
            },
            complete: function () {
                HideLoading();
                var mappedItemsEventIds = [];
                for (var i = 0; i < mappedItems.length; i++) {
                    mappedItemsEventIds.push(mappedItems[i].eventId);
                }
                //Load Resolved status on visible posts
                $.ajax({
                    url: "/Feed/GetSelectedResolvedPostIdsAndSenderIds",
                    data: { viewablePostIds: JSON.stringify(mappedItemsEventIds) },
                    datatype: "json",
                    success: function (data) {
                        for (var i = 0; i < mappedItems.length; i++) {
                            //if (mappedItems[i].eventType == "MarkHelpfulGivenEvent")
                            //alert("eventType: " + mappedItems[i].eventType + " senderId: " + mappedItems[i].senderId);
                        }
                        //Get all the Resolved Post Ids and Sender Ids
                        var dictionary = data.resolvedString;
                        if (dictionary != "") {
                            //Split the string on commas
                            var dictionarySplit = dictionary.split(',');
                            for (var i = 0; i < dictionarySplit.length; i++) {
                                var elementSplit = dictionarySplit[i].split(':');
                                eventId = elementSplit[0];
                                senderId = elementSplit[1];
                                //If the post is resolved and the current user was the poster, update the anchor link
                                if (senderId == self.userId) {
                                    $('#mark-as-resolved-' + eventId).val('Resolved');
                                    $('#mark-as-resolved-' + eventId).text("");
                                    $('#mark-as-resolved-' + eventId).append("<span style='color:green' class='glyphicon glyphicon-check'></span> <b> [Resolved] </b>");
                                }
                                    //Otherwise, display text
                                else {
                                    $('#mark-as-resolved-' + eventId).val('Resolved');
                                    var icon = "<span style='color:green' class='glyphicon glyphicon-check'></span> <b> Resolved </b>";
                                    $('#mark-as-resolved-' + eventId).text("");
                                    $('#mark-as-resolved-' + eventId).append(icon);
                                }
                            }
                        }
                        //Display the number of likes for each of the loaded posts
                        LoadLikes(mappedItems);
                    },
                    error: function (data) {

                    }
                });
            }
        });
    };

    ///summary: Gets the number of likes for each post on page load/refresh.
    ///courtney-snyder
    function LoadLikes(feedItemList) {
        //Get feed ID from each feed item
        var feedEventIds = [];
        //for (var i = 0; i < feedItemList.length; i++)
        for (var i = 0; i < Object.keys(feedItemList).length; i++) {
            feedEventIds.push(feedItemList[i].eventId);
        }

        //Get number of likes for each post
        $.ajax({
            url: "/Feed/GetPostLikeCounts",
            data: { eventIds: JSON.stringify(feedEventIds) },
            method: "GET",
            success: function (result) {
                var eventId = 0;
                var numberOfLikes = 0;
                //Note: result.likeString is a dictionary that was turned into a string (was having JSON issues)
                var dictionary = result.likeString;
                //Split the string on commas
                var dictionarySplit = dictionary.split(',');
                for (var i = 0; i < feedEventIds.length; i++) {
                    var elementSplit = dictionarySplit[i].split(':');
                    eventId = elementSplit[0];
                    numberOfLikes = elementSplit[1];
                    var name = "#feed-post-" + eventId + "-likes";
                    $(name).text("+ " + numberOfLikes);
                }
                ArePostsLiked(feedItemList);
            }
        });
    }

    ///summary: Highlights the thumb if the current user liked that post.
    ///courtney-snyder
    function IsPostLiked(feedItem) {
        $.ajax({
            type: "GET",
            url: "/Feed/IsPostLikedByUser",
            data: { eventId: feedItem.eventId, senderId: self.userId },
            dataType: "json",
            success: function (data) {
                //If the poster and current user are the same person, hide the thumb
                if (feedItem.senderId == self.userId) {
                    var name = "#feed-post-" + eventId + "-thumb";
                    $(name).text("");
                    alert("Hide " + name + " thumb")
                }
                    //If the user liked the post at some point before page load, highlight thumb
                else if (data.boolResult) {
                    feedItem.highlightMark(true);
                }
            }
        });
    }

    ///summary: Highlights the thumbs of the visible posts on the Feed if the user liked them. Also removes thumbs from 
    ///         user's own posts.
    ///courtney-snyder
    function ArePostsLiked(feedItemList) {
        var feedEventIds = [];
        for (var i = 0; i < Object.keys(feedItemList).length; i++) {
            feedEventIds.push(feedItemList[i].eventId);
        }
        $.ajax({
            type: "GET",
            url: "/Feed/ArePostsLikedByUser",
            data: { eventIds: JSON.stringify(feedEventIds), senderId: self.userId },
            //dataType: "json",
            success: function (data) {
                //If the user liked the post at some point before page load, highlight thumb
                //Note: data.likeString is an int (event ID) list that was turned into a string (was having JSON issues)
                var list = data.likeString;
                //Split the string on commas
                var listSplit = list.split(',');
                //If there are no posts liked by the user, do not enter the first for loop
                if (listSplit[0] == [])
                    listSplit.length = 0;
                for (var i = 0; i < listSplit.length; i++) {
                    //Get each Event ID as a string
                    var elementAsString = listSplit[i];
                    //Parse to int
                    var elementAsInt = parseInt(elementAsString);
                    //Get the event from the feedItemList (IMPORTANT because you need the same object reference; highlight
                    //is in a knockout data-bind
                    var result = feedItemList.filter(function (obj) {
                        return obj.eventId == elementAsInt ? obj : null;
                    });
                    //result is a single element list since each element in the feedItemList has a unique eventId
                    if (result != null && result != []) {
                        result[0].highlightMark(true);
                    }
                }

                //Remove thumbs from all posts made by the current user (Bob Smith cannot like his own post)
                for (var i = 0; i < feedItemList.length; i++) {
                    if (feedItemList[i].senderId == self.userId) {
                        var name = '#feed-post-' + feedItemList[i].eventId + '-thumb';
                        $(name).removeClass();
                    }
                }
            }
        });
    }

    self.ShowHashTagResults = function (hashtag) {
        ShowLoading();
        $.ajax({
            type: "GET",
            url: "/Feed/GetFeed",
            dataType: "json",
            data: { keywords: hashtag, events: "1,7,9" }, // 1, 7, 9 are the Ids for AskForHelp, FeedPost, and LogComment events
            cache: false,
            success: function (data, textStatus, jqXHR) {
                var mappedItems = $.map(data.Feed, function (item) { return new FeedItem(item) });
                self.items(mappedItems);

                if ($('#load-old-posts').hasClass('disabled')) {
                    $('#load-old-posts').removeClass('disabled');
                }
            },
            complete: function () {
                HideLoading();
                // If the hashtag is within the replies of this post, we want to highlight the "View Replies" Link
                $("[id^=feed-item-content]").each(function () {
                    var fullId = $(this).attr("id");
                    var postId = fullId.split("-").pop();
                    var postText = this.innerText;
                    if (!postText.includes(hashtag)) {
                        $("#expand-comments-" + postId).addClass("Hashtag");
                    }
                });
            }
        });
    };

    self.GetUnansweredPosts = function (unansweredPostIds) {

        ShowLoading();

        $.ajax({
            type: "GET",
            url: "/Feed/GetAggregateFeedFromIDs",
            dataType: "json",
            data: { stringIds: unansweredPostIds },
            cache: false,
            success: function (data, textStatus, jqXHR) {
                var mappedItems = $.map(data.Feed, function (item) { return new FeedItem(item) });
                self.items(mappedItems);
                self.items.reverse();

                if ($('#load-old-posts').hasClass('disabled')) {
                    $('#load-old-posts').removeClass('disabled');
                }
            },
            complete: function () {
                HideLoading();
            },
            error: function (data, textStatus, jqXHR) {
            }
        });
    };

    self.ReverseFeedSortOrder = function () {
        self.items.reverse();
    };

    self.GetPost = function (id) {
        var post = null;
        $.each(self.items(), function (index, value) {
            if (value.eventId == id)
                post = value;
        });
        return post;
    };

    self.InEventVisibleToList = function (eventVisibleToList) {

        //case: legacy or open to all viewers. we get a "" list in the case where the value is null 
        //or we purposly saved a list as "" so visibility is for everyone
        if (eventVisibleToList == "" || eventVisibleToList === undefined) {
            return true;
        }
        //if the list is not empty, make sure the user id is in the list.
        var idList = eventVisibleToList.split(",");

        for (var i = 0; i < idList.length; i++) {
            if (idList[i] == self.userId) {
                return true;
            }
        }
        //if the list is not "" and we did not find an id match, hide the post/reply
        return false;
    };

    self.EventVisibilityGroupsPresent = function (visibilityGroups) {
        if (null != visibilityGroups && visibilityGroups.length > 0) {
            var groups = visibilityGroups.split(',');
            if (groups.length == 1) {
                if (groups[0] == "class") { //default visiblity is class, if this is the only group then don't show the visibility icons
                    return false;
                }
            }
            return true;
        }
        return false;
    };

    self.EventVisibilityGroups = function (visibilityGroups) {
        if (self.EventVisibilityGroupsPresent(visibilityGroups)) {
            var groups = visibilityGroups.split(',');
            var formattedGroupTitles = "";

            for (var i = 0; i < groups.length; i++) {
                if (groups[i] == "class") {
                    //do nothing
                }
                else if (groups[i] == "tas") {
                    formattedGroupTitles = formattedGroupTitles + groups[i].toUpperCase().substring(0, 2) + "s ";
                }
                else if (groups[i] == "instructors") {
                    formattedGroupTitles = formattedGroupTitles + groups[i].toUpperCase().substring(0, groups[i].length) + " ";
                }
                else {
                    formattedGroupTitles = formattedGroupTitles + groups[i].toUpperCase().substring(0, groups[i].length) + " ";
                }
            }
            return formattedGroupTitles.trim();
        }
        return "EVERYONE";
    };

    $("#activity-feed-filters").submit(function (e) {

        if ($("#enableLogging").val() == "true") {
            var checkedEventsList = "";
            $(".event_checkbox").each(function () {
                if (this.checked) {
                    checkedEventsList += this.name + " ";
                }
            });

            LogActivityEvent("KeywordSearch", $("#feedSearchInput").val(), "Keywords for FilterOptions: " + checkedEventsList);
        }

        self.RequestUpdate();
        return false; // prevent page refresh
    });
}

function DetailsViewModel(userName, userId, rootId) {
    var self = this;
    self.userName = userName;
    self.userId = userId;
    self.rootId = rootId;
    self.items = ko.observableArray([]);

    var namesAndIds = [];
    $.ajax({
        url: "/Feed/GetProfileNames",
        method: "POST",
        async: false,
        success: function (data) {
            namesAndIds = data.userProfiles;
        }
    })

    self.namesAndIds = namesAndIds;

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
                self.senderId = data.Item.SenderId;
            },
            complete:
                //Load Resolved status on the post
                $.ajax({
                    url: "/Feed/IsPostResolved",
                    data: { eventId: self.rootId },
                    datatype: "JSON",
                    success: function (data) {
                        //If post is resolved
                        if (data.boolResult) {
                            //Mark Feed Item as *checked box* Resolved
                            $('#mark-as-resolved-' + self.rootId).val("Resolved");
                            var icon = "<span style='color:green' class='glyphicon glyphicon-check'></span>" + ' [Resolved]';
                            $('#mark-as-resolved-' + self.rootId).text("");
                            $('#mark-as-resolved-' + self.rootId).append(icon);
                        }
                        //Display the number of likes for the post
                        LoadLikes(self.rootId, self.senderId);
                    }
                })
        });
    };

    ///summary: Gets the number of likes for that post on page load/refresh (repeated because this is in the DetailsViewModel).
    ///         and highlights the thumb if the post has already been liked by the current viewer OR removes the thumb if the
    ///         user is the poster
    ///courtney-snyder
    function LoadLikes(eventId, senderId) {
        IsPostLiked(eventId);
        GetPostLikeCount(eventId);
        if (senderId == self.userId) {
            var name = '#feed-post-' + eventId + '-thumb';
            $(name).removeClass();
        }
    }

    ///summary: Highlights the thumb if the current user liked that post.
    ///courtney-snyder
    function IsPostLiked(eventId) {
        $.ajax({
            type: "GET",
            url: "/Feed/IsPostLikedByUser",
            data: { eventId: eventId, senderId: self.userId },
            dataType: "json",
            success: function (data) {
                //If the user liked the post at some point before page load, highlight thumb
                if (data.boolResult) {
                    self.items()[0].highlightMark(true);
                }
            }
        });
    }

    ///summary: Gets the post like count for the specified post
    ///courtney-snyder
    function GetPostLikeCount(eventId) {
        $.ajax({
            url: "/Feed/GetPostLikeCount",
            data: { eventId: eventId },
            datatype: "json",
            method: "GET",
            success: function (result) {
                var numberOfLikes = 0;
                if (result.numberOfLikes != null) {
                    numberOfLikes = result.numberOfLikes;
                }
                var name = "#feed-post-" + eventId + "-likes";
                $(name).text("+ " + numberOfLikes);
            }
        });
    }

    ///summary: If user likes/unlikes a post in the Details view, the action persists on the Feed
    ///courtney-snyder
    function UpdatePostItemLikeCount(eventId) {
        $.ajax({
            url: "/Feed/UpdatePostItemLikeCount",
            data: { eventId: eventId, senderId: self.userId },
            method: "GET"
        });
        GetPostLikeCount(eventId);
    }

    self.GetRole = function (id, role) {

        if (role == undefined || role == null) {
            var divId = "feed-item-" + id;
            $("#" + divId).find(".display_name").removeAttr("href");
            return "";
        }
        return " (" + role + ") ";
    }

    self.InEventVisibleToList = function (eventVisibleToList) {

        //case: legacy or open to all viewers. we get a "" list in the case where the value is null 
        //or we purposly saved a list as "" so visibility is for everyone
        if (eventVisibleToList == "") {
            return true;
        }
        //if the list is not empty, make sure the user id is in the list.
        var idList = eventVisibleToList.split(",");

        for (var i = 0; i < idList.length; i++) {
            if (idList[i] == self.userId) {
                return true;
            }
        }
        //if the list is not "" and we did not find an id match, hide the post/reply
        return false;
    };

    self.EventVisibilityGroupsPresent = function (visibilityGroups) {
        if (null != visibilityGroups && visibilityGroups.length > 0) {
            var groups = visibilityGroups.split(',');
            if (groups.length == 1) {
                if (groups[0] == "class") { //default visiblity is class, if this is the only group then don't show the visibility icons
                    return false;
                }
            }
            return true;
        }
        return false;
    };

    self.EventVisibilityGroups = function (visibilityGroups) {
        if (self.EventVisibilityGroupsPresent(visibilityGroups)) {
            var groups = visibilityGroups.split(',');
            var formattedGroupTitles = "";

            for (var i = 0; i < groups.length; i++) {
                if (groups[i] == "class") {
                    //do nothing
                }
                else if (groups[i] == "tas") {
                    formattedGroupTitles = formattedGroupTitles + groups[i].toUpperCase().substring(0, 2) + "s ";
                }
                else if (groups[i] == "instructors") {
                    formattedGroupTitles = formattedGroupTitles + groups[i].toUpperCase().substring(0, groups[i].length) + " ";
                }
                else {
                    formattedGroupTitles = formattedGroupTitles + groups[i].toUpperCase().substring(0, groups[i].length) + " ";
                }
            }
            return formattedGroupTitles.trim();
        }
        return "EVERYONE";
    };
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

function SetPermissions(post) {
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
            post.HideMail = data.hideMail;
            post.EventVisibilityGroups = data.eventVisibilityGroups;
            post.EventVisibleTo = data.eventVisibleTo;
            post.IsAnonymous = data.isAnonymous;
        },
        error: function () {
            post.CanDelete = false;
            post.CanEdit = false;
            post.CanMail = false;
            post.CanVote = false;
            post.ShowPicture = false;
            post.HideMail = false;
            post.EventVisibilityGroups = "";
            post.EventVisibleTo = "";
            post.IsAnonymous = false;
        }
    });
    if (post.IsAnonymous) {
        post.SenderId = 0;
    }
}

function CheckEvents(events) {
    var elist = events.split(',');
    $.each(elist, function (index, value) {
        $("#event_" + value).prop("checked", true);
    });
}

function GetCheckedEvents() {
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

function IsEventTypeChecked(eventType) {
    $('.event_checkbox').each(function (index, value) {
        if (value.checked == true && value.data('type') == eventType) {
            return true;
        }
    });
    return false;
}

function onDeleteSuccess(item) {
    $('#feed-item-' + item.eventId).hide('blind', {}, 'slow', function () { $(this).remove(); });
}

function ShowEditBox(item) {
    // populate the box with the correct content
    var text = GetFeedPostContent(item.eventId, item.parentEventId); //getting the content from the database because the text box does not contain autolinked items

    if (text === "false") { //we failed to get the user text from the database so just grab the text that is there already...
        text = $("#content-comment-" + item.eventId).text();
    }

    $('#feed-edit-textbox-' + item.eventId).val(text);

    $('#feed-edit-' + item.eventId).show('fade');
    $('#feed-item-content-' + item.eventId).hide();
    $('#btn-edit-' + item.eventId).hide('highlight');
}

function HideEditBox(item) {
    $('#feed-edit-' + item.eventId).hide();
    $('#feed-item-content-' + item.eventId).show('fade');
    $('#btn-edit-' + item.eventId).show('highlight');
}

function ShowReplyBox(item) {
    $("#btn-reply-" + item.eventId).hide();
    $("#feed-reply-" + item.eventId).show('blind');
    $("#feed-reply-textbox-" + item.eventId).val('');

    if ($("#enableLogging").val() == "true") {
        LogActivityEvent("InitiateReply", item.eventId, "EventLogId");
    }
}

function HideReplyBox(item) {
    $("#btn-reply-" + item.eventId).show('highlight');
    $("#feed-reply-" + item.eventId).hide('blind');
}

function AreRepliesExpanded(itemID) {
    return $("#feed-item-comments-" + itemID).css('display') != 'none';
}

function expandComments(item) {
    var commentsTextSpan = "#expand-comments-text-" + item.eventId;
    var replies = "#feed-item-comments-" + item.eventId;
    var post = "#feed-item-" + item.eventId;

    if ($(replies).css('display') == 'none') {
        $(replies).show('blind');
        $(commentsTextSpan).text("Hide");
        $(post).removeClass("new-replies");

        if ($("#enableLogging").val() == "true") {
            LogActivityEvent("ExpandCommentsClick", item.eventId, "EventLogId");
        }
    }
    else {
        var height = (window.innerHeight > 0) ? window.innerHeight : screen.height;

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

function HighlightNewPost(postID, isCurrentUserPost, userInVisibilityList) {
    // Show a nifty animation for the new post
    $('#feed-item-' + postID).hide().show('easeInBounce');

    if (!isCurrentUserPost && userInVisibilityList) {
        ShowNewActivityBadge();
        ShowNewPostBadge(postID);
        TitlebarNotification("New Feed Post!");
    }
}

function MakePostSucceeded(newPost) {


    //on private messages page
    var privatePage = $("#private-page").val();
    if (privatePage != undefined) {
        $('#visibility-dropdown').prop('selectedIndex', 1); //change it to My Section default
        $("#btn_post_active").val("Post to: " + $("#visibility-dropdown option:selected").text());
        $("#email_post").text("Email to: My Section");
    }
    else {
        //reset the post visibility to everyone
        $('#visibility-dropdown').prop('selectedIndex', 0);
        $("#btn_post_active").val("Post to " + $("#data-course-link").text());
        $("#email_post").text("Email to Class");
    }
    toggleVisibilityTooltip($("#visibility-dropdown option:selected").val());

    //hide custom visibility
    $('#custom-visiblity-selection').css('max-height', '0px');
    //clear last user selection
    clearSelection();

    // Clear the textbox
    $('#feed-post-textbox').val('');

    // re-enable posting
    $('#feed-post-textbox').removeAttr('disabled');
    $('#btn_post_active').removeAttr('disabled');

    newPost = ConfigureAnonymousSettings(newPost);

    // notify others about the new post
    //last THREE parameters are not used by posts here, just VS Plugin events... apparently hub doesn't like optional parameters    
    vm.hub.server.notifyNewPost(newPost, "", 0, "");
}

function ConfigureAnonymousSettings(post) {
    if (post.IsAnonymous) {
        post.SenderId = 0;
        post.ActiveCourseUserId = 0;
        post.Role = "User";
        post.AnonSenderId = 0;
        post.AnonShowPicture = false;
        post.AnonHideMail = true;
        post.AnonActiveCourseUserId = 0;
    }
    return post;
}

function MakePostFailed() {
    if ($("#visibility-dropdown option:selected").text() == "Selected Users...") {
        if (0 == updateHiddenUserIdList()) {
            ShowError('#feed-post-form', 'Unable to create post. No users have been selected for this post. Please select users or change post visiblity.', false);
        }
        else {
            ShowError('#feed-post-form', 'Unable to create post. Post textbox contains no text.', false);
        }
    }
    else {
        ShowError('#feed-post-form', 'Unable to create post. Post textbox contains no text.', false);
    }

    // re-enable posting
    $('#feed-post-textbox').removeAttr('disabled');
    $('#btn_post_active').removeAttr('disabled');
}

function HighlightNewReply(postID) {
    var replies = $("#feed-item-comments-" + postID);
    replies.children().last().hide().show('easeInBounce');

    if (!AreRepliesExpanded(postID)) {
        $("#feed-item-" + postID).addClass("new-replies");
    }
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

    //clean up anon reply details before pushing to the hub
    for (var i = 0; i < dataList.length; i++) {
        if (dataList[i].SenderName.indexOf("Anonymous") != -1) { //match
            dataList[i].SenderName = "Anonymous User";
            dataList[i].SenderId = 0;
            dataList[i].ActiveCourseUserId = 0;
        }
    }

    // notify everyone that a reply was made
    vm.hub.server.notifyNewReply(item.eventId, dataList);

    // re-enabled buttons and textbox
    $("#feed-reply-textbox-" + item.eventId).removeAttr("disabled");
    $("#feed-reply-submit-" + item.eventId).removeAttr("disabled");
    $("#feed-reply-cancel-" + item.eventId).removeAttr("disabled");
}

function PostReplyFailed(item) {
    // Set the error message text and display it for 4 seconds
    ShowError('#feed-reply-' + item.eventId, 'Unable to submit reply. Check internet connection.', true);
}

function EditSucceeded(item, content, timeString) {
    //notify everyone that an edit was made
    if (item.isComment) {
        vm.hub.server.notifyEditReply(item.parentEventId, item.eventId, content, timeString);
    }
    else {
        vm.hub.server.notifyEditPost(item.eventId, content, timeString);
    }

    // clear textbox
    $("#feed-edit-textbox-" + item.eventId).val('');

    // hide the edit form & display regular text
    HideEditBox(item);
}

function EditFailed(item) {
    // Set the error message text and display it for 4 seconds
    ShowError('#feed-edit-' + item.eventId, 'Unable to edit post. Check internet connection.', true);
}

function MarkHelpfulSucceeded(item, numMarks) {
    vm.hub.server.notifyAddMarkHelpful(item.parentEventId, item.eventId, numMarks);
}

function LoadOldPosts() {

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

function ShowError(containerID, text, insertAbove) {
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

function ShowLoading() {
    if ($('#loadingMsg').css('display') != 'none')
        return;

    $('#loadingMsg').show('fade');
    $('#load-old-posts').hide();
}

function HideLoading() {
    if ($('#loadingMsg').css('display') == 'none')
        return;

    $('#loadingMsg').hide('fade');
    $('#load-old-posts').show();
}

function ShowMoreLoading() {
    $('#loadingMoreMsg').show('fade');
    $('#load-old-posts').hide();
}

function HideMoreLoading() {
    $('#loadingMoreMsg').hide();
    $('#load-old-posts').show();
}

function ShowNewActivityBadge() {
    //$("#dashboard_middle").addClass("new-activity");
    $(".new-activity-badge").show();
}

function HideNewActivityBadge() {
    //$("#dashboard_middle").removeClass("new-activity");
    $(".new-activity-badge").hide();
}

function ShowNewPostBadge(postID) {
    $('#feed-item-' + postID + ' .new-post-badge').show();
}

function HideNewPostBadge(postID) {
    $('#feed-item-' + postID + ' .new-post-badge').hide();
}

//Summary: Gets if a post is marked as resolved by the OP or not
//courtney-snyder
function IsMarkedResolved(eventID) {
    var isResolved;
    //Get the Resolved status of the post
    $.ajax({
        url: "/Feed/IsPostResolved",
        async: false,
        data: { eventId: eventID },
        dataType: "json",
        method: "GET",
        success: function (data) {
            isResolved = data.boolResult; //Boolean stored in a JSON object because you can't just return a Boolean (too easy)
        }
    });
    if (isResolved) {
        $('#feed-item-' + eventID).attr(('mark-as-resolved-' + eventID), "Resolved");
    }
    else {
        $('#feed-item-' + eventID).attr(('mark-as-resolved-' + eventID), "Unresolved");
    }
    return isResolved;
}

function ShowUserVisibilityDialog(item) {
    var namesAndIds = [];
    $.ajax({
        url: "/Feed/GetProfileNames",
        method: "POST",
        async: false,
        success: function (data) {
            namesAndIds = data.userProfiles;
        }
    })

    var visibleListIds = item.options.eventVisibleTo.split(',');
    var userNames = new Array();
    for (var i = 0; i < visibleListIds.length; i++) {
        userNames.push([namesAndIds[visibleListIds[i]], visibleListIds[i]]);
    }

    //sort by firstname
    userNames = userNames.sort(function (a, b) {
        return a[0] > b[0];
    });

    //clear the previous instance of the dialog box
    $('#visibility-dialog').empty();

    //populate the dialog box
    $("#visibility-dialog").append("<ul class=\"custom-bullet-participants\" id='visibilityList'></ul>" +
                                    "<input type=\"hidden\" id=\"selected-event-id\" value=\"" + item.eventId + "\"/>" +
                                    "<input type=\"hidden\" id=\"selected-sender-id\" value=\"" + item.senderId + "\"/>");

    //enable removing users
    if ($("#current-user-id").val() == $("#selected-sender-id").val())
        $("#visibility-dialog").append("<input type=\"hidden\" id=\"is-op\" value=\"true\"/>");
    else
        $("#visibility-dialog").append("<input type=\"hidden\" id=\"is-op\" value=\"false\"/>");

    if ($("#is-op").val() === "true" || ($("#can-grade").length > 0 && $("#can-grade").val() == "true")) {
        for (var i = 0; i < userNames.length; i++) {
            $("#visibilityList").append("<li class=\"removable-user\" " + " id=\"participant-profile-id-" + userNames[i][1] + "\"> <span class=\"glyphicon glyphicon-remove remove-current\"></span>  <span class=\"glyphicon glyphicon-trash remove-user\"></span> <img class=\"mini-profile-image\" src=\"/user/" + userNames[i][1] + "/Picture?size=16\" alt=\"Profile Image for " + userNames[i][0] + "\" >" + userNames[i][0] + "</li>");
        }
    }
    else {
        for (var i = 0; i < userNames.length; i++) {
            $("#visibilityList").append("<li " + " id=\"participant-profile-id-" + userNames[i][1] + "\"> <img class=\"mini-profile-image\" src=\"/user/" + userNames[i][1] + "/Picture?size=16\" alt=\"Profile Image for " + userNames[i][0] + "\" >" + userNames[i][0] + "</li>");
        }
    }

    //add more members to the dialog.    
    $.ajax({
        url: "/Feed/GetPostVisibilityAddMorePartialView/",
        data: {},
        success: function (response) {
            $(response).insertAfter("#visibilityList");

        },
        error: function () {
            //todo: handle error here
        }
    });

    $("#visibility-dialog").dialog(); //now show the dialog box
}

function TitlebarNotification(message) {
    $.titleAlert(message, {
        interval: 750,
        duration: 15000,
        stopOnFocus: true
    });
}

function LogActivityEvent(eventAction, eventData, eventDataDescription) {
    var authKey = $.cookie('AuthKey').split("=")[1];
    var courseId = $("#data-course-link").attr("data-course-id");

    $.ajax({
        url: "/Feed/LogActivityEvent",
        data: { authToken: authKey, eventAction: eventAction, eventData: eventData, eventDataDescription: eventDataDescription, courseId: courseId },
        type: "POST",
        success: function (dataList) {
            //
        },
        error: function () {
            //
        }
    });
}

function LogDetailsActivity(item) {
    if ($("#enableLogging").val() == "true") {
        LogActivityEvent("ViewDetails", item.eventId, "EventLogId");
    }
    return true;
}

//feed methods for custom visibility groups
function toggleCustomGroups(groupVisibility) {
    if (groupVisibility == 'hide') {
        $('#custom-visiblity-selection').css('max-height', '0px');
    }
    else {
        $('#custom-visiblity-selection').css('max-height', '1000px');
    }
}

function clearSelection() {

    $('div').remove('.recipient');
    updateHiddenUserIdList();
    $('#custom-search-clear-selection').css('opacity', '0.65');
    $('#custom-search-clear-selection').css('cursor', 'not-allowed');
    $('#custom-visibility-selection-id-list').val($('#current-user-id').val());
    $('#NoUsers').css('display', 'inline');
}

function removeUser(id) {
    $("#selected-user-id-" + id).remove();
    var usersCount = updateHiddenUserIdList();

    if (usersCount == 0) {
        $('#custom-search-clear-selection').css('opacity', '0.65');
        $('#custom-search-clear-selection').css('cursor', 'not-allowed');
        $('#NoUsers').css('display', 'inline');
    }
}

function updateHiddenUserIdList() {
    var usersCount = 0;
    var selectedIds = $('#current-user-id').val();
    $(".recipient").each(function () {
        var id = $(this).attr('id').split('-');
        selectedIds += "," + id[id.length - 1];
        usersCount++;
    });
    $('#custom-visibility-selection-id-list').val(selectedIds);
    return usersCount;
}

function clearFilter() {
    $('#custom-user-search-input').val('');
    $('#custom-search-clear').css('opacity', '0.65');
    $('#custom-search-clear').css('cursor', 'not-allowed');
    $('#custom-search-clear').css('padding', '2px 5px 2px 5px');
    $('.icon-state').css('color', '#aaa');
}

function buildTitleOutput(eventVisibleTo, namesAndIds) {

    if (eventVisibleTo.length == 0) {
        return "";
    }

    var visibleListIds = eventVisibleTo.split(',');
    var userNames = new Array();
    for (var i = 0; i < visibleListIds.length; i++) {
        userNames.push([namesAndIds[visibleListIds[i]], visibleListIds[i]]);
    }

    //sort by firstname
    userNames = userNames.sort(function (a, b) {
        return a[0] > b[0];
    });

    var formattedOutput = "";

    for (var i = 0; i < userNames.length; i++) {
        if (i == 0) {
            formattedOutput = " - " + userNames[i][0];
        }
        else {
            formattedOutput += ", " + userNames[i][0];
        }
    }
    return formattedOutput;
}

function detectBrowser() {
    // Opera 8.0+
    var isOpera = (!!window.opr && !!opr.addons) || !!window.opera || navigator.userAgent.indexOf(' OPR/') >= 0;
    // Firefox 1.0+
    var isFirefox = typeof InstallTrigger !== 'undefined';
    // Safari <= 9 "[object HTMLElementConstructor]" 
    var isSafari = Object.prototype.toString.call(window.HTMLElement).indexOf('Constructor') > 0;
    // Internet Explorer 6-11
    var isIE = /*@cc_on!@*/false || !!document.documentMode;
    // Edge 20+
    var isEdge = !isIE && !!window.StyleMedia;
    // Chrome 1+
    var isChrome = !!window.chrome && !!window.chrome.webstore;
    // Blink engine detection
    var isBlink = (isChrome || isOpera) && !!window.CSS;
    //alert("opera: " + isOpera + " firefox: " + isFirefox + " safari: " + isSafari + " IE: " + isIE + " edge: " + isEdge + " chrome: " + isChrome + " blink: " + isBlink);
    //$("#test").append("opera: " + isOpera + " firefox: " + isFirefox + " safari: " + isSafari + " IE: " + isIE + " edge: " + isEdge + " chrome: " + isChrome + " blink: " + isBlink);
    return isOpera || isFirefox || isSafari || isIE || isEdge || isChrome || isBlink;
}

//END feed methods for custom visibility groups

function GetPreviousUrl() {
    return localStorage.getItem('prevUrl');
}

function ShowBackToSuggestions() {
    if ($("#suggestion-back-link").length) { //if the link exists
        $("#suggestion-back-link").attr('href', GetPreviousUrl());
        $("#suggestion-back-link").css('display', 'inline');
    }
}

function replaceMentionWithId(text) {
    var userNames = localStorage['UserNames'].split(',');
    var userIds = localStorage['UserIds'].split(',');

    // TODO: to boost performance, could add functionality to know which names were used (if any)
    // Replace all occurrences of @user with @id;
    for (i = 0; i < userNames.length; i++) {
        var name = userNames[i], id = "@id=" + userIds[i] + ";"; //Must include @id or else the id will not be replaced with a hyperlink to the user's profile but will reveal the id #
        var atMention = "@" + name; //MUST HAVE THIS or else ID numbers are revealed when someone does #Name (e.g. #AdMin -> #id=2)
        text = text.replace(atMention, id);
    }
    return text;
}

function GetFeedPostContent(eventId, parentEventId) {
    var text = "false";
    $.ajax({
        url: "/Feed/GetFeedPostContent",
        method: "POST",
        data: { eventId: eventId, parentEventId: parentEventId },
        async: false,
        success: function (result) {
            text = result;
        },
        error: function (result) {
            //do nothing
        }
    });
    return text;
}

//summary: Returns the current user's Course Role.
//courtney-snyder
function getUserRole() {
    var currentRole = "";
    $.ajax({
        url: "/Feed/GetUserRole", //Goes to FeedController, then GetUserRole method within that file
        method: "POST", //HTTPPOST Tag in FeedController
        async: false,
        success: function (result) { //GetUserRole returns the user role as a string
            currentRole = result;
        },
        error: function (result) {
            currentRole = "Observer"; //If an error occurs, don't let the viewer see the @mentions
        }
    })
    return currentRole;
}

///summary: Used in _FeedItems databind to check if current user and event poster are the same user
///courtney-snyder
function IsSelf(postSenderId, currentUserId, eventId) {
    var isSelf = null;
    //If the post is Anon, get the poster ID from the DB and compare with current user ID
    if (postSenderId == 0) {
        var anonSenderId;
        $.ajax({
            url: "/Feed/GetFeedItemSenderId",
            data: { eventID: parseInt(eventId) },
            datatype: "json",
            method: "GET",
            success: function (result) {
                anonSenderId = result.senderId;
                //alert("result.senderId: " + result.senderId + " anonSenderId: " + anonSenderId);
                if (anonSenderId == currentUserId) {
                    isSelf = true;
                }
            },
            error: function (result) {
            }
        });
    }
        //Otherwise, just compared the post's sender ID and current user's sender ID
    else {
        if (postSenderId == currentUserId) {
            isSelf = true;
        }
    }
    return isSelf;
}