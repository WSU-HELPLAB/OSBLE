// ********** DELIVERABLE **********

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
});

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
            databaseId: 0
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
        newDeliverable.append('<input type="hidden" class="deliverable_id" value="' + deliverableData.databaseId + '" />');

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
        $(this).find('.deliverable_id').first().attr('name', 'Assignment.Deliverables[' + i.toString() + '].DatabaseId');
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

    $('#manual_config_options').show();
    $("input[name='line_review_options']:eq(0)").attr('checked', 'checked');

    $('#InstructorCanReview').change(function () {
        if ($(this).attr('checked')) {
            $('#line_review_config').show('blind');
            // first time it is checked only
            if (categoryCount == 0) {
                addNewCategory();
            }
        }
        else {
            $('#line_review_config').hide('blind');
            // when unchecking inline comments should remove all contents
            clearInLineCategories();
        }
    });

    $('#add_new_category').click(function () {
        addNewCategory();
        return false;
    });

    $('#comment_category_selection option:eq(0)').attr('selected', 'selected');
    $('#comment_category_selection').change(switchCommentConfigListener);
    switchCommentConfig($('#comment_category_selection option:eq(0)').val());

    if ($("input[name='line_review_options']:eq(1)").attr('checked') != 'checked') {
        $("input[name='line_review_options']:eq(0)").attr('checked', 'checked');
    }
});

// apply this function in an event listener to every text box to prevent accidental submission of the whole form by the user
function disableSubmit(e) {
    // if the return key is pressed, do nothing
    if (e.which == 13) {
        return false;
    }
}

function switchCommentConfigListener() {
    switchCommentConfig($(this).val());
}
function switchCommentConfig(id) {
    $("#comment_config_samples").children().hide();
    $("#comment_sample_" + id).show('blind');
}

function addCategory(index, catName, optionName) {
    catName = typeof (catName) != 'undefined' ? catName : "";
    optionName = typeof (optionName) != 'undefined' ? optionName : "";

    var categoryId = 'category_' + index;
    var categoryOptionIdPrefix = 'category_option_' + index;
    var dataId = 'option_data_' + index;
    var addOptionId = 'add_option_' + index;

    // check limit
    if (categoryCount >= MAX_CATEGORIES) {
        return false;
    }

    // create new item
    $('#category_data').append('<div id="' + categoryId + '" class="deliverable" />');
    var newCategory = $('#' + categoryId);

    // delete button
    newCategory.append('<div class="deliverable_tools"><a href="#" title="Delete this deliverable" onclick="$(this).parent().parent().hide(\'highlight\', function () { $(this).remove(); $(\'#add_new_category_div\').animate({ \'height\': \'show\' }, { duration: 0 }); }); categoryCount--; return false;"><img src="/Content/images/delete_up.png" alt="Delete Button" /></a></div>');

    // main layout
    newCategory.append('<table><tr>');
    newCategory.append('<td>Category name:</td><td> <input type="text" id="' + categoryId + '" name="' + categoryId + '" value="' + catName + '"> </td>');
    newCategory.append('</tr><tr>');
    newCategory.append('<td>Options:</td><td></td></tr><tr> ');

    //
    newCategory.append('<td><a id="' + addOptionId + '" title="Add new option" style="text-decoration:none;cursor:pointer;"> <img src="/Content/images/add_up.png" alt="(+)" /> Add new option </a> </td>'); // must be all one line to work
    newCategory.append('</tr><tr><td colspan="2"><div id="' + dataId + '"> <input type="text" id="' + categoryOptionIdPrefix + '_0" name="' + categoryOptionIdPrefix + '_0" value="' + optionName + '"> <br />');
    // required first option
    newCategory.append('');

    newCategory.append('</div><td></tr></table>');

    // add event listeners
    $('#' + categoryId).keypress(disableSubmit);
    $('#' + categoryOptionIdPrefix + '_0').keypress(disableSubmit);

    $('#add_option_' + categoryIndex).click({ catIndex: categoryIndex }, addNewCategoryOption);

    // set focus to newly created category
    $('#' + categoryId).focus(); // doesn't work, but I don't want to change any names

    return true;
}

function addCategoryOption(categoryIndex, optionIndex, optionName) {
    optionName = typeof (optionName) != 'undefined' ? optionName : "";
    var dataId = 'option_data_' + categoryIndex;
    var categoryOptionIdPrefix = 'category_option_' + categoryIndex;

    var d = $('#' + dataId);

    // all one line because append adds closing tags automatically if there isn't a closing tag (ie </div>) within the string it is appending :/
    d.append('<div><input type="text" id="' + categoryOptionIdPrefix + '_' + optionIndex +
             '" name="' + categoryOptionIdPrefix + '_' + optionIndex +
             '" value="' + optionName + '"> <div style="display: inline; position:relative; top:0.25em;"><a href="#" tabindex="9000' +
             optionIndex + '" title="Delete This Option" ' +
             'onclick="$(this).parent().parent().hide(\'highlight\', function () { $(this).remove() }); categoryOptionIndex[' + categoryIndex + ']--; return false;"><img src="/Content/images/delete_up.png" alt="Delete" /></a></div> </div>');

    $('#' + categoryOptionIdPrefix + '_' + optionIndex).focus();
    $('#' + categoryOptionIdPrefix + '_' + optionIndex).keypress(disableSubmit);

    return true;
}

function addNamedCategory(catName, optionName) {
    var result = addCategory(categoryIndex, catName, optionName);

    //false result indicates that the row wasn't created successfully
    if (!result) {
        return;
    }

    // keep track of indices
    categoryIndex++;
    categoryCount++;
}

function addNamedCategoryOption(index, optionName) {
    var categoryIndex = index;
    addCategoryOption(categoryIndex, categoryOptionIndex[categoryIndex], optionName);
    categoryOptionIndex[categoryIndex]++;
}

function addNewCategory() {
    var result = addCategory(categoryIndex);
    
    //false result indicates that the row wasn't created successfully
    if (!result) {
        return;
    }

    // keep track of indices
    categoryIndex++;
    categoryCount++;

    //Hides the addCategory button once you have created the max amount of them
    if (categoryCount >= MAX_CATEGORIES) {
        $('#add_new_category_div').animate({ "height": "hide" }, { duration: 0 });
    } else {
        $('#add_new_category_div').animate({ "height": "show" }, { duration: 0 });
    }
}

function addNewCategoryOption(e) {
    var categoryIndex = e.data.catIndex;
    addCategoryOption(categoryIndex, categoryOptionIndex[categoryIndex]);
    categoryOptionIndex[categoryIndex]++;
}

// when unselect the inline checkbox clears all the categories in the drop down.
function clearInLineCategories() {
    var count = 0;
    var categoryId;
    do {
        categoryId = 'category_' + count;
        $('#' + categoryId).remove();
    } while (++count < categoryCount);

    // reseting the counters
    categoryIndex = 0;
    categoryCount = 0;
}
