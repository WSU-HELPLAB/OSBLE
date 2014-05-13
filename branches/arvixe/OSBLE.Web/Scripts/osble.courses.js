var meetingTemplate= '\
        <div class="meeting_time" id="meeting_time_${count}">\
            <div class="meeting_time_tools">\
                <a href="#" title="Delete This Meeting Time" onclick="$(this).parent().parent().hide(\'highlight\',function(){$(this).remove()}); return false;"><img src="/Content/images/delete_up.png" alt="Delete Button" /></a>\
            </div>\
            <table>\
            <tr>\
                <td>S</td>\
                <td>M</td>\
                <td>T</td>\
                <td>W</td>\
                <td>T</td>\
                <td>F</td>\
                <td>S</td>\
            </tr>\
            <tr>\
                <td>\
                    <input type="checkbox" ${sun_check} value="true" name="meeting_sunday_${count}" />\
                </td>\
                <td>\
                    <input type="checkbox" ${mon_check} value="true" name="meeting_monday_${count}" />\
                </td>\
                <td>\
                    <input type="checkbox" ${tue_check} value="true" name="meeting_tuesday_${count}" />\
                </td>\
                <td>\
                    <input type="checkbox" ${wed_check} value="true" name="meeting_wednesday_${count}" />\
                </td>\
                <td>\
                    <input type="checkbox" ${thu_check} value="true" name="meeting_thursday_${count}" />\
                </td>\
                <td>\
                    <input type="checkbox" ${fri_check} value="true" name="meeting_friday_${count}" />\
                </td>\
                <td>\
                    <input type="checkbox" ${sat_check} value="true" name="meeting_saturday_${count}" />\
                </td>\
            </tr>\
            </table>\
            <table>\
            <tr>\
                <td>Name (Lecture, Lab, etc.)</td>\
                <td>\
                <input type="text" size="10" name="meeting_name_${count}" value="${name}" data-val="true" data-val-required="Name Required" />\
                <br />\
                <span class="field-validation-valid" data-valmsg-for="meeting_name_${count}" data-valmsg-replace="true"></span>\
                </td>\
            </tr>\
            <tr>\
                <td>Location</td>\
                <td>\
                <input type="text" size="10" name="meeting_location_${count}" value="${location}" data-val="true" data-val-required="Location Required" />\
                <br />\
                <span class="field-validation-valid" data-valmsg-for="meeting_location_${count}" data-valmsg-replace="true"></span>\
                </td>\
            </tr>\
            <tr>\
                <td>Start Time</td>\
                <td>\
                <input type="text" size="10" class="time_picker" name="meeting_start_${count}" value="${start}" data-val="true" data-val-required="Start Time Required" />\
                <br />\
                <span class="field-validation-valid" data-valmsg-for="meeting_start_${count}" data-valmsg-replace="true"></span>\
                </td>\
            </tr>\
            <tr>\
                <td>End Time</td>\
                <td>\
                <input type="text" size="10" class="time_picker" name="meeting_end_${count}" value="${end}" data-val="true" data-val-required="End Time Required" />\
                <br />\
                <span class="field-validation-valid" data-valmsg-for="meeting_end_${count}" data-valmsg-replace="true"></span>\
                </td>\
            </tr>\
            \
            </table>\
        </div>\
        ';


var breakTemplate = '\
        <div class="break" id="break_${count}">\
            <div class="break_tools">\
                <a href="#" title="Delete This Break Time" onclick="$(this).parent().parent().hide(\'highlight\',function(){$(this).remove()}); return false;"><img src="/Content/images/delete_up.png" alt="Delete Button" /></a>\
            </div>\
            <table>\
            <tr>\
                <td>Name</td>\
                <td>\
                <input type="text" size="10" name="break_name_${count}" value="${name}" data-val="true" data-val-required="Name Required" />\
                <br />\
                <span class="field-validation-valid" data-valmsg-for="break_name_${count}" data-valmsg-replace="true"></span>\
                </td>\
            </tr>\
            <tr>\
                <td>Start</td>\
                <td>\
                <input type="text" class="date_picker" size="10" name="break_start_${count}" value="${start}" data-val="true" data-val-required="Start Date Required" />\
                <br />\
                <span class="field-validation-valid" data-valmsg-for="break_start_${count}" data-valmsg-replace="true"></span>\
                </td>\
            </tr>\
            <tr>\
                <td>End</td>\
                <td>\
                <input type="text" class="date_picker" size="10" name="break_end_${count}" value="${end}" data-val="true" data-val-required="End Date Required" />\
                <br />\
                <span class="field-validation-valid" data-valmsg-for="break_end_${count}" data-valmsg-replace="true"></span>\
                </td>\
            </tr>\
            \
            </table>\
        </div>\
        ';
function getTimezone(tz) {
    if (tz == undefined) {
        tz = Object();
    }
    $.tmpl(timezoneTemplate, {

    })
}
function addMeetingTime(mt) {
    var count = parseInt($('#meetings_max').val());

    if (mt == undefined) {
        mt = Object();
    }

    $.tmpl(meetingTemplate, {
        count: count,
        sun_check: mt.sun_check == "True" ? "checked='checked'" : "",
        mon_check: mt.mon_check == "True" ? "checked='checked'" : "",
        tue_check: mt.tue_check == "True" ? "checked='checked'" : "",
        wed_check: mt.wed_check == "True" ? "checked='checked'" : "",
        thu_check: mt.thu_check == "True" ? "checked='checked'" : "",
        fri_check: mt.fri_check == "True" ? "checked='checked'" : "",
        sat_check: mt.sat_check == "True" ? "checked='checked'" : "",
        name: mt.name,
        location: mt.location,
        start: mt.start,
        end: mt.end
        
        }).appendTo('#meeting_times');

    setupDateTime();

    $('form').removeData("validator");
    $('form').removeData("unobtrusiveValidation");
    $.validator.unobtrusive.parse('form');

    $('#meetings_max').val(count + 1);
}

function addBreak(b) {
    var count = parseInt($('#breaks_max').val());

    if (b == undefined) {
        b = Object();
    }

    $.tmpl(breakTemplate, {
        count: count,
        name: b.name,
        start: b.start,
        end: b.end

    }).appendTo('#breaks');

    setupDateTime();

    $('form').removeData("validator");
    $('form').removeData("unobtrusiveValidation");
    $.validator.unobtrusive.parse('form');

    $('#breaks_max').val(count + 1);
}