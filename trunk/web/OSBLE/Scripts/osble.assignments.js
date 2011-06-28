
// global vars
var deliverableIndex = 0;
var categoryIndex = 0; // unique identifier
var categoryCount = 0; // to check the number of categories


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

    // empty tag text box: do nothing
    // empty category text box: ignore (don't add, don't warn)
    // duplicate category: warning
    //   list of categories, linked to 
    // [saving the deliverable removes all categories and resets vars]

    // validation successful --> add deliverable

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
    newDeliverable.append('</tr><tr>');
    newDeliverable.append('<td>Categories:</td><td><span title="i,s,b,w,y">Category A</span>, <span title="glow worm, i, robot">Category B</span></td>');
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

//deliverable categories
//  add categories and tags to model(s)

$(function () {

    $('#add_new_category').click(function () {
        addNewCategory();
        return false;
    });
    
});

function categorySubmit(e) {
    // if the return key is pressed, do nothing
    if (e.which == 13) {
        return false;
    }
}


function addNewCategory() {

    // limit to six
    if (categoryCount >= 6) {
        alert("The maximum number of categories is six.");
        return false;
    }

    // create new item
    $('#category_data').append('<div id="category_' + categoryIndex + '" class="deliverable" />');
    var newCategory = $('#category_' + categoryIndex);

    // delete button
    newCategory.append('<div class="deliverable_tools"><a href="#" title="Delete This Deliverable" onclick="$(this).parent().parent().hide(\'highlight\', function () { $(this).remove() }); categoryCount--; return false;"><img src="/Content/images/delete_up.png" alt="Delete Button" /></a></div>');

    // main layout
    newCategory.append('<table><tr>');
    newCategory.append('<td>Category:</td><td> <input type="text" id="cat' + categoryIndex + '"> </td>');
    newCategory.append('</tr><tr>');
    newCategory.append('<td>Tags:</td><td> <input type="text" id="tag' + categoryIndex + '"> </td>');
    newCategory.append('</tr></table>');

    // add event listeners
    $('#cat' + categoryIndex).keypress(categorySubmit);
    $('#tag' + categoryIndex).keypress(categorySubmit);

    // set focus to newly created category
    $('#cat' + categoryIndex).focus();

    // keep track of categories
    categoryIndex++;
    categoryCount++;

}



