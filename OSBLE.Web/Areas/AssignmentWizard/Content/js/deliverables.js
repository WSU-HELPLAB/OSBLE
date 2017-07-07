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
    $('#add_new_deliverable').click(tryAddNewDeliverable);

    $('#new_deliverable_name').keypress(deliverableFormSubmit);

    $('#new_deliverable_type').keypress(deliverableFormSubmit);

    $('#remove_selected_deliverable').click(function () {
        removeSelectedDeliverable();
    });

    $("[name=NextButton]").click(function (e) {
        if (noDeliverables())
            preventSubmit(e);
    });

    $("#new_deliverable_type").on('change', function () {
        if ($("#new_deliverable_type :selected").text() == "PluginSubmission (.zip)") {
            $("#plugin_submission_validation").prop('hidden', false);
            $("#plugin_submission_validation_div").addClass("disabled");
        }
        else
        {
            $("#plugin_submission_validation").prop('hidden', true);
            $("#plugin_submission_validation_div").addClass("disabled");
        }
    });

    if ($("#new_deliverable_type :selected").text() == "PluginSubmission (.zip)") {
        $("#plugin_submission_validation").prop('hidden', false);
        $("#plugin_submission_validation_div").addClass("disabled");
    }
});

function tryAddNewDeliverable() {

    //clear validation formatting
    clearValidation();

    //check if a pluginSubmission has already been added
    var pluginSubmission = false;
    $(".deliverable td:contains('PluginSubmission (.zip)')").each(function () {
        pluginSubmission = true;
    });

    //get dropdown value
    var deliverableType = $("#new_deliverable_type :selected").text();
    var otherSubmissions = false;
    //if dropdown is plugin, check to make sure there are no other submissions, we want to limit plugin submissions to 1 deliverable.
    if (deliverableType == "PluginSubmission (.zip)") {
        $("#deliverable_data").children().each(function () {
            otherSubmissions = true;
        });
    }

    if (pluginSubmission || otherSubmissions) {
        $("#plugin_submission_validation").prop('hidden', false);
        $("#plugin_submission_validation_div").addClass("disabled");
    }
    else {
        addNewDeliverable(true);
    }
    return false;
}

function clearValidation() {
    $("#new_deliverable_validation").prop('hidden', true);
    $("#new_deliverable_name").removeClass("disabled");
    $("#add_deliverable_validation").prop('hidden', true);
    $("#add_deliverable_div").removeClass("disabled");
    $("#plugin_submission_validation").prop('hidden', true);
    $("#plugin_submission_validation_div").removeClass("disabled");
}

function preventSubmit(e) {
    clearValidation();
    e.preventDefault();
    $("#add_deliverable_validation").prop('hidden', false);
    $("#add_deliverable_div").addClass("disabled");    
}

function noDeliverables() {
    var deliverableExists = false;

    //check for any deliverables
    $("#deliverable_data").children().each(function () {
        deliverableExists = true;
    });

    if (deliverableExists)
        return false;
    return true;
}

function deliverableFormSubmit(e) {
    if (e.which == '13') {
        tryAddNewDeliverable();
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
            $("#new_deliverable_validation").prop('hidden', false);
            $("#new_deliverable_name").addClass("disabled");
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