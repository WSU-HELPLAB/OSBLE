﻿
// ********** DELIVERABLE **********

// vars
var deliverableIndex = 0;

// onLoad
$(function () {

    $('#add_new_deliverable').click(function () {
        addNewDeliverable();
        return false;
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


function addNewDeliverable(d) {

    // validation
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

    var validFileRegex = /^[a-z0-9\_\.\-]*$/i;

    if (!(validFileRegex.test(d.name))) {
        alert("File names can only contain alphanumerics, '-', '_', and '.'");
        return false;
    }

    // not used...?
    //function duplicateObject() {
    //    this.Value = false;
    //    return this;
    //}

    duplicateExists = false;

    $('#deliverable_data').children().each(function () {
        if ( (d.name.toLowerCase() == $(this).find('.deliverable_name').first().val().toLowerCase()) &&
            (d.fileType == parseInt($(this).find('.deliverable_type').first().val())) ) {

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

    // delete button
    newDeliverable.append('<div class="deliverable_tools"><a href="#" title="Delete This Deliverable" onclick="$(this).parent().parent().hide(\'highlight\',function(){$(this).remove()}); setDeliverableIndex(); return false;"><img src="/Content/images/delete_up.png" alt="Delete Button" /></a></div>');

    // main layout
    newDeliverable.append('<table><tr>');
    newDeliverable.append('<td>File Name:</td><td>' + d.name + '</td>');
    newDeliverable.append('</tr><tr>');
    newDeliverable.append('<td>Type:</td><td>' + typeName + '</td>');
    newDeliverable.append('</tr></table>');

    newDeliverable.append('<input type="hidden" class="deliverable_name" value="' + d.name + '" />');
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
        $(this).find('.deliverable_name').first().attr('name','Assignment.Deliverables[' + i.toString() + '].Name');
        $(this).find('.deliverable_type').first().attr('name', 'Assignment.Deliverables[' + i.toString() + '].Type');

        i++;
    });

}


// ********** LINE REVIEW **********

//deliverable categories
//  add categories and tags (is this the right name?) to model(s)

// empty tag text box: do nothing
// empty category text box: ignore (don't add, don't warn)
// duplicate category: warning
//   list of categories, linked to 
// [saving the deliverable removes all categories and resets vars]

// validation successful --> add deliverable

var MAX_CATEGORIES = 6;
var categoryIndex = 0; // unique identifier
var categoryCount = 0; // to check the number of categories
var categoryOptionIndex = new Array(MAX_CATEGORIES);
for (var i = 0; i < MAX_CATEGORIES; i++) {
    categoryOptionIndex[i] = 1; // because 0 is required and thus hard-coded
        // these index variables have no upper limit
}

$(function () {

    $('#InstructorCanReview').removeAttr('checked');
    $('#manual_config_options').show();
    $("input[name='line_review_options']:eq(0)").attr('checked', 'checked');


    $('#InstructorCanReview').change(function () {
        if ($(this).attr('checked')) {
            $('#line_review_config').show('blind');
        }
        else {
            $('#line_review_config').hide('blind');
        }
    });

    $('#add_new_category').click(function () {
        addNewCategory();
        return false;
    });


    $("input[name='line_review_options']").change(function () {
        if ($("input[name='line_review_options']:checked").val() == 'ManualConfig') {
            $('#auto_config_options').hide();
            $('#manual_config_options').show('blind');
        }
        else if ($("input[name='line_review_options']:checked").val() == 'AutoConfig') {
            $('#manual_config_options').hide();
            $('#auto_config_options').show('blind');
        }
        else {
            throw "Unknown Radio Button Checked: line_review_options"
        }
    });

    
});

// apply this function in an event listener to every text box to prevent accidental submission of the whole form by the user
function disableSubmit(e) {
    // if the return key is pressed, do nothing
    if (e.which == 13) {
        return false;
    }
}


function addNewCategory() {

    // limit to six
    if (categoryCount >= MAX_CATEGORIES) {
        alert("The maximum number of categories is " + MAX_CATEGORIES + ".");
        return false;
    }

    // create new item
    $('#category_data').append('<div id="category_' + categoryIndex + '" class="deliverable" />');
    var newCategory = $('#category_' + categoryIndex);

    // delete button
    newCategory.append('<div class="deliverable_tools"><a href="#" title="Delete This Deliverable" onclick="$(this).parent().parent().hide(\'highlight\', function () { $(this).remove() }); categoryCount--; return false;"><img src="/Content/images/delete_up.png" alt="Delete Button" /></a></div>');

    // main layout
    newCategory.append('<table><tr>');
    newCategory.append('<td>Category Name:</td><td> <input type="text" id="cat' + categoryIndex + '"> </td>');
    newCategory.append('</tr><tr>');
    newCategory.append('<td>Options:</td><td></td></tr><tr> ');

    //      
    newCategory.append('<td><a href="#" id="add_option_' + categoryIndex + '" title="Add New Option" style="text-decoration:none;"> <img src="/Content/images/add_up.png" alt="(+)" /> Add New Option </a> </td>'); // must be all one line to work
    newCategory.append('</tr><tr><td colspan="2"><div id="option_data_' + categoryIndex + '"> <input type="text" id="option_' + categoryIndex + '_0"> <br />');
    // required first option
    newCategory.append('');


    newCategory.append('</div><td></tr></table>');

    // add event listeners
    $('#cat' + categoryIndex).keypress(disableSubmit);
    $('#option_' + categoryIndex + '_0').keypress(disableSubmit);

    $('#add_option_' + categoryIndex).click(function () {

        var i = this.id.substring(11, this.id.length);
        var d = $('#option_data_' + i);

        // all one line because append adds closing tags automatically if there isn't a closing tag (ie </div>) within the string it is appending :/
        d.append('<div><input type="text" id="option_' + i + '_' + categoryOptionIndex[i] + '"> <div style="display: inline; position:relative; top:0.25em;"><a href="#" title="Delete This Option" onclick="$(this).parent().parent().hide(\'highlight\', function () { $(this).remove() }); categoryOptionIndex[' + i + ']--; return false;"><img src="/Content/images/delete_up.png" alt="Delete" /></a></div> </div>');
        $('#option_' + i + '_' + categoryOptionIndex[i]).keypress(disableSubmit);

        categoryOptionIndex[i]++;

        return false;
    });

    // set focus to newly created category
    $('#cat' + categoryIndex).focus();

    // keep track of indices
    categoryIndex++;
    categoryCount++;

}





