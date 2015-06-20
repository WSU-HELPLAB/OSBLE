
$(document).ready(function () {
    
    

});

$(".menu_item").click(function () {
    //debugger;
    //remove id="current" from previous tab
    $(".current").removeClass("current");
    //add id="current" to current tab
    $(this).addClass("current");

});