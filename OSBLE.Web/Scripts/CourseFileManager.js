// Created by Evan Olds for the OSBLE project at WSU

// Dependencies: CourseFilesUploader.js
// Important notes:
//    Only one file manager per page is currently allowed

// Global array for state objects
var cfm_states = new Array();

// Check wether or not a files or links was clicked.
var FilesAndLinksClick = false;
var ContextMenu = false;

//For user selection purposes
var cfm_statesSelected = new Array();
var cfm_boolMultipleSelected = false;

// Call this function to start the asynchronous request for a file listing.
// When the request completes the inner HTML of the DIV with the specified 
// ID will be set.
function cfm_getListing(targetDIVID) {
    // Get the current course ID
    var courseID = GetSelectedCourseID();

    cfm_states = new Array();

    // Find the DIV
    var theDIV = document.getElementById(targetDIVID);

    // Set the innerHTML to a loading message
    theDIV.innerHTML = "(refreshing file manager...)";

    // Do the service request to get the file list for this course
    var req = new XMLHttpRequest();
    req.addEventListener("load",
        function (args) { cfm_listcompletion(args, targetDIVID); },
        false);
    req.addEventListener("error",
        function (args) { document.getElementById(targetDIVID).innerHTML = "(error)"; },
        false);
    //req.addEventListener("abort", assignmentfilemanager_canceled, false);
    req.open("GET", "../Services/CourseFilesOps.ashx?cmd=course_files_list&courseID=" + courseID +
        "&force_nocache=" + (new Date()).toString());
    req.setRequestHeader("Cache-control", "no-cache");
    req.send();

}

function cfm_listcompletion(args, targetDIVID) {

    // Find the DIV
    var theDIV = document.getElementById(targetDIVID);

    // Make sure the service returned valid XML
    var doc = args.target.responseXML;
    if (null == doc) {
        theDIV.innerHTML = "Error: XML document from service response is null! " +
            "Please contact support for help with this issue.";
        return;
    }

    // DEBUG
    //alert(args.target.responseText);

    var root = doc.firstChild;
    if (null == root) {
        theDIV.innerHTML = "Error: Root element in XML document from service is null! " +
            "Please contact support for help with this issue.";
        return;
    }

    // The root will have a success="true" attribute if everything went ok
    if ("true" != root.getAttribute("success")) {
        theDIV.innerHTML = "(update failed, please refresh the page and contact support if the problem persists)";
        return;
    }
    else {
        // Find the <file_list> node
        var lists = doc.getElementsByTagName("file_list");
        if (0 == lists.length) {
            alert("Update failed (invalid XML returned)");
            return;
        }

        var listNode = lists[0];
        //theDIV.innerHTML = cfm_MakeDIV(listNode, "", "padding: 0px;", -1, targetDIVID);

        var cookieExists = $.cookie('fileSystemCookie');
        if (cookieExists == null) {
            $.cookie('fileSystemCookie', "root");
        }

        // using a partial view makes things much easier to understand
        var xml = encodeURIComponent(new XMLSerializer().serializeToString(listNode));
        $("#" + targetDIVID).load('/Home/FilesAndLinks', { xmlString: xml }, cfm_retrieveListingStatus);

        //Retrieve the file listings for the current user.
        //Checks the user's cookies to see what directories they had expanded and expands them.
        //cfm_retrieveListingStatus();
    }
}

