
// Makes tabs go under "more" dropdown to save space on the navbar
function ResizeTabs() {
    var w = $('body').width();

    if ($(".navbar-toggle").css("display") != "none") {
        if ($(".MoreTab").length > 0) {
            $(".MoreTab").insertAfter($(".NavTab").last()).removeClass("MoreTab").addClass("NavTab");
        }
        $("#MoreDrop").css("display", "none");
    }
    else {
        var tabWidthAffordance = 35 + $(".navbar-header").width() + $("#NavList").width() + $(".navbar-right").width();
        while (tabWidthAffordance > w) {
            // Shrink
            var tab = $(".NavTab").last();

            tab.data("twidth", tab.width());

            tab.prependTo($("#MoreList"));
            tab.removeClass("NavTab").addClass("MoreTab");

            $("#MoreDrop").css("display", "block");
            tabWidthAffordance = 35 + $(".navbar-header").width() + $("#NavList").width() + $(".navbar-right").width();
        }
        if ($(".MoreTab").length > 0) {
            var tab = $(".MoreTab").first();

            while (tab.data("twidth") + tabWidthAffordance <= w) {
                // We can fit the next tab
                tabWidthAffordance += tab.data("twidth");
                tab.insertAfter($(".NavTab").last());
                tab.removeClass("MoreTab").addClass("NavTab");
                tab = $(".MoreTab").first();
            }

            if($(".MoreTab").length == 0)
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