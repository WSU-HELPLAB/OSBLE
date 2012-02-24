//This is a simple javascript that is called on the main layout page that gets loaded on every osble page.
//It's purpose is to asynchronously postback to the server every 60 seconds so the user maintains the session 
//and it doesn't get logged out.


$(document).ready(function () {
    window.setInterval("time()", 60000);
});

function time() {
    $.ajax({
        url: "/Home/Time",
        success: function (data) {
            if (data == "true") {
            }
            else {
                alert("The session has timed out.");
            }
        },
        failure: function (result) {
            alert("The session has timed out.");
        }
    });
}