using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Ionic.Zip;
using OSBLEPlus.Logic.Utility;
using System.Xml.Linq;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class SubmitEvent : ActivityEvent
    {
        public int AssignmentId { get; set; }
        public byte[] SolutionData { get; private set; }
        public SubmitEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)Utility.Lookups.EventType.SubmitEvent;
        }

        public SubmitEvent(DateTime dateTimeValue)
            : this()
        {
            EventDate = dateTimeValue;
        }

        public byte[] GetSolutionBinary()
        {
            if (string.IsNullOrWhiteSpace(SolutionName))
                return null;

            var stream = new MemoryStream();

            using (var zip = new ZipFile())
            {
                var rootPath = Path.GetDirectoryName(SolutionName);
                List<string> files = GetSolutionFileList(rootPath);

                if (rootPath == null || !files.Any())
                {
                    return null;
                }

                //need to parse the .sln to grab any added projects now...
                List<string> vcxprojFiles = ParseSolutionForProjects(SolutionName);

                foreach (string vcxprojFile in vcxprojFiles)
                {
                    if (!vcxprojFile.Contains(rootPath)) //if it doesn't contain the root path it must be an external project
                    {   //we'll need to grab the files from each external path as the first GetSolutionFileList didn't look in any external directories
                        files.AddRange(GetSolutionFileList(Path.GetDirectoryName(vcxprojFile)));
                    }
                }

                //need to parse identifier on relative path corresponding to their vcxproj file so we can handle them separately
                List<string> relativePathFiles = new List<string>();
                foreach (var file in files)
                {
                    if (file.Contains("$$$")) //used $$$ as a delimiter to keep track of the external project file path
                    {
                        relativePathFiles.Add(file);
                    }
                }

                //temporarily remove the relative path files from the main files list -- will process next
                files = files.Except(relativePathFiles).ToList();

                foreach (var file in files)
                {
                    if (file.Split('.').Last() == "vcxproj" || file.Split('.').Last() == "sln")
                    {   //don't add the file yet, we need to modify all .vcxproj and the .sln for the new zip path structure and modify relative path references
                        continue;
                    }

                    var name = Path.GetDirectoryName(file);
                    if (name == null) continue;
                    string directoryName = "";

                    if (!file.Contains(rootPath)) //we need to account for files with a different root sub-path
                    {
                        List<string> fileParts = file.Split('\\').ToList();
                        directoryName = fileParts.Count() >= 2 ? fileParts[fileParts.Count() - 2] : "";
                    }
                    else
                    {
                        directoryName = name.Replace(rootPath, string.Empty);
                    }
                    zip.AddFile(file, directoryName);
                }

                try //enclose this portion in a try catch just in case something goes wrong... we want to submit as normal if so
                {
                    if (relativePathFiles.Count() > 0) //now add the relative path to the sourceFileDirectory
                    {
                        foreach (string file in relativePathFiles)
                        {
                            if (file.Split('.').Last() == "vcxproj")
                            {   //don't add the file yet, we need to modify it for the new zip path structure
                                continue;
                            }

                            string filePath = file.Split(new string[] { "$$$" }, StringSplitOptions.None).First();
                            string fileName = file.Split(new string[] { "$$$" }, StringSplitOptions.None).Last();
                            string directoryName = "";

                            if (!filePath.Contains(rootPath))
                            {
                                directoryName = filePath.Split('\\').ToList().Last();
                            }
                            else
                            {
                                directoryName = filePath.Replace(rootPath, string.Empty);
                            }
                            zip.AddFile(fileName, directoryName);
                        }

                        //We need to parse and modify the vcxproj file to make the relative path files referenced correctly in the submission
                        //modify the .vcxproj file to point to the source folder in the zip instead of the relative path
                        foreach (string vcxprojFile in vcxprojFiles)
                        {
                            var name = Path.GetDirectoryName(vcxprojFile);
                            if (name == null) continue;
                            string directoryName = "";
                            string fileName = vcxprojFile.Split('\\').Last();

                            if (!vcxprojFile.Contains(rootPath))
                            {
                                List<string> fileParts = vcxprojFile.Split('\\').ToList();
                                directoryName = fileParts.Count() >= 2 ? fileParts[fileParts.Count() - 2] : ""; //last is the filename, we want the directory
                            }
                            else
                            {
                                directoryName = name.Replace(rootPath, string.Empty);
                            }
                            zip.AddEntry(directoryName + "\\" + fileName, System.Text.Encoding.UTF8.GetBytes(String.Join("\n", ParseVcxproj(vcxprojFile)))); //rebuilds the vcxproj file from parsed string
                        }

                    }
                    else
                    {
                        foreach (string vcxprojFile in vcxprojFiles)
                        {
                            var name = Path.GetDirectoryName(vcxprojFile);
                            if (name == null) continue;
                            string directoryName = "";
                            string fileName = vcxprojFile.Split('\\').Last();

                            if (!vcxprojFile.Contains(rootPath))
                            {
                                List<string> fileParts = vcxprojFile.Split('\\').ToList();
                                directoryName = fileParts.Count() >= 2 ? fileParts[fileParts.Count() - 2] : ""; //last is the filename, we want the directory
                            }
                            else
                            {
                                directoryName = name.Replace(rootPath, string.Empty);
                            }
                            zip.AddFile(vcxprojFile, directoryName); //we don't have to parse this because there should be no relative paths
                        }
                    }
                }
                catch (Exception e)
                {
                    //TODO: handle errors
                }

                //need to parse the sln here before adding it to the zip. need to adjust referenced projects (if any)
                //rebuilds the sln file from parsed string to remove project relative paths
                zip.AddEntry(SolutionName.Split('\\').Last(), System.Text.Encoding.UTF8.GetBytes(String.Join("\n", ParseSolutionForRelativePaths(SolutionName))));

                zip.Save(stream);
                stream.Position = 0;

                SolutionData = stream.ToArray();
                return SolutionData;
            }
        }

        public void CreateSolutionBinary(byte[] fileData)
        {
            SolutionData = fileData;
        }
        private static List<string> GetSolutionFileList(string path)
        {
            string[] noDirectorySearchList = { "bin", "obj", "debug", "release", "ipch", "packages", ".vs" };
            string[] noFileExtension = { ".sdf", ".ipch", ".dll", ".db", ".opensdf", ".opendb" };
            var filesToAdd = new List<string>();
            if (path != null)
            {
                filesToAdd.AddRange(Directory.GetFiles(path).Where(file =>
                {
                    var extension = Path.GetExtension(file);
                    return extension != null && !noFileExtension.Contains(extension.ToLower());
                }));

                filesToAdd = (from directory in Directory.GetDirectories(path)
                              let directoryPieces = directory.ToLower().Split(Path.DirectorySeparatorChar)
                              let localDirectory = directoryPieces[directoryPieces.Length - 1]
                              where !noDirectorySearchList.Contains(localDirectory)
                              select directory)
                    .Aggregate(filesToAdd, (current, directory)
                        => current.Union(GetSolutionFileList(directory))
                            .ToList());
            }

            //parse .vcxproj to also add any files included by relative path
            List<int> indices = new List<int>();
            foreach (string item in filesToAdd)
            {
                if (item.Split('.').Last() == "vcxproj")
                {
                    indices.Add(filesToAdd.IndexOf(item));
                }
            }

            if (indices.Count() > 0)
            {
                foreach (int index in indices)
                {
                    filesToAdd.AddRange(AddRelativePathSourceFiles(filesToAdd[index]));
                }
            }

            return filesToAdd.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList(); //distinct because the recursive call above adds some files to the list twice
        }

        /// <summary>
        /// This is used to parse the .vcxproj file for relative path file includes and get the absolute path so we can add them to the solution zip.
        /// </summary>
        /// <param name="vcxprojPath">an absolute path to a .vcxproj file in the current solution</param>
        /// <returns>a list of strings, each string containing an absolute path to a file included in the .vcxproj file</returns>
        public static List<string> AddRelativePathSourceFiles(string vcxprojPath)
        {
            string rootPath = Path.GetDirectoryName(vcxprojPath);
            List<string> relativeSourceFiles = new List<string>();
            //Load vcxproj file to string
            List<string> vcxprojFile = File.ReadAllText(vcxprojPath).Replace("\r", String.Empty).Split('\n').ToList(); //convert the file to a list of strings

            foreach (string line in vcxprojFile)
            {
                if (line.Contains("Include") && !line.Contains("ProjectConfiguration")) //a file is being included, skip the ProjectConfiguration lines
                {
                    //parse the line
                    List<string> lineParts = line.Split('\"').ToList();
                    int indexOfPath = lineParts.FindIndex(lp => lp.Contains("Include")) + 1; //we want the path which is the next item after the include

                    if (indexOfPath > lineParts.Count() - 1) { continue; } //make sure we don't try an out of range index

                    lineParts[indexOfPath] = DecodeUrlString(lineParts[indexOfPath]); //need to decode special characters for local file path for zip.add e.g. '%40' will crash zip.add, so decode to '@'

                    if (lineParts[indexOfPath].Length > 0 && lineParts[indexOfPath][0] == '.')
                    {   //create absolute path from project path                            
                        string absolutePath = Path.Combine(rootPath, lineParts[indexOfPath]); //we need to get the absolute path so we can grab the file and add it to the zip.
                        string relativePath = Path.GetFullPath((new Uri(absolutePath)).LocalPath);
                        relativeSourceFiles.Add(rootPath + "$$$" + relativePath); //need to keep track of which project this file belongs to, will split on $$$ and use path to save to zip path
                    }
                    else //an include with an absolute path... 
                    {   //no need to check if it's already on the file list because .Distinct() will remove any duplicates                            
                        if (lineParts[indexOfPath].Split(':').Count() > 1) //split on thedrive path to make sure it's not a single file name and an absolute path instead
                        {
                            relativeSourceFiles.Add(rootPath + "$$$" + lineParts[indexOfPath]);    //need to keep track of which project this file belongs to, will split on $$$ and use path to save to zip path
                        }
                    }
                }
            }
            return relativeSourceFiles;
        }

        /// <summary>
        /// This is used to parse the .vcxproj file and modify each include to be in the root of the .vcxproj folder
        /// </summary>
        /// <param name="filePath">an absolute path to a .vcxproj file in the current solution</param>
        /// <returns>a list of strings with each string being a line in the modified .vcxproj file 
        /// (needs to be converted to the desired format before adding to the zipEntry)
        /// </returns>
        public static List<string> ParseVcxproj(string filePath)
        {
            List<string> vcxprojFile = File.ReadAllText(filePath).Replace("\r", String.Empty).Split('\n').ToList(); //convert the file to a list of strings
            List<string> modifiedVcxprojFile = new List<string>();
            foreach (string line in vcxprojFile) //for each line we want to find the relative file include paths and make them reference the root solution folder (in the submission) instead
            {
                if (line.Contains("Include") && !line.Contains("ProjectConfiguration")) //a file is being included, skip the ProjectConfiguration lines
                {
                    if (line.Contains("..") || line.Split(':').Count() > 1) //if the include has  relative path or there is an absolute path file reference we need to trim
                    {
                        //parse the line
                        List<string> lineParts = line.Split('\"').ToList();
                        int indexOfPath = lineParts.FindIndex(lp => lp.Contains("Include")) + 1; //we want the path which is the next item after the include

                        if (indexOfPath > lineParts.Count() - 1) { continue; } //make sure we don't try an out of range index

                        lineParts[indexOfPath - 1] = lineParts[indexOfPath - 1] + "\""; //adding opening quote back to the string
                        lineParts[indexOfPath + 1] = "\"" + lineParts[indexOfPath + 1]; //adding closing quote back to the string
                        lineParts[indexOfPath] = lineParts[indexOfPath].Split('\\').Last(); //get just the filename

                        modifiedVcxprojFile.Add(string.Join("", lineParts.ToArray()));
                    }
                    else
                    {
                        modifiedVcxprojFile.Add(line); //not a file include or already just the filename, just add it back to the list.
                    }
                }
                else
                {
                    modifiedVcxprojFile.Add(line); //not a file include, just add it back to the list.
                }
            }
            return modifiedVcxprojFile;
        }

        /// <summary>
        /// takes an absolute path to the current solution file and parses any relative project references so we can add all project files in external folders
        /// </summary>
        /// <param name="solutionFile">an absolute path to the .sln file for the current solution</param>
        /// <returns>a list of strings with each string being an absolute file path to a .vcxproj referenced in the solution .sln </returns>
        public static List<string> ParseSolutionForProjects(string solutionFile)
        {
            List<string> solutionFileLines = File.ReadAllText(solutionFile).Replace("\r", String.Empty).Split('\n').ToList(); //convert the file to a list of strings
            List<string> projectVcxprojFilePaths = new List<string>();
            string rootPath = Path.GetDirectoryName(solutionFile);

            foreach (string line in solutionFileLines)
            {
                if (line.Contains(".vcxproj")) // we only want lines with the .vcxproj paths
                {
                    List<string> lineParts = line.Split(',').ToList();
                    string rawPathValue = lineParts.Where(ln => ln.Contains(".vcxproj")).FirstOrDefault();
                    string vcxprojPath = rawPathValue.Trim().TrimStart('"').TrimEnd('"');
                    string absolutePath = Path.Combine(rootPath, vcxprojPath); //we need to get the absolute path so we can grab the file and add it to the zip.
                    string relativePath = Path.GetFullPath((new Uri(absolutePath)).LocalPath);
                    projectVcxprojFilePaths.Add(relativePath);
                }
            }
            return projectVcxprojFilePaths;
        }

        /// <summary>
        /// takes an absolute path to the current solution file and modifies any relative project references to be in the root of the zip submission
        /// </summary>
        /// <param name="solutionFile">an absolute path to the .sln file for the current solution</param>
        /// <returns>a list of strings with each string being a line in the modified .sln file 
        /// (needs to be converted to the desired format before adding to the zipEntry)
        /// </returns>
        public static List<string> ParseSolutionForRelativePaths(string solutionFile)
        {
            List<string> solutionFileLines = File.ReadAllText(solutionFile).Replace("\r", String.Empty).Split('\n').ToList(); //convert the file to a list of strings
            List<string> modifiedSolutionFile = new List<string>();

            foreach (string line in solutionFileLines)
            {
                if (line.Contains(".vcxproj") && line.Contains("..")) // we only want to modify lines with the .vcxproj paths which include a relative path
                {
                    List<string> lineParts = line.Split(',').ToList();
                    List<string> modifiedLine = new List<string>();

                    foreach (string part in lineParts) //rebuild the line
                    {
                        if (part.Contains(".vcxproj"))
                        {
                            List<string> partParts = part.Split('\\').ToList();
                            List<string> modifiedParts = new List<string>();
                            for (int i = partParts.Count() - 2; i < partParts.Count(); i++)
                            {   //we want to skip the first two parts which are .. and the root .sln folder in the external directory
                                modifiedParts.Add(partParts[i]);
                            }
                            string partialLine = string.Join("\\", modifiedParts.ToArray());
                            modifiedLine.Add(" \"" + partialLine); //we need to add the opening quote which was stripped off by skipping partParts[0]
                        }
                        else
                        {
                            modifiedLine.Add(part);
                        }
                    }
                    modifiedSolutionFile.Add(string.Join(",", modifiedLine.ToArray()));
                }
                else
                {
                    modifiedSolutionFile.Add(line); //not a project path, just add the line back to the list.
                }
            }
            return modifiedSolutionFile;
        }

        /// <summary>
        /// decodes file path to ensure no encoding characters remain in the file path (allows the absolute path to be used to add files to the zip)
        /// </summary>
        /// <param name="url">a string path/url</param>
        /// <returns>a string path/url with encoding values replaced with their corresponding character e.g. '%40' > '@'</returns>
        private static string DecodeUrlString(string url)
        {
            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(url)) != url)
                url = newUrl;
            return newUrl;
        }

        public override SqlCommand GetInsertCommand()
        {
            var cmd = new SqlCommand
            {
                CommandText = string.Format(@"
DECLARE {0} INT
INSERT INTO dbo.EventLogs (EventTypeId, EventDate, SenderId, CourseId) VALUES (@EventTypeId, @EventDate, @SenderId, @CourseId)
SELECT {0}=SCOPE_IDENTITY()
INSERT INTO dbo.SubmitEvents (EventLogId, EventDate, SolutionName, AssignmentId)
VALUES ({0}, @EventDate, @SolutionName, @AssignmentId)
SELECT {0}", StringConstants.SqlHelperLogIdVar)
            };

            cmd.Parameters.AddWithValue("EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("EventDate", EventDate);
            cmd.Parameters.AddWithValue("SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("SolutionName", SolutionName);
            cmd.Parameters.AddWithValue("AssignmentId", AssignmentId);

            return cmd;
        }
    }
}
