$(document).ready(documentReady);

function documentReady() {
    var localDate = new Date();
    var localOffset = localDate.getTimezoneOffset();
    var releaseDateStr = $("#ReleaseDate").val() + " " + $("#ReleaseTime").val();
    var dueDateStr = $("#DueDate").val() + " " + $("#DueTime").val();
    var releaseMoment = moment(releaseDateStr, "MM/DD/YYYY hh:mm A");
    var dueMoment = moment(dueDateStr, "MM/DD/YYYY hh:mm A");

    //record the local offset
    $("#utc-offset").val(localOffset);

    //adjust our moments by the local offset
    releaseMoment = releaseMoment.subtract('minutes', localOffset);
    dueMoment = dueMoment.subtract('minutes', localOffset);

    //republish modified times to the browser
    $("#ReleaseDate").val(releaseMoment.format('MM/DD/YYYY'));
    $("#ReleaseTime").val(releaseMoment.format('hh:mm A'));

    $("#DueDate").val(dueMoment.format('MM/DD/YYYY'));
    $("#DueTime").val(dueMoment.format('hh:mm A'));


}