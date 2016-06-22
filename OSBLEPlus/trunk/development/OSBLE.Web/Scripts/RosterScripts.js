

var clickCounter = 0;

//execute this when button is pressed
//on loading/reloading the page this will execute and populate the variables as needed.
$(document).ready(function () {

    //before anything, sweep the roster
    $.ajax({
        url: '/Roster/SweepRoster/',
        dataType: "json",
        traditional: true,
        data: {  },
        complete: function (data) {
            if (data.responseText === "True") {
                //if the roster sweep changed users, reload page
                window.location.reload();
            }
            else {
                //do nothing
            }
        }
    })

    //first populate the multi section user boxes
    var multiSectionUsers = $("[class=UserLI][section='-1']");

    if (multiSectionUsers.length > 0) {
        var multiSectionUserIDs = Array();

        //this grabs all IDs from the users in the multiple section section
        $.each(multiSectionUsers, function (i, val) {
            var ID = this.querySelectorAll('[id=userID]');
            multiSectionUserIDs.push(ID[0].getAttribute("studentID"));
        });


        $.ajax({
            url: '/Roster/GetMultiSections/',
            dataType: "json",
            traditional: true,
            data: { studentIDs: multiSectionUserIDs },
            complete: function (data) {
                if (data.responseJSON != null) {
                    for (x = 0; x < data.responseJSON.length; x++) {

                        for (y = 0; y < multiSectionUsers[x].childNodes.length; y++) {

                            if (multiSectionUsers[x].childNodes[y].id == "SectionList") {
                                multiSectionUsers[x].childNodes[y].innerHTML = "Sections: " + data.responseJSON[x].substring(0, data.responseJSON[x].length - 1);
                                break;
                            }
                        }
                    }
                }
            }
        })
    }
    //end of multi section box change

    var boxes = document.getElementsByClassName("sectionRoleCheckBox");
    $.each(boxes, function (i, val) {
        this.checked = false;
    });

    //$(function () {
    //    $("#sectionSelection").multiselect();
    //});

    boxes = document.getElementsByClassName("sectionCheckBox");
    $.each(boxes, function (i, val) {
        this.checked = false;
    });
        
    document.getElementById("selectAllUsers").checked = false;

    var students = document.getElementsByClassName("UserLI");

    $.each(students, function (i, val) {
        var check = grabCheckBoxFromStudentLI(this);
        check.checked = false;
        this.style.backgroundColor = ""; //#595959
        this.style.color = "black";
    });
    clickCounter = 0;
});

    
//This will move the selected users to the specified section
function moveUsers() {
    var sectionDestinationList = $('#sectionSelection').val();
    var classID = document.getElementById("data-course-link").getAttribute("data-course-id");
    var courseIDs = grabIDsOfSelected();
    var sectionDestination;

    //if only one is selected, use legacy code
    if (sectionDestinationList.length === 1) {
        sectionDestination = sectionDestinationList[0];
    }

    else { //else, set flag to -1 , -1 signifies there are multiple sections
        sectionDestination = -1;
    }

    var sectionDestinationString;

    if (sectionDestinationList[0] === "-2") {
        sectionDestination = -2;
        sectionDestinationString = "all";
    }
    else
        sectionDestinationString = idsToString(sectionDestinationList);

    EditSections(courseIDs, sectionDestination, sectionDestinationString, classID);
}

//this will use an Ajax call to use the controller method to move the users
function EditSections(userIDs, desiredSection, sectionDestinationString, abstractCourseID) {
    $.ajax({
        url: '/Roster/EditSections/',
        dataType: "json",
        traditional: true,
        data: { ids: userIDs, section: desiredSection, sectionList: sectionDestinationString, courseID: abstractCourseID },
        complete: function (data) {
            if (data === false) {

            }
            else {
                window.location.reload();
            }
        }
    })
}


//mail all selected users
function massMail() {
    var IDs = grabIDsOfSelected();
    var IDString = idsToString(IDs);

    //update element to equal IDString
    $("#emailIDList").val(IDString);

    //submit the form
    $("#emailForm").submit();
}

function massWithdraw(){
    var IDs = grabIDsOfSelected();
    var IDString = idsToString(IDs);

    //update element to equal IDstring
    $("#withdrawIDList").val(IDString);

    //submit the form
    $("#withdrawForm").submit();
}

