
$("#course-id-select").change(function () {
    $.ajax({
        url: "GetStudentsForCourseID",
        method: "POST",
        data: { courseId: $(this).val() },
        dataType: "json",
        success: function (data) {
            if (data != null) {
                // first get rid of all current items
                $(".student-list-item").remove();

                // then loop through the data
                $.each(data, function (index, value) {
                    var name = value.FirstName + ' ' + value.LastName;
                    $("#studentList").append(
                        '<li id="li-user-"' + value.ID + '" \
                                data-fullname="' + name + '" class="student-list-item">\
                                    <input class="cb-user" value="' + value.ID + '" \
                                    type="checkbox" name="userId" checked="checked" />' +
                            name +
                        '</li>'
                        );
                });
            }
        }
    });
});

$("#user-show-all").change(function () {
    if (this.checked) {
        $(".cb-user").prop("checked", true);
    }
    else {
        $(".cb-user").prop("checked", false);
    }
});