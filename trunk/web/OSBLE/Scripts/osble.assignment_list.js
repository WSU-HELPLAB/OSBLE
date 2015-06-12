$(function () {
    $('.assignment_accordion').accordion({ collapsible: true, header: 'div.assignment_header', autoHeight: false, active: false });

    $('.assignment_header').unbind('focus');
});