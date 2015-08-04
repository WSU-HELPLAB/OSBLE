using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