function cfm_MakeDIV(listNode, relativeDir, styleString, parentStateIndex, targetDIVID) {

    var result = "<div id=\"content_of_" + parentStateIndex.toString() +
        "\" style=\"" + styleString + "\">";

    // Get the current course ID
    var courseID = GetSelectedCourseID();

    var subdir_count = cfm_CountChildrenWithName(listNode, "folder");
    var file_count = cfm_CountChildrenWithName(listNode, "file");
    var j = 0;

    // Go through any folders (subdirectories) first
    for (var i = 0; i < listNode.childNodes.length; i++) {
        var tempNode = listNode.childNodes[i];
        if (null == tempNode || 1 != tempNode.nodeType || "folder" != tempNode.nodeName) { continue; }

        var folderName = tempNode.getAttribute("name");
        var folderPath = relativeDir + "/" + folderName;
        // Special case for root
        if ("/" == folderName) {
            folderName = "Files and Links";
            folderPath = "/";
        }
        var canUploadTo = tempNode.getAttribute("can_upload_to");
        if (null != canUploadTo && "true" == canUploadTo.toLowerCase()) { canUploadTo = true; }
        else { canUploadTo = false; }
        var canDelete = tempNode.getAttribute("can_delete");
        if (null != canDelete && "true" == canDelete.toLowerCase()) { canDelete = true; }
        else { canDelete = false; }

        // Each folder has a state object associate with it
        var stateObj = {
            arrayIndex: cfm_states.length,
            allowsDeletion: canDelete,
            allowsUploads: canUploadTo,
            allowsCollapsing: true, // For now everything can be collapsed
            controlsVisible: false,
            expanded: false,
            fm_div_ID: targetDIVID,
            isFolder: true,
            name: folderName,
            parentIndex: parentStateIndex,
            targetFolder: folderPath,
            getExpanderImgSrc: function () {
                if (this.expanded) {
                    return "/Content/images/arrow_down.png";
                }
                else {
                    return "/Content/images/arrow_right.png";
                }
            }
        };

        // Put it in the global array
        var stateObjIndex = cfm_states.length;
        cfm_states[stateObjIndex] = stateObj;

        var ss = stateObjIndex.toString();

        var theStyle

        theStyle = "padding: 3px; border: 0px;";

        // One new DIV for the folder name 
        if (folderName == "Files and Links") {

            //If the user has control over files and links add controls to the folder div.
            //Controls are added to the div by making the div part of the context-menu class.
            //There are three types of context menus one for root, one for a file, and one for a directory
            /*if (canUploadTo == true) {
                result += "<div class=\"context-menu-three box menu-1\" state-obj=\"" + stateObjIndex.toString() + "\" id=\"folder_div_" + ss + "\" style=\"" + theStyle + "\" name=\"Files and Links\">";
            }
            else {
                result += "<div state-obj=\"" + stateObjIndex.toString() + "\" id=\"folder_div_" + ss + "\" style=\"" + theStyle + "\" name=\"Files and Links\">";
            }*/
            result += '<div state-obj="' + stateObjIndex.toString() + '" id="folder_div_' + ss + '" style="' + theStyle + '" name="Files and Links">';
        }
        else {
            //If user has control over files and links add controls to the div
            if (canUploadTo == true && canDelete == true) {
                result += "<div class=\"context-menu-one box menu-1\" state-obj=\"" + stateObjIndex.toString() + "\" id=\"folder_div_" + ss + "\" style=\"" + theStyle + "\" name=\"Folders\" folder-name=\"" + folderName + "\">";
                result += "<div id=\"stateSelectID_" + stateObjIndex.toString() + "\" class=\"itemSelection\" state-obj-select=\"" + stateObjIndex.toString() + "\" file-or-folder=\"folder\" folder-name=\"" + folderName + "\">";
            }
            else {
                result += "<div state-obj=\"" + stateObjIndex.toString() + "\" id=\"folder_div_" + ss + "\" style=\"" + theStyle + "\" name=\"Folders\" folder-name=\"" + folderName + "\">";
                result += "<div id=\"stateSelectID_" + stateObjIndex.toString() + "\" class=\"itemSelection\" state-obj-select=\"" + stateObjIndex.toString() + "\" file-or-folder=\"folder\" folder-name=\"" + folderName + "\">";
            }
        }
        if (folderPath != "/") {
            result += "<table width=100%; id=\"folder_text_" + ss + "\" style=\"" + " table-layout: fixed; " + "\">";
            result += "<tr>";
            result += "<td style=\"" + " width: 100%; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; " + "\">";
            if (stateObj.allowsCollapsing) {
                if (folderName == "Files and Links") {
                    result += "<h3 class=\"files_links_header\" style=\"-webkit-margin-before: .60em; -webkit-margin-after: .5em;\">Files and Links </h3>"
                                + "<span><a  class=\"root_context_menu\" href=\"#\" > <img width=\"19\" height=\"19\" class=\"files_links_tooltip\" alt=\"(?)\" src=\"../../Content/images/folder_plus.png\"></a></span>"
                                + "<span><a  class=\"files_links\" href=\"#\" > <img width=\"19\" height=\"19\" class=\"files_links_tooltip\" alt=\"(?)\" src=\"../../Content/images/tooltip/109_AllAnnotations_Help_19x19_72.png\">"
                                + "<span id=\"files_links_tooltip\"><p class=\"files_links_p\">Right clicking \"Files and Links\" or on uploaded Files/Folders will bring up a dialog for the current course file manager</p></span></a> </span>";
                } else {
                    result += "<a style=\"cursor: pointer;\" title=\"" + folderName + "\" onclick=\"cfm_expand_collapse(" + ss + ");\">";
                    result += "<img id=\"expander_img_" + ss + "\" src=\"" + stateObj.getExpanderImgSrc() + "\" />&nbsp;" + folderName;
                    // commented line is to put a folder+ icon to allow left click menu like the one next to files and links, not working currently due to undefined opt.$trigger when implementing this feature
                    //+ "<span><a  class=\"root_context_subfolder\" href=\"#\" > <img width=\"19\" height=\"19\" class=\"files_links_tooltip\" alt=\"(?)\" src=\"../../Content/images/folder_plus.png\"></a>";
                    result += "</a>";
                }
            }
            else { result += folderName; }
            result += "</td>";
            result += "</tr>";
            result += "</table>";
            result += "</div>";

            if (folderName != "Files and Links") {
                result += "</div>";
            }
        }

        // Then the recursive call makes another DIV for the files (provided there are some)
        if (cfm_CountChildrenWithName(tempNode, "file") > 0 ||
            cfm_CountChildrenWithName(tempNode, "folder") > 0) {
            result += cfm_MakeDIV(tempNode, folderPath,
                "padding: 0px; margin-left: 10px; display: none;",
                stateObjIndex, targetDIVID);
        }

        j++;
    }

    // Go through the list of files
    j = 0;
    for (var i = 0; i < listNode.childNodes.length; i++) {
        var tempNode = listNode.childNodes[i];
        if (null == tempNode || 1 != tempNode.nodeType || "file" != tempNode.nodeName) { continue; }

        var fileName = tempNode.getAttribute("name");
        var imgSrc = "/Content/images/fileextimages/_blank.png";
        var lastDotIndex = fileName.lastIndexOf(".");
        var ext = "";
        if (-1 != lastDotIndex) {
            ext = fileName.substr(lastDotIndex);
        }
        ext = ext.toLowerCase();

        // Determine relevant permissions
        var canDelete = tempNode.getAttribute("can_delete");
        if (null != canDelete && "true" == canDelete.toLowerCase()) { canDelete = true; }
        else { canDelete = false; }

        var theFullPath = relativeDir;
        if (null == theFullPath || 0 == theFullPath.length) {
            theFullPath = "/" + fileName;
        }
        else {
            if ("/" == relativeDir) {
                theFullPath = "/" + fileName;
            }
            else {
                theFullPath = relativeDir + "/" + fileName;
            }
        }

        // Each file also has a state object associated with it. The states are very 
        // similar to the folder object states, but not identical. They do however 
        // go in the same global array.
        var fileStateObj = {
            arrayIndex: cfm_states.length,
            allowsDeletion: canDelete,
            controlsVisible: false,
            fm_div_ID: targetDIVID,
            fullPath: theFullPath,
            isFolder: false,
            name: fileName,
            parentIndex: parentStateIndex,
            targetFolder: relativeDir
        };

        // Put it in the global array
        var stateObjIndex = cfm_states.length;
        cfm_states[stateObjIndex] = fileStateObj;

        // Determine an icon based on the file extension
        if (".aac" == ext || ".ai" == ext || ".aiff" == ext || ".avi" == ext ||
            ".bmp" == ext || ".c" == ext || ".cpp" == ext || ".css" == ext ||
            ".dat" == ext || ".dmg" == ext || ".doc" == ext || ".dotx" == ext ||
            ".dwg" == ext || ".dxf" == ext || ".eps" == ext || ".exe" == ext ||
            ".flv" == ext || ".gif" == ext || ".h" == ext || ".hpp" == ext ||
            ".html" == ext || ".ics" == ext || ".iso" == ext || ".java" == ext ||
            ".jpg" == ext || ".key" == ext || ".mid" == ext || ".mp3" == ext ||
            ".mp4" == ext || ".mpg" == ext || ".odf" == ext || ".ods" == ext ||
            ".odt" == ext || ".otp" == ext || ".ots" == ext || ".ott" == ext ||
            ".pdf" == ext || ".php" == ext || ".png" == ext || ".ppt" == ext ||
            ".psd" == ext || ".py" == ext || ".qt" == ext || ".rar" == ext ||
            ".rb" == ext || ".rtf" == ext || ".sql" == ext || ".tga" == ext ||
            ".tgz" == ext || ".tiff" == ext || ".txt" == ext || ".wav" == ext ||
            ".xls" == ext || ".xlsx" == ext || ".xml" == ext || ".yml" == ext ||
            ".zip" == ext) {
            imgSrc = "/Content/images/fileextimages/" + ext.substr(1) + ".png";
        }

        // Determine an ID for the file's DIV that has both the index of the parent 
        // folder's state object and the index of this file
        var theID = "file" + j.toString() + "_folderState" + parentStateIndex.toString();
        j++;

        // Make a link for the file downloader service
        // Note that we need relative path in filename for files in folders
        var linkURL = "/FileHandler/CourseDocument?courseId=" +
            courseID.toString() + "&filePath=" + fileStateObj.fullPath;

        var copyLink = linkURL.toString();
        //If the user has control over the Files and Links
        //then add in the controls by making the file div a member of the context-menu class, same as above
        if (fileStateObj.allowsDeletion) {
            result += "<div id=\"stateSelectID_" + stateObjIndex.toString() + "\" class=\"itemSelection\" state-obj-select=\"" + stateObjIndex.toString() + "\" file-or-folder=\"file\" copy-link=\"" + copyLink + "\" >";
            result += "<div class=\"context-menu-two box menu-1\" state-obj=\"" + stateObjIndex.toString() + "\" id=\"" + theID + "\" style=\"padding: 3px;";
        }
        else {
            result += "<div id=\"stateSelectID_" + stateObjIndex.toString() + "\" class=\"itemSelection\" state-obj-select=\"" + stateObjIndex.toString() + "\" file-or-folder=\"file\"  >";
            result += "<div state-obj=\"" + stateObjIndex.toString() + "\" id=\"" + theID + "\" style=\"padding: 3px; ";
        }

        //if (0 == i % 2) { result += "#ffffff; "; }
        //else { result += "#ffffff; "; }
        result += "border: 0px;";
        result += "\">";

        result += "<table width=100%; id=\"folder_text_" + ss + "\" style=\"" + " table-layout: fixed; " + "\">";
        result += "<tr class=\"selector\">";
        result += "<td style=\"" + " width: 100%; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; " + "\">";
        result += ("<a title=\"" + fileName + "\" href=\"" + linkURL + "\"><img src=\"" + imgSrc + "\" />" + fileName + "</a>");
        result += "</td>";
        result += "</tr>";
        result += "</table>";
        result += "</div>";
        result += "</div>";

    }
    result += "</div>";

    return result;
}

