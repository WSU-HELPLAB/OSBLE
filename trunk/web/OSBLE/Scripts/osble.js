// Ready function for date/time pickers
function setupDateTime() {
    $('.date_picker').datepicker();
    $('.time_picker').timepicker({
        showPeriod: true,
        showLeadingZero: false
    });
}

$(function () {
    setupDateTime();
});