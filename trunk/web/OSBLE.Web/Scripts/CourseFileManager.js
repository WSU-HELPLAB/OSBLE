// Created by Evan Olds for the OSBLE project at WSU

// Dependencies: CourseFilesUploader.js
// Important notes:
//    Only one file manager per page is currently allowed

// Global array for state objects
var cfm_states = new Array();

// Call this function to start the asynchronous request for a file listing.
// When the request completes the inner HTML of the DIV with the specified 
// ID will be set.
function cfm_getListing(targetDIVID) {
    // Get the current course ID
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

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
            "Please contact support for help with this issue.  error 1";
        return;
    }

    // DEBUG
    //alert(args.target.responseText);

    var root = doc.firstChild;
    if (null == doc) {
        theDIV.innerHTML = "Error: Root element in XML document from service is null! " +
            "Please contact support for help with this issue. error 2";
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
        theDIV.innerHTML = cfm_MakeDIV(listNode, "", "padding: 0px;", -1, targetDIVID);
    }

    for (var i = 0; i < cfm_states.length; i++) {
        cfm_expand_collapse(i);
    }
}

function cfm_MakeDIV(listNode, relativeDir, styleString, parentStateIndex, targetDIVID) {
    var rArrowImg = "<img src=\"/Content/images/arrow_right.png\" />";
    //var rArrowImg = "<img src=\"/Content/images/arrow_down.png\" />";

    var result = "<div id=\"content_of_" + parentStateIndex.toString() +
        "\" style=\"" + styleString + "\">";

    // Get the current course ID
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

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

        // Directories always have a left, bottom, and right borders. Root directory is a 
        // special case that has all borders. Note that directories will get a top border 
        // when the directory above them is expanded, but this is changed at when such an 
        // action occurs.
        var theStyle
        if ("/" == folderPath) {
            theStyle = "padding: 3px; border: 0px; background: white; ";
        }
        else {
            theStyle = "padding: 3px; border: 0px; background: white; ";
        }

        // One new DIV for the folder name and control buttons
        result += "<div id=\"folder_div_" + ss + "\" style=\"" + theStyle + "\">";
        result += "<table width=100%; id=\"folder_text_" + ss + "\" style=\"" + " table-layout: fixed; " + "\">";
        result += "<tr>";
        result += "<td style=\"" + " width: 85%; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; " + "\">";
        if (stateObj.allowsCollapsing) {
            result += "<a style=\"cursor: pointer;\" onclick=\"cfm_expand_collapse(" + ss + ");\">";
            result += "<img id=\"expander_img_" + ss + "\" src=\"" + stateObj.getExpanderImgSrc() + "\" />&nbsp;" + folderName;
            result += "</a>";
        }
        else { result += folderName; }
        result += "</td>";
        result += "<td style=\"" + " width: 50%; overflow: hidden; " + "\">";
        if (stateObj.allowsUploads) {
            // Can only rename and delete if not root
            if ("/" != folderPath) {

                if (stateObj.allowsDeletion) {
                    result += "<a onclick='cfm_DeleteFolderIconClicked(" + stateObjIndex.toString() + ");' " +
                        "title=\"Delete this folder...\">" +
                        "<img style=\"cursor: pointer;\" align=\"right\" " +
                        "src=\"/Content/images/delete_up.png\"></a>";
                }

                // Again we're assuming that if they can upload files then they can also 
                // rename folders. These two concepts might need to end up being separate 
                // permission values later on.
                result += "<a onclick='cfm_RenameFolderIconClicked(" + stateObjIndex.toString() + ");' " +
                    "title=\"Rename this folder...\">" +
                    "<img style=\"cursor: pointer;\" align=\"right\" " +
                    "src=\"/Content/images/edit_up.png\"></a>";
            }

            // Need a button to upload files (makes uploader control appear when clicked)
            result += "<a onclick='cfm_AddUploader(" + stateObjIndex.toString() + ");' " +
                "title=\"Upload files to this folder...\">" +
                "<img style=\"cursor: pointer;\" align=\"right\" " +
                "src=\"/Content/images/publish.png\"></a>";

            // Right now we're assuming that if they can upload files then they can also 
            // create subfolders. These two concepts might need to end up being separate 
            // permission values later on.
            result += "<a onclick='cfm_CreateFolderIconClicked(" + stateObjIndex.toString() + ");' " +
                "title=\"Create a subfolder within this folder...\">" +
                "<img style=\"cursor: pointer;\" align=\"right\" " +
                "src=\"/Content/images/folder_plus.png\"></a>";
        }
        // There's another DIV within for dynamically created controls
        result += "</td>";
        result += "</tr>";
        result += "</table>";
        result += "<div id=\"folder_controls_" + stateObjIndex.toString() + "\"></div>";
        result += "</div>";



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

        result += "<div id=\"" + theID + "\" style=\"padding: 3px; background-color: ";
        if (0 == i % 2) { result += "#ffffff; "; }
        else { result += "#ffffff; "; }
        result += "border: 0px;";
        result += "\">";

        result += "<table width=100%; id=\"folder_text_" + ss + "\" style=\"" + " table-layout: fixed; " + "\">";
        result += "<tr>";
        result += "<td style=\"" + " width: 85%; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; " + "\">";


        result += ("<a href=\"" + linkURL + "\"><img src=\"" + imgSrc + "\" />" + fileName + "</a>");

        result += "</td>";
        result += "<td style=\"" + " width: 50%; overflow: hidden; " + "\">";

        // I'm going to go with the approach of having a single edit button per file that, 
        // when clicked, brings up options for further actions. This button will only appear 
        // if the user has deletion permissions.
        if (fileStateObj.allowsDeletion) {
            result += "<a onclick='cfm_FileDeleteIconClicked(" + stateObjIndex.toString() + ");' " +
                "title=\"Delete this file\">" +
                "<img style=\"cursor: pointer;\" align=\"right\" " +
                "src=\"/Content/images/delete_up.png\"></a>";

            result += "<a onclick='cfm_FileRenameIconClicked(" + stateObjIndex.toString() + ");' " +
                "title=\"Rename this file...\">" +
                "<img style=\"cursor: pointer;\" align=\"right\" " +
                "src=\"/Content/images/edit_up.png\"></a>";
        }

        result += "</td>";
        result += "</tr>";
        result += "</table>";

        // There's another DIV for controls for the file, which is empty by default
        result += "<div id=\"file_controls_" + stateObjIndex.toString() + "\"></div>";

        result += "</div>";
    }
    result += "</div>";

    return result;
}

