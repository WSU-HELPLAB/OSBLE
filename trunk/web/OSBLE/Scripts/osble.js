// Ready function for date/time pickers
function setupDateTime() {
    setupDate('.date_picker');
    setupTime('.time_picker');
}

// Used to set up dynamically created time pickers.
function setupTime(element) {
    $(element).timepicker({
        showPeriod: true,
        showLeadingZero: false
    });
}

// Used to set up dynamically created date pickers.
function setupDate(element) {
    $(element).datepicker();
}

$(function () {
    setupDateTime();
});