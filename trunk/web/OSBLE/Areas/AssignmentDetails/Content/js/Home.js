//register event handlers
$(document).ready(function () {
    $("#DeleteAssignmentLink").click(openConfirmDeleteWindow);
});

/*Functions and variables for deleteAssignment modal box*/
function openConfirmDeleteWindow(event) {
    event.preventDefault();
    $("#confirmDeleteWindow").dialog({
        modal: true,
        resizable: false,
        width: 310,
        height: 135,
        closeOnEscape: false
    });
    return false;
}

function hideConfirmDeleteWindow() {
    $("#confirmDeleteWindow").dialog('close');
}