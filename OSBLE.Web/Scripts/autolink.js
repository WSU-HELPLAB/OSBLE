
//Downloaded from: https://github.com/bryanwoods/autolink-js

(function () {
    var autoLink, linkUsernames, linkHashtags,
      __slice = [].slice;
    var courseUserNames = getUserNames();    

    autoLink = function () {
        var k, linkAttributes, option, options, pattern, v;
        options = 1 <= arguments.length ? __slice.call(arguments, 0) : [];

        pattern = /(^|[\s\n>]|<br\/?>)((?:https?|ftp):\/\/[\-A-Z0-9+\u0026\u2019@#\/%?=()~_|!:,.;]*[\-A-Z0-9+\u0026@#\/%=~()_|])/gi;
        if (!(options.length > 0)) {
            return this.replace(pattern, "$1<a href='$2'>$2</a>").linkUsernames().linkHashtags();
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
        return this.replace(pattern, function (match, space, url) {
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

            for (i = 0; i < text.length; i++) {
                // If we find an '@' character, see if it's followed by "id=" then a number then a semicolon
                if (text[i] === '@') {
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
                var id = parseInt(idString);

                if (id != NaN) {                    
                    
                    studentFullName = courseUserNames[id];
                    
                    if (studentFullName === "" || studentFullName === undefined) continue; // If the ID doesn't represent a user, don't make a link

                    // Now replace the id number in the string with an html link with the user's full name
                    text = text.replace(text.substr(index, length + 2), "<a href=\"/Profile/Index/" + id + "\">@" + studentFullName + "</a>");
                }
            }

            return text;
        } catch (e) {
            return this; // Will return unmodified text in failure
        }
    };

    linkHashtags = function () {
        var htPattern = /(^|[\s\n>]|<br\/?>)#([a-z|0-9]+)/gi;
        return this.replace(htPattern, '$1<a class="Hashtag" href="/Feed/ShowHashtag?hashtag=$2">#$2</a>');
    }

    String.prototype['autoLink'] = autoLink;
    String.prototype['linkUsernames'] = linkUsernames;
    String.prototype['linkHashtags'] = linkHashtags;

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
