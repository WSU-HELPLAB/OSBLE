using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;
using OSBLEPlus.Logic.Utility;

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
                var files = GetSolutionFileList(rootPath);
                if (rootPath == null || !files.Any())
                {
                    return null;
                }
                foreach (var file in files)
                {
                    var name = Path.GetDirectoryName(file);
                    if (name == null) continue;
                    var directoryName = name.Replace(rootPath, string.Empty);
                    zip.AddFile(file, directoryName);
                }
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
        private static IEnumerable<string> GetSolutionFileList(string path)
        {

            string[] noDirectorySearchList = { "bin", "obj", "debug", "release", "ipch", "packages" };
            string[] noFileExtension = { ".sdf", ".ipch", ".dll" };
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
            return filesToAdd.ToArray();
        }

        public override string GetInsertScripts()
        {
            var modifiedCourse = CourseId.ToString();
            modifiedCourse = modifiedCourse == string.Empty ? "NULL" : modifiedCourse;

            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId, CourseId) VALUES ({0}, '{1}', {2}, '{5}', {6})
SELECT {7}=SCOPE_IDENTITY()
INSERT INTO dbo.SubmitEvents (EventLogId, EventDate, SolutionName, AssignmentId)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}', {4}) SELECT {7}", EventTypeId, EventDate, SenderId, SolutionName, AssignmentId, BatchId, modifiedCourse, StringConstants.SqlHelperLogIdVar);
        }
    }
}
