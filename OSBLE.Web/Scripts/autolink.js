
//Downloaded from: https://github.com/bryanwoods/autolink-js

(function () {
    var autoLink, linkUsernames, linkHashtags,
      __slice = [].slice;

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
        var unPattern = /(^|[\s\n>]|<br\/?>)@id=([0-9]+)/g;
        return this.replace(unPattern, function (match, space, index) {
            var name = "";
            $.ajax({
                url: "/Feed/GetProfileName",
                data: { id: index },
                method: "POST",
                async: false,
                success: function (data) {
                    name = data.Name;
                }
            })

            if (index > 0)
                return space + '<a href="/Profile/Index/' + index + '">@' + name + '</a>';
            else
                return match;
        });
    };

    linkHashtags = function () {
        var htPattern = /(^|[\s\n>]|<br\/?>)#([a-z]+)/gi;
        return this.replace(htPattern, '$1<a href="/Feed/ShowHashtag?hashtag=$2">#$2</a>');
    }

    String.prototype['autoLink'] = autoLink;
    String.prototype['linkUsernames'] = linkUsernames;
    String.prototype['linkHashtags'] = linkHashtags;

}).call(this);