//This is a simple javascript that is called on the main layout page that gets loaded on every osble page.
//It's purpose is to asynchronously postback to the server every 60 seconds so the user maintains the session 
//and it doesn't get logged out.


//$(document).ready(function () {
//    //check in every 5 minutes
//    window.setInterval("time()", 5 * 60000);
//});
//var isTimedOut = false;
//function time() {
//    $.ajax({
//        url: "/Home/Time",
//        success: function (data) {
//            data = $.trim(data);
//            if (data == "true") {
//            }
//            else {
//                if (!isTimedOut) {
//                    alert("The session has timed out.  To avoid losing work on this page, please open a new browser window and re-log into OSBLE.");
//                    isTimedOut = true;
//                }
//            }
//        },
//        failure: function (result) {
//            if (!isTimedOut) {
//                alert("The session has timed out.  To avoid losing work on this page, please open a new browser window and re-log into OSBLE.");
//                isTimedOut = true;
//            }
//        }
//    });
//}