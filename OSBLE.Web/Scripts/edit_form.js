var text = '';

function updateText(id) {
    // get the div we're working with
    var originalText = $("#content-comment-" + id);
    var editForm = $("#edit-form-items-" + id);

    // make sure we're not already editing
    if (originalText.html() !== "Editing...") {
        text = originalText.html();
        originalText.html("Editing...");
        editForm.show("blind");
    }
}

function editText(id, bool) {
    var editForm = $("#edit-form-items-" + id);
    var originalText = $('#content-comment-' + id);
    var textFromInput = $('#edit-form-textarea-' + id);

    if (bool === true) {
        originalText.html(textFromInput.val());
    }
    else {
        originalText.html(text);
    }

    editForm.hide("blind");
}