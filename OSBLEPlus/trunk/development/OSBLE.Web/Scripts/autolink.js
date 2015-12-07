
//Downloaded from: https://github.com/bryanwoods/autolink-js

(function () {
    var autoLink, linkUsernames,
      __slice = [].slice;

    autoLink = function () {
        var k, linkAttributes, option, options, pattern, v;
        options = 1 <= arguments.length ? __slice.call(arguments, 0) : [];

        pattern = /(^|[\s\n>]|<br\/?>)((?:https?|ftp):\/\/[\-A-Z0-9+\u0026\u2019@#\/%?=()~_|!:,.;]*[\-A-Z0-9+\u0026@#\/%=~()_|])/gi;
        if (!(options.length > 0)) {
            return this.replace(pattern, "$1<a href='$2'>$2</a>").linkUsernames();
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
        }).linkUsernames();
    };

    linkUsernames = function () {
        var unPattern = /(^|[\s\n>]|<br\/?>)@([A-Z][a-z]+)([A-Z][a-z]+)/g;
        return this.replace(unPattern, function (match, space, first, last) {
            var index = -1;

            $.ajax({
                url: "/Feed/GetProfileIndexForName",
                data: { firstName: first, lastName: last },
                method: "POST",
                async: false,
                success: function (data) {
                    index = data.Index;
                }
            });

            if (index > 0)
                return space + '<a href="/Profile/Index/' + index + '">@' + first + last + '</a>';
            else
                return match;
        });
    };

    String.prototype['autoLink'] = autoLink;
    String.prototype['linkUsernames'] = linkUsernames;

}).call(this);