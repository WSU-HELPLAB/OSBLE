
// Makes tabs go under "more" dropdown to save space on the navbar
function ResizeTabs() {
    if ($(".navbar-toggle").css("display") != "none") {
        if ($(".MoreTab").length > 0) {
            $(".MoreTab").insertAfter($(".NavTab").last()).removeClass("MoreTab").addClass("NavTab");
        }
        $("#MoreDrop").css("display", "none");
    }
    else {
        var availableWidth = $("#NavListRight").offset().left - $("#NavList").offset().left - 105;
        var requiredWidth = $("#NavList").width();
        while (requiredWidth > availableWidth) {
            // Shrink
            var tab = $(".NavTab").last();

            tab.data("twidth", tab.width());

            tab.prependTo($("#MoreList"));
            tab.removeClass("NavTab").addClass("MoreTab");

            $("#MoreDrop").css("display", "block");
            requiredWidth = $("#NavList").width();
        }
        if ($(".MoreTab").length > 0) {
            var tab = $(".MoreTab").first();

            while (tab.data("twidth") + requiredWidth < availableWidth) {
                // We can fit the next tab
                requiredWidth += tab.data("twidth");
                tab.insertAfter($(".NavTab").last());
                tab.removeClass("MoreTab").addClass("NavTab");
                tab = $(".MoreTab").first();
            }

            // first check if theres only one element left. Reason is that we
            // might be able to fit that tab if we get rid of the more tab
            if ($(".MoreTab").length == 1)
            {
                if (tab.data("twidth") + requiredWidth - $("#MoreDrop").width() < availableWidth)
                {
                    tab.insertAfter($(".NavTab").last());
                    tab.removeClass("MoreTab").addClass("NavTab");
                    $("#MoreDrop").css("display", "none");
                }
            }
            else if($(".MoreTab").length == 0)
            {
                $("#MoreDrop").css("display", "none");
            }
        }
    }
}

$(document).ready(function () {            
    $(window).resize(function () { ResizeTabs(); });
    ResizeTabs();
});