// Functions below here are in alphabetical order. Keep them that way.

function cfm_AddUploader(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //To tell the highlighting that the context menu was clicked.
    ContextMenu = true;

    // Find the appropriate DIV
    var targetDIV = document.getElementById("folder_controls_" + stateObjectIndex.toString());

    // Add the uploader control into the DIV
    $('body').append(
"<div id=\"input_dialog\" title=\"Upload File(s)\"> \ " +
    "<p style=\"font-size: small;\">NOTE: all special characters except '-' and '_' will be stripped from file names.</p>" +
    "Upload files to: " + state.name +
    "<br />" + fileuploader_getcontrolshtml(
        "cfm_files_" + stateObjectIndex.toString(), true,
        "cfm_GetExtraServiceArgs(" + stateObjectIndex.toString() + ");",
        "cfm_uploadComplete(" + stateObjectIndex.toString() + ");") +
        "<input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
        "onclick=\"removePopUp()\");\" />" +
"</div>");

    //Settings for the pop up dialog box
    $('#input_dialog').dialog({
        modal: true,
        autoOpen: true,
        resizable: false,
        width: 350,
        height: 250,
        closeOnEscape: true,
        close: removePopUp
    });

    //Open the dialog box
    $('#input_dialog').dialog('open');

    // Find the submit button and make its width 100%
    var btnUpload = document.getElementById("btnSubmit_cfm_files_" + stateObjectIndex.toString());
    if (btnUpload) {
        btnUpload.style.width = "100%";
    }

    // Mark the controls as visible
    state.controlsVisible = true;
}

//Once the upload finsishes
function cfm_uploadComplete(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];
    // Refresh the listing
    cfm_getListing(state.fm_div_ID);
}


// Counts the number of child nodes under listNode that have a node type of 1 and 
// a node name that matches node_name.
function cfm_CountChildrenWithName(listNode, node_name) {
    var count = 0;
    for (var i = 0; i < listNode.childNodes.length; i++) {
        var tempNode = listNode.childNodes[i];
        if (null != tempNode && 1 == tempNode.nodeType && node_name == tempNode.nodeName) {
            count++;
        }
    }
    return count;
}

function cfm_CreateFolder(stateObjectIndex) {
    

    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Get the current course ID
    var courseID = GetSelectedCourseID();

    // Find the textbox and get the folder name
    var tb = document.getElementById("tbSubfolder_" + stateObjectIndex.toString());
    if (null == tb) {
        // Problem, but we can't do much about it
        cfm_hideControls(stateObjectIndex);
        return;
    }

    // Folder name cannot be empty
    if (0 == tb.value.length) {
        tb.focus();
        return;
    }

    // Get the "full" path for the folder to be created
    var name = state.targetFolder;
    if ("/" != name.substr(name.length - 1) &&
        "\\" != name.substr(name.length - 1)) {
        name += "/";
    }
    //clear out any special characters from file name
    name += tb.value.replace(/[^a-z0-9]+/gi, " ");

    // Make an XML HTTP request to the service. The service will return an 
    // updated file listing in response to the folder creation request, 
    // provided it succeeds.
    var req = new XMLHttpRequest();
    req.addEventListener("load",
        function (args) {
            cfm_listcompletion(args, state.fm_div_ID);
        },
        false);
    req.addEventListener("error",
        function (args) { alert("Folder creation error"); },
        false);
    req.open("POST", "../Services/CourseFilesOps.ashx?cmd=create_folder&courseID=" +
        courseID + "&folder_name=" + name);
    req.send();

    //Remove pop up dialog box
    removePopUp();

    //cfm_expand_collapseRoot();

    //Expand any previously expanded directories after the file listing is complete.
    cfm_retrieveListingStatus();
}

