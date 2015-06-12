$(function () {
    $('.activity_accordion').accordion({ collapsible: true, active: false, header: 'div.activity_header', autoHeight: false});

    $('.activity_accordion').unbind('focus');
});

function loadActivityContent(activityId) {
    var tableToUpdate = "#table_" + activityId;
    var urlToGet = "/Assignment/ActivityTeacherTable/" + activityId;
    $.ajax({
        type: "GET",
        url: urlToGet,
        success: function (data) { $(tableToUpdate).html(data); }
    });
}
