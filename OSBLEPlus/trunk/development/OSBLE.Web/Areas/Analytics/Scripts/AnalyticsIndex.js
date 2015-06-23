
$(document).ready(function () {
    
    $(".menu_item").click(function () {
        //debugger;
        //remove id="current" from previous tab
        $(".current").removeClass("current");
        //add id="current" to current tab
        $(this).addClass("current");

        //$("#analytics_options").load("/Areas/Views/Analytics/_DefaultOptions.cshtml");
        //$.ajax({
        //        url: '/Analytics/Analytics/GetOptions',
        //        contentType: 'application/html; charset=utf-8',
        //        type: 'GET',
        //        dataType: 'html'
        //    })
        //    .success(function(result) {
        //        $("#analytics_options").html(result);
        //    })
        //    .error(function(xhr, status) {
        //        alert(status);
        //    });
    });

});



function updateCurrentTab() {
    
}