function cfm_CreateFolderIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //To tell the highlighting that the context menu was clicked.
    ContextMenu = true;

    // Find the appropriate DIV
    var targetDIV = document.getElementById("folder_controls_" + stateObjectIndex.toString());

    //// Add the creation controls into the DIV
    $('body').append(
"<div id=\"input_dialog\" title=\"Create Folder\"> " +
        "<p style=\"font-size: small;\">NOTE: all special characters except '-' and '_' will be stripped from the folder name.</p>" +
        "<br />Create Folder:&nbsp;" +
        "<input type=\"text\" id=\"tbSubfolder_" + stateObjectIndex.toString() + "\" />" +
        "<br /><input type=\"button\" value=\"Create\" style=\"width: 100%;\" " +
        "onclick=\"cfm_CreateFolder(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
        "onclick=\"removePopUp()\");\" />" +
"</div>");
    

    $('#input_dialog').dialog({
        modal: true,
        autoOpen: true,
        resizable: false,
        width: 300,
        height: 250,
        closeOnEscape: true,
        close: removePopUp
    });

    $('#input_dialog').dialog('open');

    // Mark the controls as visible
    state.controlsVisible = true;
}

function cfm_DeleteFile(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Get the current course ID
    var courseID = GetSelectedCourseID();

    // Make an XML HTTP request to the service
    var req = new XMLHttpRequest();
    req.addEventListener("load",
        function (args) {
            cfm_listcompletion(args, state.fm_div_ID);
        },
        false);
    req.addEventListener("error",
        function (args) { alert("File deletion error"); },
        false);
    req.open("POST", "../Services/CourseFilesOps.ashx?cmd=delete_file&courseID=" +
        courseID + "&file_name=" + state.fullPath);
    req.send();

    //Remove pop up dialog box
    removePopUp();

    cfm_expand_collapseRoot();

    //Expand any previously expanded directories after the file listing is complete.
    cfm_retrieveListingStatus();
}

function cfm_DeleteFolder(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //var element = document.getElementById("stateSelectID_" + stateObjectIndex);
    //var elementTitle = $(element).attr("folder-name");

    // Get the current course ID
    var courseID = GetSelectedCourseID();

    // Make an XML HTTP request to the service
    var req = new XMLHttpRequest();
    req.addEventListener("load",
        function (args) {
            cfm_listcompletion(args, state.fm_div_ID);
        },
        false);
    req.addEventListener("error",
        function (args) { alert("Folder deletion error"); },
        false);
    req.open("POST", "../Services/CourseFilesOps.ashx?cmd=delete_folder&courseID=" +
        courseID + "&folder_name=" + state.targetFolder);
    req.send();

    //Remove pop up dialog box
    removePopUp();

    //cfm_expand_collapseRoot();

    //If the directory was expanded remove it from the cookie string
    cfm_removeCookie(stateObjectIndex);

    //Expand any previously expanded directories after the file listing is complete.
    cfm_retrieveListingStatus();
}

function cfm_DeleteFolderIconClicked(stateObjectIndex) {

    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //To tell the highlighting that the context menu was clicked.
    ContextMenu = true;

    // Find the appropriate DIV
    var targetDIV = document.getElementById("folder_controls_" + stateObjectIndex.toString());

    //// Add the confirmation controls into the DIV
    $('body').append(
"<div id=\"input_dialog\" title=\"Delete Folder\"> \
        <br />Are you sure you want to delete this folder and all its contents?" +
        "<br /><input type=\"button\" value=\"Yes, delete\" style=\"width: 100%;\" " +
        "onclick=\"cfm_DeleteFolder(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"No, cancel\" style=\"width: 100%;\" " +
        "onclick=\"removePopUp()\" />" +
"</div>");

    //Seetings for the pop up dialog box
    $('#input_dialog').dialog({
        modal: true,
        autoOpen: true,
        resizable: false,
        width: 225,
        height: 225,
        closeOnEscape: true,
        close: removePopUp
    });

    //Create the pop up dialog box
    $('#input_dialog').dialog('open');

    // Mark the controls as visible
    state.controlsVisible = true;
}

//This function finds and expands the root, used for making it always expanded.
function cfm_expand_collapseRoot() {

    var elements = document.getElementsByName('Files and Links');
    var root = elements[0];

    var rootNum = $(root).attr('state-obj');

    cfm_expand_collapse(rootNum, true);

}

//This function retrieves how the files and links folder was last listed.
//Any expanded directories are retrieved from the cookie and are expanded.
//Any exapnded directory will be listed in the cookie.
//Each directory is seperated by a comma in the cookie
function cfm_retrieveListingStatus() {

    //Check to see if a user has a cookies file 
    var cookieExists = $.cookie('fileSystemCookie');

    //If the files and links cookie doesn't exist create it
    if (cookieExists == null) {
        $.cookie('fileSystemCookie');
    }
    else {
        //Get the cookie
        tmpString = cookieExists.toString();

        //Split the string retrieved from the cookie into parts seperated by a comma
        //Directories are seperated by commas, any expanded directory will be listed in the cookie
        var tmpArray = tmpString.split(',');

        //For each entry in the cookie check to see if there is a corresponding element
        //in the currently open document
        for (var j = 0; j < tmpArray.length; j++) {
            if (tmpArray[j] != "" && tmpArray[j] != "root") {
                var cookieElement = tmpArray[j];
                /*
                //Check each element
                for (var i = 0; i < elements.length; i++) {
                    var tmpElement = elements[i];
                    var tmpName = $(tmpElement).attr('folder-name');
                    //If the element is found expand it
                    if (tmpName == cookieElement) {
                        var foundElementId = $(tmpElement).attr('state-obj');
                        cfm_expand_collapse(foundElementId, true);
                        boolFound = true;
                    }
                }*/
                var folder = $('#folder_' + cookieElement);
                if (folder.length > 0) {
                    cfm_expand_collapse(Number(cookieElement), true);
                }
                else {
                    //If there is no corresponding element to the cookie entry
                    //Remove that entry from the cookie
                    cfm_removeCookie(cookieElement);
                }

            }
        }
    }
}