// Functions below here are in alphabetical order. Keep them that way.

function cfm_AddUploader(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Find the appropriate DIV
    var targetDIV = document.getElementById("folder_controls_" + stateObjectIndex.toString());

    // Add the uploader control into the DIV
    targetDIV.innerHTML = "<br />" + fileuploader_getcontrolshtml(
        "cfm_files_" + stateObjectIndex.toString(), true,
        "cfm_GetExtraServiceArgs(" + stateObjectIndex.toString() + ");",
        "cfm_uploadComplete(" + stateObjectIndex.toString() + ");") +
        "<input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
        "onclick=\"cfm_hideControls(" + stateObjectIndex.toString() + ");\" />";

    // Find the submit button and make its width 100%
    var btnUpload = document.getElementById("btnSubmit_cfm_files_" + stateObjectIndex.toString());
    if (btnUpload) {
        btnUpload.style.width = "100%";
    }

    // Mark the controls as visible
    state.controlsVisible = true;
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
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

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
    name += tb.value;

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
}

function cfm_CreateFolderIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Find the appropriate DIV
    var targetDIV = document.getElementById("folder_controls_" + stateObjectIndex.toString());

    // Add the creation controls into the DIV
    targetDIV.innerHTML = "<br />Create Folder:&nbsp;" +
        "<input type=\"text\" id=\"tbSubfolder_" + stateObjectIndex.toString() + "\" />" +
        "<br /><input type=\"button\" value=\"Create\" style=\"width: 100%;\" " +
        "onclick=\"cfm_CreateFolder(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
        "onclick=\"cfm_hideControls(" + stateObjectIndex.toString() + ");\" />";

    // Mark the controls as visible
    state.controlsVisible = true;
}

function cfm_DeleteFile(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Get the current course ID
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

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
}

function cfm_DeleteFolder(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Get the current course ID
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

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
}

function cfm_DeleteFolderIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Find the appropriate DIV
    var targetDIV = document.getElementById("folder_controls_" + stateObjectIndex.toString());

    // Add the confirmation controls into the DIV
    targetDIV.innerHTML = "<br />Are you sure you want to delete this folder and all its contents?" +
        "<br /><input type=\"button\" value=\"Yes, delete\" style=\"width: 100%;\" " +
        "onclick=\"cfm_DeleteFolder(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"No, cancel\" style=\"width: 100%;\" " +
        "onclick=\"cfm_hideControls(" + stateObjectIndex.toString() + ");\" />";

    // Mark the controls as visible
    state.controlsVisible = true;
}

