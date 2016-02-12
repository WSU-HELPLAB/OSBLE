//**********
// deliverables.js
//
// Responsible for manipulating /AssignmentWizard/Deliverables view code
//
//**********

// vars
var deliverableIndex = 0;

// onLoad
$(function () {
    $('#add_new_deliverable').click(function () {
        addNewDeliverable(true);
        return false;
    });

    $('#new_deliverable_name').keypress(deliverableFormSubmit);

    $('#new_deliverable_type').keypress(deliverableFormSubmit);

    $('#remove_selected_deliverable').click(function () {
        removeSelectedDeliverable();
    });

    $("[name=NextButton]").click(function (e) {
        if (noDeliverables())
            preventSubmit(e);
    });
});

function preventSubmit(e) {
    e.preventDefault();
    alert("Click 'add this deliverable to the assignment' currently no deliverables are required listed in the assignment.  Conversely if you do not want deliverables, edit the assignment components and uncheck the box requiring deliverables.");
}

function noDeliverables() {
    var deliverableExists = $("#deliverable_0").length;

    if (deliverableExists)
        return false;
    return true;
}

function deliverableFormSubmit(e) {
    if (e.which == '13') {
        addNewDeliverable(true);
        e.preventDefault();
    }
}

function addNewDeliverable(isAdd, deliverableData) {
    // validation
    if (deliverableData == undefined) {
        deliverableData = new Object({
            name: $('#new_deliverable_name').val(),
            fileType: parseInt($('#new_deliverable_type').val()),
        });

        $('#new_deliverable_name').val("");
        $('#new_deliverable_name').focus();
    }

    if (deliverableData.name == undefined || deliverableData.name == "") {
        if (isAdd) {
            alert('The deliverable name is required.');
        }
        return false;
    }

    var validFileRegex = /^[a-z0-9\_\.\-]*$/i;

    if (!(validFileRegex.test(deliverableData.name))) {
        alert("File names can only contain alphanumerics, '-', '_', and '.'");
        return false;
    }

    duplicateExists = false;

    $('#deliverable_data').children().each(function () {
        if ((deliverableData.name.toLowerCase() == $(this).find('.deliverable_name').first().val().toLowerCase()) &&
            (deliverableData.fileType == parseInt($(this).find('.deliverable_type').first().val()))) {
            duplicateExists = true;
            return false;
        }
    });

    if (duplicateExists) {
        alert('You cannot create duplicate deliverables.');
        return false;
    }

    $('#deliverable_data').append('<div id="deliverable_' + deliverableIndex + '" class="deliverable" />');

    var newDeliverable = $('#deliverable_' + deliverableIndex);

    var typeVal = $('#new_deliverable_type').children('option').eq(deliverableData.fileType).val();
    var typeName = $('#new_deliverable_type').children('option').eq(deliverableData.fileType).html();

    // delete button
    newDeliverable.append('<div class="deliverable_tools"><a href="#" title="Delete This Deliverable" onclick="$(this).parent().parent().hide(\'highlight\',function(){$(this).remove()}); setDeliverableIndex(); return false;"><img src="/Content/images/delete_up.png" alt="Delete Button" /></a></div>');

    // main layout
    newDeliverable.append('<table><tr>');
    newDeliverable.append('<td>File Name:</td><td>' + deliverableData.name + '</td>');
    newDeliverable.append('</tr><tr>');
    newDeliverable.append('<td>Type:</td><td>' + typeName + '</td>');
    newDeliverable.append('</tr></table>');

    newDeliverable.append('<input type="hidden" class="deliverable_name" value="' + deliverableData.name + '" />');
    newDeliverable.append('<input type="hidden" class="deliverable_type" value="' + typeVal + '" />');

    deliverableIndex++;

    setDeliverableIndex();

}

function removeSelectedDeliverable() {
    var value = $('#deliverable_list').selectedValues()[0];

    $('#deliverable_list').removeOption(value);
    $('#deliverable_' + value).remove();

    setDeliverableIndex();
}

function setDeliverableIndex() {
    var i = 0;
    $('#deliverable_data').children().each(function () {
        $(this).find('.deliverable_name').first().attr('name', 'Assignment.Deliverables[' + i.toString() + '].Name');
        $(this).find('.deliverable_type').first().attr('name', 'Assignment.Deliverables[' + i.toString() + '].Type');
        i++;
    });
}