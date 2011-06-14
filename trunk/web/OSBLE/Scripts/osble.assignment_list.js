$(function () {
    $('.assignment_accordion').accordion({ collapsible: true, header: 'div.assignment_header', active: false });

    $('.assignment_header').unbind('focus');
});