﻿// Created by Evan Olds for the OSBLE project at WSU
// This file contains functions for web-service-based file uploading on the 
// assignment details page in the assignment creation wizard
// The actual uploading code isn't here, that's in the generic uploader 
// script. This is just to manage upload related things on the assignment 
// description page.
var srvcArgsDesc = "";
var srvcArgsSol = "";

//For downloading files
var globlCourseID;

function assignmentfilemanager_update(assignmentID)
{
    // Reset status to loading
    document.getElementById("descriptionFilesDIV").innerHTML = "(please wait...)";
    document.getElementById("solutionFilesDIV").innerHTML = "(please wait...)";

    // Get the current course ID
    var courseID = GetSelectedCourseID();

    globlCourseID = courseID;
    globlAssignmentID = assignmentID;

    // Do the service request to get the file list for this assignment
    var req = new XMLHttpRequest();
    req.addEventListener("load",
        function (args) { assignmentfilemanager_listcompletion(args, assignmentID, "descriptionFilesDIV"); },
        false);

    //req.addEventListener("error", assignmentfilemanager_fail, false);
    //req.addEventListener("abort", assignmentfilemanager_canceled, false);
    req.open("GET", "../Services/CourseFilesOps.ashx?cmd=assignment_files_list&courseID=" +
        courseID + "&assignmentID=" + assignmentID + "&forceNoCache=" + (new Date()).toString());
    req.send();
}

function assignmentfilemanager_listcompletion(args, assignmentID, descDIVName)
{
    var doc = args.target.responseXML;
    var root = doc.firstChild;
    if ("true" != root.getAttribute("success"))
    {
        var msg = "(update failed, please refresh the page and contact support if the problem persists)";
        document.getElementById(descDIVName).innerHTML = msg;
        document.getElementById("filesAssignSolution").innerHTML = msg;
    }
    else
    {
        // Find the <file_list> node
        var lists = doc.getElementsByTagName("file_list");
        if (0 == lists.length)
        {
            alert("Update failed (invalid XML returned)");
            return;
        }

        var listNode = lists[0];
        var descHTML = "<ul>";
        var solHTML = "<ul>";
        if (listNode.childNodes.length > 0 && 1 == listNode.childNodes[0].nodeType)
        {
            var files = listNode.getElementsByTagName("file");

            // Go through the list of files
            for (var i = 0; i < files.length; i++)
            {
                var tempNode = files[i];
                if (null == tempNode || 1 != tempNode.nodeType) { continue; }

                // Look for the "assignment_description" node
                var descNodes = tempNode.getElementsByTagName("assignment_description");
                if (descNodes && descNodes.length > 0)
                {
                    // Only take it if it has the right assignment ID. At the time of this 
                    // writing all files with this attribute should have the right ID or 
                    // else they wouldn't even have been returned in the request. But this 
                    // just makes it slightly more robust.
                    if (assignmentID == descNodes[0].childNodes[0].nodeValue)
                    {
                        //Add download and remove links
                        descHTML += "<li><a href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_download" +
                               "&courseID=" + globlCourseID + "&assignmentID=" + assignmentID + "&filename=" + tempNode.getAttribute("name") + "\">" + tempNode.getAttribute("name") + "</a>";
                        //Delete file link
                        descHTML += "<a id = \"deleteFile\" href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_delete" +
                               "&courseID=" + globlCourseID + "&assignmentID=" + assignmentID + "&filename=" + tempNode.getAttribute("name") + "\"><img src=\"/Content/images/delete_up.png\" alt=\"Delete Button\"></img></a>" +
                               "</li>";

                        //descHTML += ("<li>" + tempNode.getAttribute("name") + "</li>");
                    }
                }

                // Look for the "assignment_solution" node
                var solNodes = tempNode.getElementsByTagName("assignment_solution");
                if (solNodes && solNodes.length > 0)
                {
                    if (assignmentID == solNodes[0].childNodes[0].nodeValue)
                    {
                        //Add download and remove links
                        solHTML += "<li><a href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_download" +
                               "&courseID=" + globlCourseID + "&assignmentID=" + assignmentID + "&filename=" + tempNode.getAttribute("name") + "\">" + tempNode.getAttribute("name") + "</a>";
                        //Delete file link
                        solHTML += "<a id = \"deleteFile\" href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_delete" +
                               "&courseID=" + globlCourseID + "&assignmentID=" + assignmentID + "&filename=" + tempNode.getAttribute("name") + "\"><img src=\"/Content/images/delete_up.png\" alt=\"Delete Button\"></img></a>" +
                               "</li>";


                       // solHTML += ("<li>" + tempNode.getAttribute("name") + "</li>");
                    }
                }
            }
        }

        descHTML += "</ul>";
        solHTML += "</ul>";

        // When a file upload completes we want to refresh the list
        var onCompletion = "assignmentfilemanager_update(" + assignmentID.toString() + ");";

        // Add the upload controls HTML too
        srvcArgsDesc = "&assignmentID=" + assignmentID + "&fileusage=assignment_description";
        srvcArgsSol = "&assignmentID=" + assignmentID + "&fileusage=assignment_solution";
        descHTML += fileuploader_getcontrolshtml("src_assignment_description", true, "assignmentfilemanager_getDescSrvcArgs();", onCompletion);
        solHTML += fileuploader_getcontrolshtml("src_assignment_solution", true, "assignmentfilemanager_getSolSrvcArgs();", onCompletion);

        // Put the upload controls in
        document.getElementById(descDIVName).innerHTML = descHTML;
        document.getElementById("solutionFilesDIV").innerHTML = solHTML;
    }
}

function assignmentfilemanager_getDescSrvcArgs() {
    return srvcArgsDesc;
}

function assignmentfilemanager_getSolSrvcArgs() {
    return srvcArgsSol;
}