function massEnroll() {
    var IDs = grabIDsOfSelected();
    var IDString = idsToString(IDs);

    //update element to equal IDstring
    $("#enrollIDList").val(IDString);

    //submit the form
    $("#enrollForm").submit();
}

function massRemove() {
    if (confirm("Are you sure you want to remove these users?") == true) {
        //do nothing
    } else {
        return;
    }

    var IDs = grabIDsOfSelected();
    var IDString = idsToString(IDs);
    //update element to equal idstring
    $("#removeIDList").val(IDString);

    //submit the form
    $("#removeForm").submit();
}
  

//this method grabs the IDs of all selected users
function grabIDsOfSelected() {
    var IDs = Array();
    var students = document.getElementsByClassName("UserLI");

    $.each(students, function (i, val) {
        var temp = grabCheckBoxFromStudentLI(this);

        //if this student is selected, add the ID to the list
        if (temp.checked) {
            var ID = this.querySelectorAll('[id=userID]');
            IDs.push(ID[0].getAttribute("studentID"));
        }
    });

    return IDs;
}

//turns a list into a CSV string
function idsToString(IDs){
    var myStr = ""
    $.each(IDs, function(i, val){
        myStr += this + ",";
    });

    return myStr;
}


//if you click an individual button
function blockClicked(block) {
    var box = grabCheckBoxFromStudentLI(block);

    if (box.checked) {
        changeToUncheck(block, box);
        triggerCheckBox(block, false);
    }
    else {
        changeToCheck(block, box);
        triggerCheckBox(block, true);
    }

    ////check how many students are selected and update page styling
    showOrHideMultiple();
}


//if you click a section button
function roleClicked(roleCheck) {

    var currentSection = roleCheck.getAttribute("section");

    var sectionBlock = roleCheck.parentElement.parentElement;
    var students = sectionBlock.querySelectorAll('[class=UserLI]');

    //select all students that are in this section
    if (roleCheck.checked) {
        $.each(students, function (i, val) {
            var temp = grabCheckBoxFromStudentLI(this);

            changeToCheck(this, temp);
        });
    }

        //deselect all students that are in this section
    else {
        $.each(students, function (i, val) {
            var temp = grabCheckBoxFromStudentLI(this);
            changeToUncheck(this, temp);
        });
    }


    allInSectionOrNot(currentSection);
    allCheckedOrNot();


    //check how many students are selected and update page styling
    showOrHideMultiple();
}

//this method will select/deselect all students
function selectAll(selectAllBox) {
    var allStudents = document.getElementsByClassName("UserLI");

    //if you want to select all students
    if (selectAllBox.checked) {
        $.each(allStudents, function (i, val) {
            var box = grabCheckBoxFromStudentLI(this);
            changeToCheck(this, box);
        });

        $.each(document.getElementsByClassName("sectionRoleCheckBox"), function (i, val) {
            this.checked = true;
        });

        $.each(document.getElementsByClassName("sectionCheckBox"), function (i, val) {
            this.checked = true;
        });
    }

        //if you want to deselect all students
    else {
        $.each(allStudents, function (i, val) {
            var box = grabCheckBoxFromStudentLI(this);
            changeToUncheck(this, box);
        });

        $.each(document.getElementsByClassName("sectionRoleCheckBox"), function (i, val) {
            this.checked = false;
        });

        $.each(document.getElementsByClassName("sectionCheckBox"), function (i, val) {
            this.checked = false;
        });
    }

    showOrHideMultiple();

}

//for each role in the section, check or uncheck users
function sectionCheckChange(sectionCheck) {
    var roles = sectionCheck.parentElement.querySelectorAll('[class=sectionRoleCheckBox]');

    $.each(roles, function (i, val) {
        var students = this.parentElement.parentElement.querySelectorAll('[class=UserLI]');
        if (sectionCheck.checked) {
            this.checked = true;
        }
        else {
            this.checked = false;
        }

        $.each(students, function (x, value) {
            var temp = grabCheckBoxFromStudentLI(this);

            if (sectionCheck.checked) {
                changeToCheck(this, temp);
            }
            else {
                changeToUncheck(this, temp);
            }
        });
    });

    allCheckedOrNot();
    showOrHideMultiple();

}

