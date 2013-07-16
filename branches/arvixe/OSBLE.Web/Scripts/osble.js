﻿$(document).ready(documentReady);

//Called when the document has finished loading and is safe to make DOM calls
function documentReady() {
    parseDates();
}

//converts UTC times to local (browser) times
function parseDates() {
    $('time.utc-time').each(function (index) {
        var milliseconds = $(this).attr('datetime');
        var formatString = $(this).attr('data-date-format');
        var currentDate = moment.utc(milliseconds, 'X');
        var localDate = new Date();
        var localOffset = localDate.getTimezoneOffset();
        currentDate = currentDate.subtract('minutes', localOffset);
        $(this).html(currentDate.format(formatString));
        $(this).removeClass("utc-time");
        $(this).addClass("local-time");
    });
}

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

function onSilverlightError(sender, args) {
    var appSource = "";
    if (sender != null && sender != 0) {
        appSource = sender.getHost().Source;
    }

    var errorType = args.ErrorType;
    var iErrorCode = args.ErrorCode;

    if (errorType == "ImageError" || errorType == "MediaError") {
        return;
    }

    var errMsg = "Unhandled Error in Silverlight Application " + appSource + "\n";

    errMsg += "Code: " + iErrorCode + "    \n";
    errMsg += "Category: " + errorType + "       \n";
    errMsg += "Message: " + args.ErrorMessage + "     \n";

    if (errorType == "ParserError") {
        errMsg += "File: " + args.xamlFile + "     \n";
        errMsg += "Line: " + args.lineNumber + "     \n";
        errMsg += "Position: " + args.charPosition + "     \n";
    }
    else if (errorType == "RuntimeError") {
        if (args.lineNumber != 0) {
            errMsg += "Line: " + args.lineNumber + "     \n";
            errMsg += "Position: " + args.charPosition + "     \n";
        }
        errMsg += "MethodName: " + args.methodName + "     \n";
    }
    //alert(errMsg);
    //omthrow new Error(errMsg);
}