//Add an entry to the cookie.
//Entries are based on the directorie's name.
//Each entry is seperate by a comma in the cookie
function cfm_addCookie(cookieName) {
    //Check to see if a user has a cookies file 
    var cookieExists = $.cookie('fileSystemCookie');
    if (cookieExists != null) {
        //If there is a cookie file add the entry
        var tmpString = $.cookie('fileSystemCookie');
        tmpString = tmpString.toString();
        tmpString = tmpString + "," + cookieName;
        $.removeCookie('fileSystemCookie');
        $.cookie('fileSystemCookie', tmpString);
    }

}

//Remove an entry from the cookie
function cfm_removeCookie(cookieName) {
    //Check to see if a user has a cookies file 
    var cookieExists = $.cookie('fileSystemCookie');
    if (cookieExists != null) {
        var tmpString = $.cookie('fileSystemCookie');
        //Get list of entires and split them into an array
        tmpString = tmpString.toString();
        var tmpArray = tmpString.split(',');

        //Try and find the cookie entry in the list
        var indexOfCookie = tmpArray.indexOf(cookieName);

        //If the entry is found indexOf will return greater than -1
        if (indexOfCookie > -1) {

            //Remove the entry from the cookie list.
            tmpArray.splice(indexOfCookie, 1);

            //Rebuild the cookie list.  Make sure the first entry is correct
            if (tmpArray[1] != "" && tmpArray[0] != null) {
                if (tmpArray[0] == "" && tmpArray[1] != null) {
                    var finalCookie = tmpArray[1];
                }
                else {
                    var finalCookie = tmpArray[0];
                }
            }

            //Rebuild the cookie list.
            for (var i = 0; i < tmpArray.length; i++) {
                if (tmpArray[i] != "") {
                    if (tmpArray[i] != "root") {
                        finalCookie = finalCookie + "," + tmpArray[i];
                    }
                }
            }

            //Replace the old cookie list with the new one.
            $.removeCookie('fileSystemCookie');
            $.cookie('fileSystemCookie', finalCookie);
        }
    }

}

function cfm_expand_collapse(stateObjectIndex, cookies) {
    
    if (stateObjectIndex <= 0)
        return;

    var id = '#folder_' + stateObjectIndex;
    var folder = $(id);
    if (folder.length == 0)
        return;

    var arrow = folder.find('.expArrow').first();
    var state = cfm_states[stateObjectIndex];

    if (folder.hasClass("folder-collapsed")) {
        // open the folder
        folder.removeClass("folder-collapsed").addClass("folder-open");
        // point arrow down
        arrow.removeClass("glyphicon-triangle-right").addClass("glyphicon-triangle-bottom");
        //update state
        state.expanded = true;
    }
    else {
        // close the folder
        folder.removeClass("folder-open").addClass("folder-collapsed");
        // point arrow down
        arrow.removeClass("glyphicon-triangle-bottom").addClass("glyphicon-triangle-right");
        //update state
        state.expanded = false;
    }
    
    var cookieTitle = stateObjectIndex.toString();
    if (cookies == null && cookieTitle != null) {
        if (state.expanded) {
            cfm_addCookie(cookieTitle);
        } else {
            cfm_removeCookie(cookieTitle);
        }
    }

    /*
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];
    var ss = stateObjectIndex.toString();

    // Get the expander image
    var theIMG = document.getElementById("expander_img_" + ss);

    var display = "block";
    if (state.expanded) {
        display = "none";
    }
    var contentDIV = document.getElementById("content_of_" + ss);
    if (null != contentDIV) {
        contentDIV.style.display = display;
    }

    var cookieElement = document.getElementById("stateSelectID_" + ss);
    var cookieTitle = $(cookieElement).attr('folder-name');

    if (cookies == null && cookieTitle != "Files and Links" && cookieTitle != null) {
        if (state.expanded == false) {
            cfm_addCookie(cookieTitle);
        } else {
            cfm_removeCookie(cookieTitle);
        }
    }

    // Update the state
    state.expanded = !state.expanded;

    // Update the source of the image now that the new state is set
    if (null != theIMG) { theIMG.src = state.getExpanderImgSrc(); }
    */
}

function cfm_FileDeleteIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //To tell the highlighting that the context menu was clicked.
    ContextMenu = true;

    // Find the appropriate DIV
    var targetDIV = document.getElementById("file_controls_" + stateObjectIndex.toString());

    //// Add the controls into the DIV
    $('body').append(
"<div id=\"input_dialog\" title=\"Delete\"> \
        <br /><div>Are you sure you want to delete?</div>" +
        "<input type=\"button\" value=\"Yes, delete\" style=\"width: 100%;\" " +
        "onclick=\"cfm_DeleteFile(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"No, Cancel\" style=\"width: 100%;\" " +
        "onclick=\"removePopUp()\");\" />\
</div>");

    //make the div we just created into a dialog box
    $('#input_dialog').dialog({
        modal: true,
        autoOpen: true,
        resizable: false,
        width: 245,
        height: 225,
        closeOnEscape: true,
        close: removePopUp
    });

    //Create the dialog box
    $('#input_dialog').dialog('open');
}

