// Created by Evan Olds on 5-23-13 for the OSBLE project
// For two years this project has been using a Silverlight file uploader and I 
// figured it was time to replace that.

if (XMLHttpRequest)
{
    document.write("<form enctype='multipart/form-data' action='../Services/CourseFilesUploader.ashx' method='post'>");
    document.write("<input id='file_src' name='file_src' type='file' multiple />");
    document.write("<input type='button' value='Upload' onclick='fileuploader_uploadfiles();' />");
    document.write("</form> <div id='uploadProgressDIV'></div>");
}
else
{
    document.write("File upload not supported. Please upgrade your web browser.");
}

var fileuploader_currentindex = -1;
var fileuploader_filesarray = null;
function fileuploader_uploadfiles()
{
    var files = document.getElementById("file_src").files;
    if (0 == files.length) { return; }
    fileuploader_filesarray = files;

    // We upload one file at a time and this is the 0-based index of the one we're on
    fileuploader_currentindex = 0;
    var progressDIV = document.getElementById("uploadProgressDIV");
    
    progressDIV.innerHTML = "Uploading (1 of " + files.length + "): 0%";
    fileuploader_uploadnext();
}

function fileuploader_uploadnext()
{
    if (null == fileuploader_filesarray)
    {
        // Nothing we can do
        document.getElementById("uploadProgressDIV").innerHTML = "";
        return;
    }

    // If the index is beyond the last item in the array then this implies completion
    if (fileuploader_currentindex >= fileuploader_filesarray.length)
    {
        document.getElementById("uploadProgressDIV").innerHTML = "Uploaded " +
            fileuploader_currentindex + ((1 == fileuploader_currentindex) ? " file" : " files");
        fileuploader_currentindex = -1;
        fileuploader_filesarray = null;

        // Refresh the page. This is probably going to be temporary since ideally the file 
        // listing should be refreshable without a total page reload. But right now it's 
        // not, so I'm sticking with this.
        document.location.reload();

        return;
    }

    // First get the ID of the currently selected course. The uploader "control" produced 
    // by this script must be on a page with the "course_select" selector. As far as I know 
    // this is every single page in the OSBLE site.
    var selectCourseObj = document.getElementById("course_select");
    var courseID = selectCourseObj.value;

    var fd = new FormData();
    fd.append("file_src", fileuploader_filesarray[fileuploader_currentindex]);

    var req = new XMLHttpRequest();
    req.upload.addEventListener("progress", fileuploader_progress, false);
    req.addEventListener("load", fileuploader_completion, false);
    req.addEventListener("error", fileuploader_fail, false);
    req.addEventListener("abort", fileuploader_canceled, false);
    req.open("POST", "../Services/CourseFilesUploader.ashx?courseID=" + courseID);
    req.send(fd);
}

function fileuploader_progress(args)
{
    if (args.lengthComputable)
    {
        var percent = Math.round(args.loaded * 100 / args.total);
        document.getElementById("uploadProgressDIV").innerHTML = "Uploading (" +
            (fileuploader_currentindex + 1) + " of " + 
            fileuploader_filesarray.length + "): " + percent.toString() + "%";
    }
}

function fileuploader_completion(args)
{
    if (-1 == fileuploader_currentindex || null == fileuploader_filesarray)
    {
        fileuploader_currentindex = -1;
        fileuploader_filesarray = null;
        return;
    }

    // TODO: Parse response XML and make sure the upload worked
    //alert(args.target.responseText);

    // Start the next upload if there is one
    fileuploader_currentindex++;
    fileuploader_uploadnext();
}

function fileuploader_fail(args)
{
    fileuploader_currentindex = -1;
    fileuploader_filesarray = null;
    document.getElementById("uploadProgressDIV").innerHTML = "Error uploading file. Please try again.";
}

function fileuploader_canceled(args)
{
    document.getElementById("uploadProgressDIV").innerHTML = "Upload canceled or connection lost";
}