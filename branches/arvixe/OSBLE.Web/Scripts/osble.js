$(document).ready(documentReady);

//Called when the document has finished loading and is safe to make DOM calls
function documentReady() {
    parseDates();
    updateTimezoneOffsets();
}

//updates all time-related elements with proper UTC offset information
function updateTimezoneOffsets()
{
    var localDate = new Date();
    var localOffset = localDate.getTimezoneOffset();

    //update all of our UTC offset information that gets sent to the server
    $('input.utc-offset').each(function () {
        $(this).val(localOffset);
    });
    
    /*
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
    $("#DueTime").val(dueMoment.format('hh:mm A'));*/
}

//converts UTC times to local (browser) times
function parseDates() {
    $('time.utc-time').each(function (index) {               
        var milliseconds = $(this).attr('datetime');
        var formatString = $(this).attr('data-date-format');                
        var original = new Date(milliseconds * 1000);        
        var originaldate = moment(original);
        var currentDate = moment.utc(milliseconds, 'X');
        var originalOffset = originaldate.zone();        
        currentDate = currentDate.subtract('minutes', originalOffset);
        $(this).html(currentDate.format(formatString));        
        $(this).removeClass("utc-time");
        $(this).addClass("course-local-time");
    });
    
    $('time.utc-time-events').each(function (index) {
        var milliseconds = $(this).attr('datetime');
        var formatString = $(this).attr('data-date-format');
        var original = new Date(milliseconds * 1000);
        var originaldate = moment(original);
        var currentDate = moment.utc(milliseconds, 'X');
        var originalOffset = originaldate.zone();

        var localDate = new Date();
        var localOffset = localDate.getTimezoneOffset();
        var od = moment(original).isDST();
        var ld = moment(localDate).isDST();

        if (!od && ld)
        {
            originalOffset -= (originalOffset - localOffset);
            currentDate = currentDate.subtract('minutes', originalOffset);
        }
        else if (od && !ld)
        {
            originalOffset += (originalOffset - localOffset);
            currentDate = currentDate.subtract('minutes', originalOffset);
        }
        else
        {
            currentDate = currentDate.subtract('minutes', originalOffset);
        }
        $(this).html(currentDate.format(formatString));        
        $(this).removeClass("utc-time");
        $(this).addClass("course-local-event-time");
    });
    $('time.utc-time-link').each(function (index) {
        var milliseconds = $(this).attr('datetime');
        var formatString = $(this).attr('data-date-format');
        var currentDate = moment.utc(milliseconds, 'X');
        var localDate = new Date();
        var localOffset = localDate.getTimezoneOffset();
        var txt = $(this).html();
        var tmp= txt.split(">");
        currentDate = currentDate.subtract('minutes', localOffset);
        var replace = tmp[0];
        replace = replace.concat(">");
        var linedate = currentDate.format(formatString);
        linedate = linedate.concat("</a>");
        replace = replace.concat(linedate);
        $(this).html(replace);        
        $(this).removeClass("utc-time-link");
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