function cfm_FileRenameIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //To tell the highlighting that the context menu was clicked.
    ContextMenu = true;

    // Find the appropriate DIV
    var targetDIV = document.getElementById("file_controls_" + stateObjectIndex.toString());

    //// Add the controls into the DIV
    var name = state.name;
    if (name.contains('.'))
        name = name.substr(0, name.lastIndexOf('.'));
    $('body').append(
"<div id=\"input_dialog\" title=\"Rename\"> \
        <br /><div><input type=\"text\" id=\"tbRenameFile_" +
        stateObjectIndex.toString() + "\" value=\"" + name +
        "\" style=\"width: 100%; box-sizing: border-box;\"></div>" +
        "<input type=\"button\" value=\"Rename\" style=\"width: 100%;\" " +
        "onclick=\"cfm_RenameFile(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
        "onclick=\"removePopUp()\");\" />\
</div>");

    //make the div we just created into a dialog box
    $('#input_dialog').dialog({
        modal: true,
        autoOpen: true,
        resizable: false,
        width: 225,
        height: 225,
        closeOnEscape: true,
        close: removePopUp
    });

    //Create the dialog box
    $('#input_dialog').dialog('open');
}

//Remove popup controls
function removePopUp() {
    //hide the dialog box
    $('#input_dialog').dialog("destroy");
    //then remove the div
    $('#input_dialog').remove();
}

// Function that gets the extra service arguments for the file uploader web service. We 
// need to add an argument that indicates what the target folder is.
function cfm_GetExtraServiceArgs(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // The state object has the target folder information
    return "&target_folder=" + state.targetFolder + "&uploadLocalTime=" +
        (new Date()).toString();
}

function cfm_hideControls(stateObjectIndex) {
    var cntrls = null;
    if (cfm_states[stateObjectIndex].isFolder) {
        cntrls = document.getElementById("folder_controls_" + stateObjectIndex.toString());
    }
    else {
        cntrls = document.getElementById("file_controls_" + stateObjectIndex.toString());
    }
    if (cntrls) { cntrls.innerHTML = ""; }
}

function cfm_RenameFile(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Get the current course ID
    var courseID = GetSelectedCourseID();

    // Find the textbox and get the folder name
    var tb = document.getElementById("tbRenameFile_" + stateObjectIndex.toString());
    if (null == tb) {
        // Problem, but we can't do much about it
        cfm_hideControls(stateObjectIndex);
        return;
    }

    // remove special chars
    tb.value = tb.value.replace(/[^a-z0-9._-]+/gi, "");

    // put the file extention back on
    if (state.name.contains('.'))
        tb.value += state.name.substr(state.name.lastIndexOf('.'));

    // New file name cannot be empty
    if (0 == tb.value.length) {
        tb.focus();
        return;
    }

    // Make an XML HTTP request to the service
    var req = new XMLHttpRequest();
    req.addEventListener("load",
        function (args) {
            cfm_listcompletion(args, state.fm_div_ID);
        },
        false);
    req.addEventListener("error",
        function (args) { alert("Error renaming file"); },
        false);
    req.open("POST", "../Services/CourseFilesOps.ashx?cmd=rename_file&courseID=" +
        courseID + "&file_name=" + state.fullPath + "&new_name=" + tb.value);
    req.send();

    //Remove the pop up dialog box
    removePopUp();
    cfm_expand_collapseRoot();

    //Expand the previously expanded divs
    cfm_retrieveListingStatus();
}

function cfm_RenameFolder(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //Get the old file name to remove cookies
    //var element = $("stateSelectID_" + stateObjectIndex);
    //var elementTitle = $(element).attr("folder-name");

    // Get the current course ID
    var courseID = GetSelectedCourseID();

    // Find the textbox and get the folder name
    var tb = document.getElementById("tbRenameFolder_" + stateObjectIndex.toString());
    if (null == tb) {
        // Problem, but we can't do much about it
        cfm_hideControls(stateObjectIndex);
        return;
    }

    // remove special chars
    tb.value = tb.value.replace(/[^a-z0-9]+/gi, " ");

    // New folder name cannot be empty
    if (0 == tb.value.length) {
        tb.focus();
        return;
    }

    // Make an XML HTTP request to the service
    var req = new XMLHttpRequest();
    req.addEventListener("load",
        function (args) {
            cfm_listcompletion(args, state.fm_div_ID);
        },
        false);
    req.addEventListener("error",
        function (args) { alert("Error renaming folder"); },
        false);
    req.open("POST", "../Services/CourseFilesOps.ashx?cmd=rename_folder&courseID=" +
        courseID + "&folder_name=" + state.targetFolder + "&new_name=" + tb.value);
    req.send();

    //Remove the pop up box
    removePopUp();

    cfm_expand_collapseRoot();

    //If the current div being renamed is expanded
    /*if (state.expanded == true) {
        //Remove the div's old name in the cookie and replace it with the new name
        cfm_removeCookie(stateObjectIndex.toString());
        cfm_addCookie(tb.value.toString());
    }
    else {
        //Relist the previously expanded divs
        cfm_retrieveListingStatus();
    }*/
    cfm_retrieveListingStatus();
}

function cfm_RenameFolderIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //To tell the highlighting that the context menu was clicked.
    ContextMenu = true;

    // Find the appropriate DIV
    //var targetDIV = $("#folder_" + stateObjectIndex.toString());

    //// Add the rename controls into the DIV
    //targetDIV.innerHTML = "<br /><div><input type=\"text\" id=\"tbRenameFolder_" +
    //    stateObjectIndex.toString() + "\" value=\"" + state.name +
    //    "\" style=\"width: 100%; box-sizing: border-box;\"></div>" +
    //    "<input type=\"button\" value=\"Rename\" style=\"width: 100%;\" " +
    //    "onclick=\"cfm_RenameFolder(" + stateObjectIndex.toString() + ");\" />" +
    //    "<br /><input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
    //    "onclick=\"cfm_hideControls(" + stateObjectIndex.toString() + ");\" />";

    //// Add the controls into the DIV
    $('body').append(
"<div id=\"input_dialog\" title=\"Rename Folder\"> \
        <br /><div><input type=\"text\" id=\"tbRenameFolder_" +
        stateObjectIndex.toString() + "\" value=\"" + state.name +
        "\" style=\"width: 100%; box-sizing: border-box;\"></div>" +
        "<input type=\"button\" value=\"Rename\" style=\"width: 100%;\" " +
        "onclick=\"cfm_RenameFolder(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
        "onclick=\"removePopUp()\" />" +
"</div>");

    //make the div we just created into a dialog box
    $('#input_dialog').dialog({
        modal: true,
        autoOpen: true,
        resizable: false,
        width: 225,
        height: 225,
        closeOnEscape: true,
        close: removePopUp
    });

    //Create the dialog box
    $('#input_dialog').dialog('open');

    // Mark the controls as visible
    state.controlsVisible = true;
}


