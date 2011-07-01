$(function () {
    $('.activity_accordion').accordion({ collapsible: true, active: 0, header: 'div.activity_header', autoHeight: false });

    $('.activity_accordion').unbind('focus');
});