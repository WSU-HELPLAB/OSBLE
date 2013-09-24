$(document).ready(documentReady);

function documentReady() {
    var localDate = new Date();
    var localOffset = localDate.getTimezoneOffset();
    var releaseDateStr = $("#ReleaseDate").val() + " " + $("#ReleaseTime").val();
    var dueDateStr = $("#DueDate").val() + " " + $("#DueTime").val();
    var releaseMoment = moment(releaseDateStr, "MM/DD/YYYY hh:mm A");
    var dueMoment = moment(dueDateStr, "MM/DD/YYYY hh:mm A");

    //Event time and date converstions
    var StartEventTimeStr = $("#StartDate").val() + " " + $("#StartTime").val();
    var EndEventTimeStr = $("#EndDate").val() + " " + $("#EndTime").val();

    var startEventTimeMoment = moment(StartEventTimeStr, "MM/DD/YYYY hh:mm A");
    var endEventTimeMoment = moment(EndEventTimeStr, "MM/DD/YYYY hh:mm A");

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

    //Event time and date converstions
    startEventTimeMoment = startEventTimeMoment.subtract('minutes', localOffset);
    endEventTimeMoment = endEventTimeMoment.subtract('minutes', localOffset);

    $("#StartDate").val(startEventTimeMoment.format('MM/DD/YYYY'));
    $("#StartTime").val(startEventTimeMoment.format('hh:mm A'));

    $("#EndDate").val(endEventTimeMoment.format('MM/DD/YYYY'));
    $("#EndTime").val(endEventTimeMoment.format('hh:mm A'));
}