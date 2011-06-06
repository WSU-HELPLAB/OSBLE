$(function () {

    $('#add_new_deliverable').click(function () {
        addNewDeliverable();
    });

    $('#new_deliverable_name').keypress(deliverableFormSubmit);

    $('#new_deliverable_type').keypress(deliverableFormSubmit);

    $('#remove_selected_deliverable').click(function () {
        removeSelectedDeliverable();
    });

});

function deliverableFormSubmit(e) {
    if (e.which == '13') {
        addNewDeliverable();
        e.preventDefault();
    }
}

var deliverableIndex = 0;

function addNewDeliverable(d) {
    if (d == undefined) {
        d = new Object({
            name: $('#new_deliverable_name').val(),
            fileType: parseInt($('#new_deliverable_type').val())
        });

        $('#new_deliverable_name').val("");
        $('#new_deliverable_name').focus();
    }

    if (d.name == undefined || d.name == "") {
        alert('The deliverable name is required.');
        return false;
    }

    var validFileRegex = /^[a-z0-9\_\-]*$/i;

    if (!(validFileRegex.test(d.name))) {
        alert("File names can only contain alphanumerics, '-', and '_'.");
        return false;
    }

    function duplicateObject() {
        this.Value = false;
        return this;
    }

    duplicateExists = false;

    $('#deliverable_data').children().each(function () {
        if (
            (d.name.toLowerCase() == $(this).children('.deliverable_name').first().val().toLowerCase()) &&
            (d.fileType == parseInt($(this).children('.deliverable_type').first().val()))
            ) {

            duplicateExists = true;
            return false;
        }
    });

    if (duplicateExists) {
        alert('You cannot create duplicate deliverables.');
        return false;
    }

    $('#deliverable_list').addOption(deliverableIndex, d.name + " - " + $('#new_deliverable_type').children('option').eq(d.fileType).html());

    $('#deliverable_data').append('<div id="deliverable_' + deliverableIndex + '"/>');

    var newDeliverable = $('#deliverable_' + deliverableIndex);

    newDeliverable.append('<input type="hidden" class="deliverable_name" value="' + d.name + '" />');
    newDeliverable.append('<input type="hidden" class="deliverable_type" value="' + $('#new_deliverable_type').children('option').eq(d.fileType).val() + '" />');

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
    var i=0;
    $('#deliverable_data').children().each(function () {
        $(this).children('.deliverable_name').first().attr('name','Assignment.Deliverables[' + i.toString() + '].Name');
        $(this).children('.deliverable_type').first().attr('name', 'Assignment.Deliverables[' + i.toString() + '].Type');

        i++;
    });
    
}