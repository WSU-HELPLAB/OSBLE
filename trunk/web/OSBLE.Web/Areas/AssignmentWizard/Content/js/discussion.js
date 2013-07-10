$(document).ready(documentReady);

function documentReady() {
    var localDate = new Date();
    var localOffset = localDate.getTimezoneOffset();
    var initialPostStr = $("#InitialPostDueDate").val() + " " + $("#InitialPostDueDueTime").val();

    var initialPostMoment = moment(initialPostStr, "MM/DD/YYYY hh:mm A");


    //record the local offset
    $("#utc-offset").val(localOffset);

    //adjust our moments by the local offset
    initialPostMoment = initialPostMoment.subtract('minutes', localOffset);

    //republish modified times to the browser
    $("#InitialPostDueDate").val(initialPostMoment.format('MM/DD/YYYY'));
    $("#InitialPostDueDueTime").val(initialPostMoment.format('hh:mm A'));

}