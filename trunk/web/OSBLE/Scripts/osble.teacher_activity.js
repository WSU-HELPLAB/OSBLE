$(function () {
    $('.activity_accordion').accordion({ collapsible: true, active: false, header: 'div.activity_header', autoHeight: false });

    $('.activity_accordion').unbind('focus');
});