//When multiple files are selected and delete is called
function cfm_multipleFileDelete() {

    //To tell the highlighting that the context menu was clicked.
    ContextMenu = true;

    // Add the controls into the DIV
    // Different from deleting one file, the deletion calls will need to be partitioned to
    // appropriate deletion calls.  Function call is cfm_PartitionDeleteCalls()
    $('body').append(
"<div id=\"input_dialog\" title=\"Delete Files\"> " +
     "<br /><div>Are you sure you want to delete (" + cfm_statesSelected.length.toString() + ") files?</div>" +
        "<input type=\"button\" value=\"Yes, delete\" style=\"width: 100%;\" " +
        "onclick=\"cfm_PartitionDeleteCalls();\" />" +
        "<br /><input type=\"button\" value=\"No, Cancel\" style=\"width: 100%;\" " +
        "onclick=\"removePopUp()\");\" />\ " +
"</div>");

    //make the div we just created into a dialog box
    $('#input_dialog').dialog({
        modal: true,
        autoOpen: true,
        resizable: false,
        width: 225,
        height: 225,
        closeOnEscape: true,
        close: removePopUp
    });

    //Create the dialog box
    $('#input_dialog').dialog('open');
}

function cfm_CopyLinkClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    //To tell the highlighting that the context menu was clicked.
    ContextMenu = true;

    // Find the appropriate DIV
    var targetDIV = $("#file_" + stateObjectIndex.toString());
    var copyLink = "https://plus.osble.org";
    copyLink += $(targetDIV).data("path");


    //// Add the controls into the DIV
    $('body').append(
"<div id=\"input_dialog\" title=\"Copy File Link\"> \
        <br /><div><input type=\"text\" onfocus=\"this.select()\" value=\"" + copyLink +
        "\" style=\"width: 100%; box-sizing: border-box;\"></div>" +
        "<br /><input type=\"button\" value=\"Ok\" style=\"width: 100%;\" " +
        "onclick=\"removePopUp()\");\" />\
</div>");

    //make the div we just created into a dialog box
    $('#input_dialog').dialog({
        modal: true,
        autoOpen: true,
        resizable: false,
        width: 225,
        height: 225,
        closeOnEscape: true,
        close: removePopUp
    });

    //Create the dialog box
    $('#input_dialog').dialog('open');

    //Referesh the listing for highlighting purposes
    cfm_getListing(state.fm_div_ID);

}

//When multiple files and/or folders have been selected we need to 
//divide up the delete calls depending on whether or not
//the item is a folder or file
function cfm_PartitionDeleteCalls() {
    for (var i = 0; i < cfm_statesSelected.length; i++) {
        elem = document.getElementById('stateSelectID_' + cfm_statesSelected[i]);

        var m = $(elem).attr('state-obj-select');
        var type = $(elem).attr('file-or-folder');
        var index = cfm_statesSelected.indexOf(m);

        if (index > -1) {

            if (type == "file") {
                cfm_DeleteFile(m);
                cfm_statesSelected.splice(m, 1);
            }
            else if (type == "folder") {
                cfm_DeleteFolder(m);
                cfm_statesSelected.splice(m, 1);
            }

        }
    }
}

// Context menu stuff
// A context menu with full functionality, this is a folder which is not the root of the directory.
$(function () {
    $.contextMenu(
        {
            selector: '.context-menu-one',
            callback: function (key, options) {
            },
            items: {
                "Rename": {
                    name: "Rename",
                    icon: "edit",
                    callback: function (key, opt) {
                        // get the state object id of the clicked div and call the rename function
                        var m = opt.$trigger.attr('state-obj');
                        cfm_RenameFolderIconClicked(m);
                    }
                },
                "Delete": {
                    name: "Delete",
                    icon: "delete",
                    callback: function (key, opt) {
                        // get the state object id of the clicked div and call the delete function
                        var m = opt.$trigger.attr('state-obj');

                        if (cfm_boolMultipleSelected == true) {
                            cfm_multipleFileDelete();
                        }
                        else {
                            cfm_DeleteFolderIconClicked(m);
                        }
                    }
                },
                "Upload File": {
                    name: "Upload File(s)",
                    icon: "add",
                    callback: function (key, opt) {
                        // get the state object id of the clicked div and call the upload function
                        var m = opt.$trigger.attr('state-obj');
                        cfm_AddUploader(m);
                    }
                },
                "Create Folder": {
                    name: "Create Folder",
                    icon: "add2",
                    callback: function (key, opt) {
                        // get the state object id of the clicked div and call the create  function
                        var m = opt.$trigger.attr('state-obj');
                        cfm_CreateFolderIconClicked(m);
                    }
                }
            },
            events: {
                show: function (opt) {
                    $(this).css("background", "#dfdfdf");
                },
                hide: function (opt) {
                    $(this).css("background", "white");
                }
            }
        });


});

// This is a typical file that is uploaded to the file system like a .PDF
$(function () {
    $.contextMenu({
        selector: '.context-menu-two',
        items: {
            "Rename": {
                name: "Rename",
                icon: "edit",
                callback: function (key, opt) {
                    // get the state object id of the clicked div and call the rename function
                    var m = opt.$trigger.attr('state-obj');
                    cfm_FileRenameIconClicked(m);
                }

            },
            "Delete": {
                name: "Delete",
                icon: "delete",
                callback: function (key, opt) {
                    // get the state object id of the clicked div and call the delete function
                    var m = opt.$trigger.attr('state-obj');
                    if (cfm_boolMultipleSelected == true) {
                        cfm_multipleFileDelete();
                    }
                    else {
                        cfm_FileDeleteIconClicked(m);
                    }
                }

            },
            "Copy Link": {
                name: "Copy Link",
                icon: "cut",
                callback: function (key, opt) {
                    // get the state object id of the clicked div and call the delete function
                    var m = opt.$trigger.attr('state-obj');
                    cfm_CopyLinkClicked(m);
                }

            }
        },
        events: {
            show: function (opt) {
                $(this).css("background", "#dfdfdf");
            },
            hide: function (opt) {
                $(this).css("background", "white");
            }
        }
    });
});

