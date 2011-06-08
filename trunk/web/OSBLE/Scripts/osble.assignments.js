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
            (d.name.toLowerCase() == $(this).find('.deliverable_name').first().val().toLowerCase()) &&
            (d.fileType == parseInt($(this).find('.deliverable_type').first().val()))
            ) {

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

    var typeVal = $('#new_deliverable_type').children('option').eq(d.fileType).val();
    var typeName = $('#new_deliverable_type').children('option').eq(d.fileType).html();

    newDeliverable.append('<div class="deliverable_tools"><a href="#" onclick="$(this).parent().parent().hide(\'highlight\',function(){$(this).remove()}); setDeliverableIndex(); return false;"><img src="/Content/images/delete_up.png" alt="Delete Button" /></a></div>');


    newDeliverable.append('<table><tr>');
    newDeliverable.append('<td>File Name</td><td><input type="text" class="deliverable_name" value="' + d.name + '" /></td>');
    newDeliverable.append('</tr><tr>');
    newDeliverable.append('<td>Type</td><td>' + typeName + '</td>');
    newDeliverable.append('</tr></table>');


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
    var i=0;
    $('#deliverable_data').children().each(function () {
        $(this).children('.deliverable_name').first().attr('name','Assignment.Deliverables[' + i.toString() + '].Name');
        $(this).children('.deliverable_type').first().attr('name', 'Assignment.Deliverables[' + i.toString() + '].Type');

        i++;
    });
    
}