//changes the studentLI to be checked
function changeToCheck(studentLI, studentCheckBox) {
    if (!studentCheckBox.checked) {
        studentCheckBox.checked = true;
        studentLI.style.backgroundColor = "#A6A6A6"; //#595959
        studentLI.style.color = "white";
        clickCounter++;
    }
}

//changes the studentLI to be unchecked
function changeToUncheck(studentLI, studentCheckBox) {
    if (studentCheckBox.checked) {
        studentCheckBox.checked = false;
        studentLI.style.backgroundColor = ""; //#595959
        studentLI.style.color = "black";
        clickCounter--;
    }
}

//this fetches the hidden checkbox from a studentLI
function grabCheckBoxFromStudentLI(studentLI) {
    for (x = 0; x < studentLI.childNodes.length; x++) {
        if (studentLI.childNodes[x].id == "moveStudentBox") {
            return studentLI.childNodes[x];
        }
    }
    return null;
}

//this changes the styling of the multipleUserActions section
function showOrHideMultiple() {
    var multipleActionHeader = document.getElementById("multipleSelectedAction");

    if (clickCounter > 1) {
        //TODO:: Display the multiple email, the multiple move, multiple withdraw, multiple KARATE CHOP!
        multipleActionHeader.style.backgroundColor = "#dbdbdb";
    }

    else {
        //multipleActionHeader.style = "display: none;";
        multipleActionHeader.style.backgroundColor = "";
    }
}


//this method is when you change one checkbox. Determines if the roleSection checkbox needs to be checked and all other checkboxes.
function triggerCheckBox(studentLI, added) {
    var container = studentLI.parentElement.parentElement; //grab the block which contains
    var students = studentLI.parentElement.childNodes;
    var change = true;
    var box;
    var header;

    //grab the container
    for (x = 0; x < container.childNodes.length; x++) {
        if (container.childNodes[x].id === "sectionRoleHeader") {
            header = container.childNodes[x];
            break;
        }
    }

    //grab the check box
    for (x = 0; x < header.childNodes.length; x++) {
        if (header.childNodes[x].id === "sectionRoleCheckAll") {
            box = header.childNodes[x];
            break;
        }
    }

    //now we have the box and the header
    //if we just checked a user
    if (added === true) {
        $.each(students, function (i, val) {
            var studentBox = grabCheckBoxFromStudentLI(this);

            if (studentBox != null) //not guranteed every child node is a student LI
            {
                if (!studentBox.checked) //if you find a box that's unchecked, return
                {
                    change = false;
                }
            }
        });

        //if you hit here, you're ready to check the box.
        if (change === true) {
            box.checked = true;
            allInSectionOrNot(box.getAttribute("section"));
            allCheckedOrNot();
        }
    }

        //if you remove even one, you need to uncheck the box
    else {
        box.checked = false;
        var sectionBoxes = document.getElementsByClassName("sectionCheckBox");
        for (x = 0; x < sectionBoxes.length; x++) {
            if (sectionBoxes[x].getAttribute("section") === box.getAttribute("section")) {
                sectionBoxes[x].checked = false;
            }
        }
        document.getElementById("selectAllUsers").checked = false;
    }
}

//check if all roleBoxes in a section are checked, if so, check the section box
function allInSectionOrNot(section) {
    var Boxes = document.getElementsByClassName("sectionCheckBox");
    var sectionBox;
    var flag = true;

    for (x = 0; x < Boxes.length; x++) {
        if (Boxes[x].getAttribute("section") === section) {
            sectionBox = Boxes[x];
        }
    }

    Boxes = document.getElementsByClassName("sectionRoleCheckBox");
    $.each(Boxes, function (i, val) {
        if (this.getAttribute("section") === section) {
            if (!this.checked) {
                flag = false;
            }
        }
    });

    if (flag === true) {
        sectionBox.checked = true;
    }
    else {
        sectionBox.checked = false;
    }
}

//whenever you check or uncheck a box, call this function to see if the select all box needs to change.
function allCheckedOrNot() {
    allBoxes = document.getElementsByClassName("sectionRoleCheckBox");
    var allow = true;
    $.each(allBoxes, function (i, val) {
        if (!this.checked) {
            allow = false;
        }
    });

    if (allow === true) {
        document.getElementById("selectAllUsers").checked = true;
    }
    else {
        document.getElementById("selectAllUsers").checked = false;
    }
}
