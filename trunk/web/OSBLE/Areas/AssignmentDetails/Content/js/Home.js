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

function create_and_open_downloading_submission_dialog() {

    //create the div that we will then make into a dialog
    $('body').append(
      '<div id="downloading_submission_dialog" title="Download Submission"> \
       <p>The zip is being generated and will automatically start downloading when it is ready, please do not leave the page or click the link again</p> \
   </div>');

    //make the div we just created into a dialog box
    $('#downloading_submission_dialog').dialog({
        modal: false,
        autoOpen: true,
        resizable: true,
        width: 350,
        height: 300,
        closeOnEscape: true,
        close: remove_downloading_submission_dialog,
        buttons: { "OK": remove_downloading_submission_dialog }
    });

    $('#downloading_submission_dialog').dialog('open');
    return false;
}

function remove_downloading_submission_dialog() {

    //change the dialog back into a normal div (that is what destroy does although it does not destroy the div)
    $('#downloading_submission_dialog').dialog("destroy");

    //then remove the div
    $('#downloading_submission_dialog').remove();
}

function SubmissionLoaded() {
    $('#submit_files_dialog').dialog('open');
}