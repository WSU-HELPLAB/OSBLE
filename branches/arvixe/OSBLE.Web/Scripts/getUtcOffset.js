$(document).ready(documentReady);

function documentReady() {
    var localDate = new Date();
    var localOffset = localDate.getTimezoneOffset();

    //record the local offset
    $("#utc-offset").val(localOffset);

    var cookieExists = $.cookie('utcOffset');
    if (cookieExists == null) {
        $.cookie('utcOffset', localOffset);
    }
    else {
        $.removeCookie('utcOffset');
        $.cookie('utcOffset', localOffset);
    }
}