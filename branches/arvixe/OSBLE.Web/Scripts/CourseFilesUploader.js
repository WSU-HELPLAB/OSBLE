// Created by Evan Olds on 5-23-13 for the OSBLE project
// For two years this project has been using a Silverlight file uploader and I 
// figured it was time to replace that.

// inputNameString:
//   String for the name (both id and name) of the <input type="file" ...> object.
// allowMultiple:
//   Boolean value indicating whether or not multiple files can be selected for upload.
// onGetExtraServiceArgs:
//   Function that returns a string of the form: "&arg1=val1&arg2=val2". This string 
//   represents additional arguments to be passed to the upload service. The course ID 
//   will be auto-determined so this does not need to be provided. This argument can be 
//   null if desired.
// onCompletion:
//   Code to execute when upload is complete. If this is null then the page will be refreshed 
//   when the upload completes. If non-null, this string must not contain single or double 
//   quotes.
function fileuploader_getcontrolshtml(inputNameString, allowMultiple, onGetExtraServiceArgs, onCompletion)
{
    var divName = "uploadProgressDIV_" + inputNameString;
    if (null == onGetExtraServiceArgs) {
        onGetExtraServiceArgs = "null";
    }
    else {
        onGetExtraServiceArgs = "\"" + onGetExtraServiceArgs + "\"";
    }
    if (null == onCompletion) {
        onCompletion = "document.location.reload();";
    }

    var code = "<form enctype='multipart/form-data' action='../Services/CourseFilesUploader.ashx' method='post'>";
    code += "<input id='" + inputNameString + "' name='" + inputNameString + "' type='file' ";
    if (allowMultiple) { code += "multiple />"; }
    else { code += "/>"; }
    code += "<input type='button' value='Upload' id='btnSubmit_" + inputNameString;
    code += "' onclick='fileuploader_uploadfiles(\"";
    code += inputNameString + "\", \"" + divName + "\", " + onGetExtraServiceArgs + ", ";
    code += "\"" + onCompletion + "\");' />";
    code += "</form> <div id='" + divName + "'></div>";
    return code;
}

function fileuploader_uploadfiles(fileSourceElementName, progressDIVName, onGetExtraServiceArgs, onCompletion)
{
    // If there's a function to call to get extra service arguments, then call it
    var extraServiceArgs = null;
    if (null != onGetExtraServiceArgs) {
        extraServiceArgs = eval(onGetExtraServiceArgs);
    }

    var files = document.getElementById(fileSourceElementName).files;
    if (0 == files.length) { return; }

    // We upload one file at a time
    var progressDIV = document.getElementById(progressDIVName);    
    progressDIV.innerHTML = "Uploading (1 of " + files.length + "): 0%";
    fileuploader_uploadRemaining(files, 0, progressDIV, extraServiceArgs, onCompletion);
}

function fileuploader_uploadRemaining(filesArray, fileIndex, progressDIV, extraServiceArgs, onCompletion)
{
    if (null == filesArray)
    {
        // Nothing we can do
        progressDIV.innerHTML = "";
        return;
    }

    // If the index is beyond the last item in the array then this implies completion
    if (fileIndex >= filesArray.length)
    {
        progressDIV.innerHTML = "Uploaded " +
            fileIndex.toString() + ((1 == fileIndex) ? " file" : " files");

        // Execute the onCompletion code
        eval(onCompletion);

        return;
    }

    // First get the ID of the currently selected course. The uploader "control" produced 
    // by this script must be on a page with the "course_select" selector. As far as I know 
    // this is every single page in the OSBLE site.
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

    var fd = new FormData();
    fd.append("file_src", filesArray[fileIndex]);

    // Build the POST URL
    var url = "../Services/CourseFilesUploader.ashx?courseID=" + courseID;
    if (null != extraServiceArgs) {
        url += extraServiceArgs;
    }

    var req = new XMLHttpRequest();
    req.upload.addEventListener(
        "progress",
        function (args) { fileuploader_progress(args, fileIndex, filesArray.length, progressDIV); },
        false);
    req.addEventListener("load",
        function (args)
        {
            fileuploader_completion(args, filesArray, fileIndex + 1, progressDIV, extraServiceArgs, onCompletion);
        },
        false);
    req.addEventListener("error",
        function (args) { fileuploader_fail(args, progressDIV); },
        false);
    req.addEventListener("abort", fileuploader_canceled, false);
    req.open("POST", url);
    req.send(fd);
}

function fileuploader_progress(args, currentIndex, totalFileCount, progressDIV)
{
    if (args.lengthComputable)
    {
        var percent = Math.round(args.loaded * 100 / args.total);
        progressDIV.innerHTML = "Uploading (" +
            (currentIndex + 1) + " of " + 
            totalFileCount.toString() + "): " + percent.toString() + "%";
    }
}

function fileuploader_completion(args, filesArray, nextIndex, progressDIV, extraServiceArgs, onCompletion)
{
    // TODO: Parse response XML and make sure the upload worked
    //alert(args.target.responseXML);

    // Start the next upload if there is one
    fileuploader_uploadRemaining(filesArray, nextIndex, progressDIV, extraServiceArgs, onCompletion);
}

function fileuploader_fail(args, progressDIV)
{
    progressDIV.innerHTML = "Error uploading file. Please try again.";
}

function fileuploader_canceled(args)
{
    document.getElementById("uploadProgressDIV").innerHTML = "Upload canceled or connection lost";
}