// This is the functionality for a root directory
$(function () {
    $.contextMenu({
        selector: '.context-menu-three',
        callback: function (key, options) {
            var m = "clicked: " + key;
            window.console && console.log(m) || alert(m);
        },
        items: {
            "Upload File": {
                name: "Upload File(s)",
                icon: "add",
                callback: function (key, opt) {
                    // get the state object id of the clicked div and call the rename function   
                    if (typeof opt.$trigger === "undefined") {
                        m = 0;
                    }
                    else {
                        var m = opt.$trigger.attr('state-obj');
                    }
                    cfm_AddUploader(m);
                }

            },
            "Create Folder": {
                name: "Create Folder",
                icon: "add2",
                callback: function (key, opt) {
                    // get the state object id of the clicked div and call the create function
                    if (typeof opt.$trigger === "undefined") {
                        m = 0;
                    }
                    else {
                        var m = opt.$trigger.attr('state-obj');
                    }
                    cfm_CreateFolderIconClicked(m);
                }

            }
        }
    });
});

// Click highlighting on Files and Links
$(document).on('click', '.itemSelection', function (e) {
    //Gets the element clicked and the divs that are currently selected
    var m = $(this).attr('state-obj-select');

    var index = cfm_statesSelected.indexOf(m);

    //If the user is holding down the ctrl key when a div is clicked
    if (e.ctrlKey) {
        //If the div is selected (it is in the cfm_statesSelected array) change it's background color to white
        if (index > -1) {
            //Remove the div from the states selected array
            cfm_statesSelected.splice(index, 1);
            $(this).css("background", "white");
        }
            //Else if the div is not selected (not in the cfm_statesSelected array)
            //change the div background color to gray #dfdfdf
        else {
            //Add the div to the statesSelected array
            cfm_statesSelected.push(m);
            $(this).css("background", "#dfdfdf");
        }
    }
    else { //if the user just clicks the div with nothing else held down
        //Keep the color the same
        $(this).css("background", "#dfdfdf");

        // If multiple itmes are selected at the time of this event 
        // remove all of them from the states selected array and change their background to white
        // this is is suppose to mimic OS file operations.
        for (var i = 0; i < cfm_statesSelected.length; i++) {
            if (cfm_statesSelected[i] != m) {
                elem = document.getElementById('stateSelectID_' + cfm_statesSelected[i]);
                $(elem).css("background", "white");
            }
        }
        var stateSelected = new Array();
        stateSelected.push(m);
        cfm_statesSelected = stateSelected;
    }

    //Check to see if multiple divs are selected
    if (cfm_statesSelected.length > 1) {
        cfm_boolMultipleSelected = true;
    }
    else {
        cfm_boolMultipleSelected = false;
    }

    //An itemSelection was clicked
    FilesAndLinksClick = 1;
});

//Check for all clicks
$(document).on('click', 'html', function (e) {

    //Check to see if files and links was clicked and if not remove all clicked
    if (FilesAndLinksClick == 1 || ContextMenu == true) {
        //Reset the files and links click
        FilesAndLinksClick = 0;
        ContextMenu = false;
    }
    else {
        //If files and links not clicked remove all clicks and reset the links clicked check
        for (var i = 0; i < cfm_statesSelected.length; i++) {
            elem = document.getElementById('stateSelectID_' + cfm_statesSelected[i]);
            $(elem).css("background", "white");
        }
        cfm_statesSelected = [];
        FilesAndLinksClick = 0;
        cfm_boolMultipleSelected = false;
    }

});

//If the mouse hovers change the background color no matter what to gray
$(document).on('mouseenter', '.itemSelection', function () {
    $(this).css("background-color", "#dfdfdf");
});

//If the item is selected when the mouse leaves the div don't change the background color to white 
//If the item is not selected then change the background color to white
$(document).on('mouseleave', '.itemSelection', function () {
    //Gets the element clicked and the divs that are currently selected
    var m = $(this).attr('state-obj-select');
    var index = cfm_statesSelected.indexOf(m);

    if (index > -1) {
        $(this).css("background", "#dfdfdf");
    }
    else {
        $(this).css("background", "white");
    }
});


//Check for clicking Folder+ icon after "Files and Links" 
$(document).on('click', '.root_context_menu', function (e) {
    
    //we need to find the root menu UL
    $(".context-menu-list.context-menu-root").each(function () {
        var listText = $(this).text();        
        
        //when we find the root div, display it at the mouse cursor
        if (listText == "Upload File(s)Create Folder") {
            var currentMousePos = { x: -1, y: -1 };
            currentMousePos.x = e.pageX;
            currentMousePos.y = e.pageY;

            $(this).css("display", "inline");            
            $(this).css("left", currentMousePos.x);
            $(this).css("top", currentMousePos.y);
            $(this).css("width", "171px");
            $(this).css("z-index", "1");

        }
    });

});

//hide the context menu (shown by clicking the folder+ icon) when clicking anywhere else
$('body').click(function (event) {
    if (!$(event.target).closest('.context-menu-list.context-menu-root').length) {
        $('.context-menu-list.context-menu-root').hide();
    };
});

//TODO: need to fix the opt.$trigger undefined before this will work
//Check for clicking a subfolder Folder+ icon
$(document).on("click", ".root_context_subfolder", function (e) {
   
    //RenameDeleteUpload File(s)Create Folder
    ////we need to find the subfolder menu UL
    $(".context-menu-list.context-menu-root").each(function () {
        var listText = $(this).text();                
        //when we find the subfolder menu, display it at the mouse cursor
        if (listText == "RenameDeleteUpload File(s)Create Folder") {
            var currentMousePos = { x: -1, y: -1 };
            currentMousePos.x = e.pageX;
            currentMousePos.y = e.pageY;

            $(this).css("display", "inline");            
            $(this).css("left", currentMousePos.x);
            $(this).css("top", currentMousePos.y);
            $(this).css("width", "171px");
            $(this).css("z-index", "1");

        }
    });

});
