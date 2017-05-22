
//Downloaded from: https://github.com/bryanwoods/autolink-js
//linkUsernames adjusted by Courtney Snyder (May 2017) to add anonymity to the feed when viewed by Observers.
//getUserRole written by Courtney Snyder (May 2017).

(function () {
    var autoLink, linkUsernames, linkHashtags, embedYouTube, formatCode, embedImages,
      __slice = [].slice;
    var courseUserNames = getUserNames();
    var currentUser = getUserRole(); //Gets current user's AbstractCourseRole
    var isObserver = false;
    if (currentUser == "Observer") {
        isObserver = true;
    }

    //NOTE: ?component=7 is added to links to force opening in the main window in the VS Plugin, it has no effect in-browser
    autoLink = function () {
        var k, linkAttributes, option, options, pattern, v, text;
        options = 1 <= arguments.length ? __slice.call(arguments, 0) : [];

        text = this;

        //embed any images in the text
        text = embedImages(text);

        if (detectBrowser()) { //only do this if we can detect the browser... this returns false from the VS Plugin and we want to prevent doing this as it breaks the feed on the plugin
            text = embedYouTube(text);
        }

        //format any code, but only if the page is not on the embedded feed (it breaks the feed for some reason...)
        var re = /\/feed\/osbide/g;
        var isMatch = false;
        while ((match = re.exec($(location).attr('href').toLowerCase())) != null) {
            isMatch = true;
        }

        if (!isMatch) {
            text = formatCode(text);
        }

        pattern = /(^|[\s\n>]|<br\/?>)((?:https?|ftp):\/\/[\-A-Z0-9+\u0026\u2019@#\/%?=()~_|!:,.;]*[\-A-Z0-9+\u0026@#\/%=~()_|])/gi;
        if (!(options.length > 0)) {
            return text.replace(pattern, "$1<a href='$2?component=7'>$2</a>").linkUsernames().linkHashtags();
        }
        option = options[0];
        linkAttributes = ((function () {
            var _results;
            _results = [];
            for (k in option) {
                v = option[k];
                if (k !== 'callback') {
                    _results.push(" " + k + "='" + v + "'");
                }
            }
            return _results;
        })()).join('');
        return text.replace(pattern, function (match, space, url) {
            var link;
            link = (typeof option.callback === "function" ? option.callback(url) : void 0) || ("<a href='" + url + "'" + linkAttributes + ">" + url + "</a>");
            return "" + space + link;
        }).linkUsernames().linkHashtags();
    };

    linkUsernames = function () {
        var text = this;

        try { // Just for good measure, try this stuff and return original string on catch
            // We need to parse the body and replace any @id=XXX; with html links for student's actual names.
            var nameIndices = [];

            for (i = 0; i < text.length; i++) { //Get all instances of @mentions
                if (text[i] === '@') { // If we find an '@' character, see if it's followed by "id=" then a number then a semicolon
                    if (text.substr(i + 1, 3) === "id=") {
                        var digit = 0, rIndex = 4, hasDigit = false;
                        while (!isNaN(parseInt(text.substr(i + rIndex, 1)))) { // Keep reading characters until we hit something that isn't a digit
                            hasDigit = true;
                            rIndex++;
                        }
                        if (hasDigit === true && text[i + rIndex] === ';') {
                            nameIndices.push(i); // If the character following the numbers is a semicolon, we know there is a name reference here so record the index
                        }
                    }
                }
            }
            nameIndices.reverse();
            //For each iteration, check user role (if user is an Observer, display mentions as @AnonymousXXXX; else display mentions as @StudentName)
            for (i = 0; i < nameIndices.length; i++) { // In reverse order, we need to replace each @id=... with the student's link
                var index = nameIndices[i];
                // First let's get the length of the part we will replace and also record the id
                var length = 0, tempIndex = index + 1;
                var idString = "";
                while (text[tempIndex] != ";") { length++; tempIndex++; idString += text[tempIndex]; }

                // Get the id= part off the beginning of idString and the ; from the end
                idString = idString.substr(2, idString.length - 2);
                idString = idString.substr(0, idString.length - 1);

                // Then get the student's name from the id
                if (isObserver) {
                    studentFullName = "Anonymous" + Math.floor(Math.random() * 2000).toString(); //Possibly get number of students in the class, x, and multiply random by 2*x or i*x instead of 1000 (an arbitrary value)
                    text = text.replace(text.substr(index, length + 2), "<a>@" + studentFullName + "</a>"); //Observer can see someone was mentioned but will remove the link to the @ person's profile
                }
                else {
                    var id = parseInt(idString);

                    if (id != NaN) {

                        studentFullName = courseUserNames[id];
                        if (studentFullName === "" || studentFullName === undefined) continue; // If the ID doesn't represent a user, don't make a link

                        // Now replace the id number in the string with an html link with the user's full name
                        // If observer, remove href=\ (observer can see someone was mentioned but will remove the link to the @ person's profile)
                        text = text.replace(text.substr(index, length + 2), "<a href=\"/Feed/Profile/" + id + "?component=7\" class=\"Mention\">@" + studentFullName + "</a>");
                    }
                }
            }
            return text;
        }
        catch (e) {
            return this; // Will return unmodified text in failure
        }
    };

    linkHashtags = function () {
        var htPattern = /(^|[\s\n>]|<br\/?>)#([a-z|0-9]+)/gi;
        return this.replace(htPattern, '$1<a class="Hashtag" href="/Feed/ShowHashtag?hashtag=$2?component=7">#$2</a>');
    }

    embedYouTube = function (text) {
        var regexG = /(?:https?:\/\/)?(?:www\.)?(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))((\w|-){11})(?:\S+)?/g;
        var regex = /(?:https?:\/\/)?(?:www\.)?(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))((\w|-){11})(?:\S+)?/;
        var match = text.match(regexG);

        if (match) {
            if (match.length === 1) { // Single link
                // Get non-global match so we can get the video id
                match = text.match(regex);
                var url = match[0], videoId = match[1];

                // First check to see if the match accidentally took the <div> as well
                if (url.includes("</div>")) url = url.replace("</div>", "");

                return text.replace(url, '<br /><iframe width="360" height="202" src="//www.youtube.com/embed/' + videoId + '" frameborder="0" allowfullscreen></iframe><br />');
            }
            else { // Multiple links
                for (i = 0; i < match.length; i++) {
                    var url = match[i];

                    // First check to see if the match accidentally took the <div> as well
                    if (url.includes("</div>")) url = url.replace("</div>", "");

                    // Then get video id by doing another match (non-global) which we know will only have one result.
                    var videoId = url.match(regex);
                    videoId = videoId[1];

                    text = text.replace(url, '<br /><iframe width="360" height="202" src="//www.youtube.com/embed/' + videoId + '" frameborder="0" allowfullscreen></iframe><br />');
                }
                return text;
            }
        }
        else return text; // If no youtube link found by regex, just return original content
    }

    formatCode = function (text) {

        var re = /\[code\]/g;
        var indexList = new Array();
        while ((match = re.exec(text)) != null) {
            indexList.push(match.index);
        }

        var start, middle, end;

        if (indexList.length == 2) {
            start = text.slice(0, indexList[0]);
            middle = (text.slice(indexList[0], indexList[1] + 6)).replace(/\[code\]/g, "").trim();
            end = text.slice(indexList[1] + 6), text.length;

            var result = self.hljs.highlightAuto(middle);
            middle = result.value;
            text = start + "<pre><code >" + decodeHtml(middle) + "</code></pre>" + end;
        }

        var re = /```/g;
        var indexList = new Array();
        while ((match = re.exec(text)) != null) {
            indexList.push(match.index);
        }

        if (indexList.length == 2) {
            start = text.slice(0, indexList[0]);
            middle = (text.slice(indexList[0], indexList[1] + 3)).replace(/```/g, "").trim();
            end = text.slice(indexList[1] + 3), text.length;

            var result = self.hljs.highlightAuto(middle);
            middle = result.value;
            text = start + "<pre><code >" + decodeHtml(middle) + "</code></pre>" + end;
        }

        return text;
    }

    embedImages = function (text) {

        var re = /(https?:\/\/.*\.(?:png|jpg|gif|gifv))/gi;

        var indexListUrl = new Array();
        while ((match = re.exec(text)) != null) {
            indexListUrl.push(match[0]);
        }

        //remove any duplicates so we only replace once
        var uniqueUrls = new Array();
        $.each(indexListUrl, function (i, el) {
            if ($.inArray(el, uniqueUrls) === -1) uniqueUrls.push(el);
        });

        for (var i = 0; i < uniqueUrls.length; i++) {
            var re = new RegExp(RegExp.quote(uniqueUrls[i]), "g");
            text = text.replace(re, "<br/><a target=\"_blank\" href=\"" + uniqueUrls[i] + "?component=7\"><img src=\"" + uniqueUrls[i] + "?component=7\" alt=\"Embedded Image\" style=\"width:320px;height:240;\"></a><br/>");
        }

        return text;
    }

    String.prototype['autoLink'] = autoLink;
    String.prototype['linkUsernames'] = linkUsernames;
    String.prototype['linkHashtags'] = linkHashtags;
    String.prototype['embedYouTube'] = embedYouTube;
    String.prototype['formatCode'] = formatCode;
    String.prototype['embedImages'] = embedImages;

}).call(this);

function getUserNames() {
    var namesAndIds = [];
    $.ajax({
        url: "/Feed/GetProfileNames",
        method: "POST",
        async: false,
        success: function (data) {
            namesAndIds = data.userProfiles;
            for (var k in namesAndIds) { //remove the space in the middle of each name
                if (namesAndIds.hasOwnProperty(k)) {
                    namesAndIds[k] = namesAndIds[k].replace(" ", "");
                }
            }
        }
    })
    return namesAndIds;
}

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

function decodeHtml(html) {
    var txt = document.createElement("textarea");
    txt.innerHTML = html;
    return txt.value;
}

RegExp.quote = function (str) {
    return str.replace(/([.?*+^$[\]\\(){}|-])/g, "\\$1");
};