function cfm_expand_collapse(stateObjectIndex) {
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

        // We want to be consistent and clean with border styles, so there are many 
        // states we have to handle.
        // 1. We just expanded a folder "F" that has parent "P"
        //   1a. There's another folder G within P that is below F. In this case G needs 
        //       a 1-pixel thick top border.
        //   1b. There is not another folder within P and below F, but there are files 
        //       in P (one or more) that come directly below F in the listing. In this 
        //       case the first such file needs a 1-pixel thick top border.
        // 2. We just collapsed a folder (similar to 1 but we remove borders)

        // Need to search in array and check for folder states with == parent index.
        var nextFolderIndex = -1;
        for (var i = stateObjectIndex + 1; i < cfm_states.length; i++) {
            if (true === cfm_states[i].isFolder) {
                if (cfm_states[i].parentIndex == state.parentIndex) {
                    nextFolderIndex = i;
                    break;
                }
            }
        }
        if (-1 != nextFolderIndex) {
            var nextFolder = document.getElementById("folder_div_" + nextFolderIndex.toString());
            if (null != nextFolder) {
                if (!state.expanded) {
                    // Case 1a. It wasn't expanded and now it is, meaning the folder below it needs a border
                    nextFolder.style.borderTop = "0px";
                }
                else {
                    nextFolder.style.borderTop = 0;
                }
            }
        }
        else {
            var firstFile = document.getElementById("file0_folderState" + state.parentIndex.toString());
            if (null != firstFile) {
                if (!state.expanded) {
                    // Case 1b. It wasn't expanded and now it is, meaning the file below it needs a border
                    firstFile.style.borderTop = "0px";
                }
                else {
                    firstFile.style.borderTop = 0;
                }
            }
        }
    }

    // Update the state
    state.expanded = !state.expanded;

    // Update the source of the image now that the new state is set
    if (null != theIMG) { theIMG.src = state.getExpanderImgSrc(); }
}

function cfm_FileDeleteIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Find the appropriate DIV
    var targetDIV = document.getElementById("file_controls_" + stateObjectIndex.toString());

    // Add the rename controls into the DIV
    targetDIV.innerHTML = "<br /><div>Are you sure you want to delete this file?</div>" +
        "<input type=\"button\" value=\"Yes, delete\" style=\"width: 100%;\" " +
        "onclick=\"cfm_DeleteFile(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"No, Cancel\" style=\"width: 100%;\" " +
        "onclick=\"cfm_hideControls(" + stateObjectIndex.toString() + ");\" />";
}

function cfm_FileRenameIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Find the appropriate DIV
    var targetDIV = document.getElementById("file_controls_" + stateObjectIndex.toString());

    // Add the rename controls into the DIV
    targetDIV.innerHTML = "<br /><div><input type=\"text\" id=\"tbRenameFile_" +
        stateObjectIndex.toString() + "\" value=\"" + state.name +
        "\" style=\"width: 100%; box-sizing: border-box;\"></div>" +
        "<input type=\"button\" value=\"Rename\" style=\"width: 100%;\" " +
        "onclick=\"cfm_RenameFile(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
        "onclick=\"cfm_hideControls(" + stateObjectIndex.toString() + ");\" />";
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
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

    // Find the textbox and get the folder name
    var tb = document.getElementById("tbRenameFile_" + stateObjectIndex.toString());
    if (null == tb) {
        // Problem, but we can't do much about it
        cfm_hideControls(stateObjectIndex);
        return;
    }

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
}

function cfm_RenameFolder(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Get the current course ID
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

    // Find the textbox and get the folder name
    var tb = document.getElementById("tbRenameFolder_" + stateObjectIndex.toString());
    if (null == tb) {
        // Problem, but we can't do much about it
        cfm_hideControls(stateObjectIndex);
        return;
    }

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
}

function cfm_RenameFolderIconClicked(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];

    // Find the appropriate DIV
    var targetDIV = document.getElementById("folder_controls_" + stateObjectIndex.toString());

    // Add the rename controls into the DIV
    targetDIV.innerHTML = "<br /><div><input type=\"text\" id=\"tbRenameFolder_" +
        stateObjectIndex.toString() + "\" value=\"" + state.name +
        "\" style=\"width: 100%; box-sizing: border-box;\"></div>" +
        "<input type=\"button\" value=\"Rename\" style=\"width: 100%;\" " +
        "onclick=\"cfm_RenameFolder(" + stateObjectIndex.toString() + ");\" />" +
        "<br /><input type=\"button\" value=\"Cancel\" style=\"width: 100%;\" " +
        "onclick=\"cfm_hideControls(" + stateObjectIndex.toString() + ");\" />";

    // Mark the controls as visible
    state.controlsVisible = true;
}

function cfm_uploadComplete(stateObjectIndex) {
    // Get the state object at the specified index
    var state = cfm_states[stateObjectIndex];
    // Refresh the listing
    cfm_getListing(state.fm_div_ID);
}