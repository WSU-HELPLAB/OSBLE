using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using Ionic.Zip;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.Tests.Activities
{
    [TestClass]
    public class PostsTests
    {
        [TestMethod]
        public void CanPostAskForHelpEvent()
        {
            // Arrange
            var log = new AskForHelpEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                Code = "dummy code",
                CourseId = 1,
                UserComment = "dummy user comment"
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<AskForHelpEvent>(
                        "select b.EventLogId, a.EventTypeId, b.Code, a.SenderId, b.SolutionName, b.UserComment, a.CourseId " +
                        "from EventLogs a " +
                        "inner join AskForHelpEvents b on b.EventLogId=a.Id and a.Id=@id",
                        new {@Id = result}).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.Code == log.Code);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.UserComment == log.UserComment);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
            }
        }

        [TestMethod]
        public void CanPostBuildEvent()
        {
            // Arrange
            var build = new BuildEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                CourseId = 1,
            };

            build.ErrorItems.Add(new BuildEventErrorListItem
            {
                BuildEvent = build,
                ErrorListItem = new ErrorListItem
                {
                    Project = "Dummy Project",
                    Column = 1,
                    Line = 2,
                    File = "Dummy File Name",
                    Description = "Dummy Desc"
                }
            });

            build.Breakpoints.Add(new BuildEventBreakPoint
            {
                BreakPoint = new BreakPoint
                {
                    Condition = "Dummy Condition",
                    File = "Dummy File",
                    FileColumn = 1,
                    FileLine = 1,
                    FunctionColumnOffset = 0,
                    FunctionLineOffset = 0,
                    FunctionName = "Dummy FunctionName",
                    Name = "Dummy bk Name",
                    Enabled = true
                },
                BuildEvent = build
            });

            build.Documents.Add(new BuildDocument{
                Build = build,
                Document = new CodeDocument
                                {
                                    FileName = "Dummy File",
                                    Content = "Dummy code doc content"
                                }});

            // Act
            var result = Posts.SaveEvent(build);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<BuildEvent>(
                        @"select b.EventLogId, l.EventTypeId, l.EventDate, l.SenderId, EventId=b.Id, b.SolutionName, l.CourseId
                            from BuildEvents b
                            inner join EventLogs l on l.Id=b.EventLogId
                            where l.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == build.SenderId);
                Assert.IsTrue(savedlog.EventType == build.EventType);
                Assert.IsTrue(savedlog.SolutionName == build.SolutionName);
                Assert.IsTrue(savedlog.CourseId == build.CourseId);
            }
        }

        [TestMethod]
        public void CanPostCutCopyPasteEvent()
        {
            // Arrange
            var log = new CutCopyPasteEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                EventActionId = (int)CutCopyPasteActions.Copy,
                CourseId = 1,
                DocumentName = "dummy document",
                Content = "dummy content"
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<CutCopyPasteEvent>(
                        @"select b.EventLogId, a.EventTypeId, b.Content, a.SenderId, b.SolutionName, b.DocumentName, a.CourseId, EventActionId=b.EventAction
                            from EventLogs a
                            inner join CutCopyPasteEvents b on b.EventLogId=a.Id and a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.EventActionId == log.EventActionId);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.DocumentName == log.DocumentName);
                Assert.IsTrue(savedlog.Content == log.Content);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
            }
        }

        [TestMethod]
        public void CanPostDebugEvent()
        {
            // Arrange
            var log = new DebugEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                ExecutionAction = 1,
                CourseId = 1,
                DocumentName = "dummy document",
                DebugOutput = "dummy debug output",
                LineNumber = 1
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<DebugEvent>(
                        @"select b.EventLogId, a.EventTypeId, b.DebugOutput, a.SenderId, b.SolutionName, b.DocumentName, a.CourseId, b.LineNumber
                            from EventLogs a
                            inner join DebugEvents b on b.EventLogId=a.Id and a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.LineNumber == log.LineNumber);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.DocumentName == log.DocumentName);
                Assert.IsTrue(savedlog.DebugOutput == log.DebugOutput);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
            }
        }

        [TestMethod]
        public void CanPostEditorActivityEvent()
        {
            // Arrange
            var log = new EditorActivityEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                CourseId = 1
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<EditorActivityEvent>(
                        @"select b.EventLogId, a.EventTypeId,a.SenderId, b.SolutionName, a.CourseId
                        from EventLogs a
                        inner join EditorActivityEvents b on b.EventLogId=a.Id and a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
            }
        }

        [TestMethod]
        public void CanPostExceptionEvent()
        {
            // Arrange
            var log = new ExceptionEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                CourseId = 1,
                DocumentName = "Dummy Exception Document Name",
                ExceptionAction = 1,
                ExceptionCode = 1,
                ExceptionDescription = "Dummy Exception Desc",
                ExceptionType = "Dummy Exception Type",
                ExceptionName = "Dummy Exception Name",
                LineContent = "Dummy Exception Line Content",
                LineNumber = 1
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<ExceptionEvent>(
                        @"select b.EventLogId, a.EventTypeId,a.SenderId, b.SolutionName, a.CourseId,
                        b.DocumentName, b.ExceptionAction, b.ExceptionCode, b.ExceptionDescription, b.ExceptionType,
                        b.ExceptionName, b.LineContent, b.LineNumber
                        from EventLogs a
                        inner join ExceptionEvents b on b.EventLogId=a.Id and a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
                Assert.IsTrue(savedlog.DocumentName == log.DocumentName);
                Assert.IsTrue(savedlog.ExceptionAction == log.ExceptionAction);
                Assert.IsTrue(savedlog.ExceptionCode == log.ExceptionCode);
                Assert.IsTrue(savedlog.ExceptionDescription == log.ExceptionDescription);
                Assert.IsTrue(savedlog.ExceptionType == log.ExceptionType);
                Assert.IsTrue(savedlog.ExceptionName == log.ExceptionName);
                Assert.IsTrue(savedlog.LineContent == log.LineContent);
                Assert.IsTrue(savedlog.LineNumber == log.LineNumber);
            }
        }

        [TestMethod]
        public void CanPostFeedPostEvent()
        {
            // Arrange
            var log = new FeedPostEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                CourseId = 1,
                Comment = "dummy comment"
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<FeedPostEvent>(
                        @"select b.EventLogId, a.EventTypeId, a.SenderId, b.SolutionName, a.CourseId, b.Comment
                            from EventLogs a
                            inner join FeedPostEvents b on b.EventLogId=a.Id and a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.Comment == log.Comment);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
            }
        }

        [TestMethod]
        public void CanPostLogCommentEvent()
        {
            // Arrange
            var log = new LogCommentEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                CourseId = 1,
                SourceEventLogId = 1,
                Content = "Dummy log comment"
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<LogCommentEvent>(
                        @"select b.EventLogId, a.EventTypeId, a.SenderId, b.SolutionName, a.CourseId, b.SourceEventLogId, b.Content
                            from EventLogs a
                            inner join LogCommentEvents b on b.EventLogId=a.Id and a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
                Assert.IsTrue(savedlog.SourceEventLogId == log.SourceEventLogId);
                Assert.IsTrue(savedlog.Content == log.Content);
            }
        }

        [TestMethod]
        public void CanPostHelpfulMarkEvent()
        {
            // Arrange
            var log = new HelpfulMarkGivenEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                CourseId = 1,
                LogCommentEventId = 1
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<HelpfulMarkGivenEvent>(
                        @"select b.EventLogId, a.EventTypeId, a.SenderId, b.SolutionName, a.CourseId, b.LogCommentEventId
                            from EventLogs a
                            inner join HelpfulMarkGivenEvents b on b.EventLogId=a.Id and a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
                Assert.IsTrue(savedlog.LogCommentEventId == log.LogCommentEventId);
            }
        }

        [TestMethod]
        public void CanPostSaveEvent()
        {
            // Arrange
            var log = new SaveEvent
            {
                SenderId = 1,
                SolutionName = "Dummy Solution",
                CourseId = 1,
                Document = new CodeDocument
                {
                    FileName = "Dummy Save File",
                    Content = "Dummy save code doc content"
                }
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<SaveEvent>(
                        @"select b.EventLogId, a.EventTypeId, a.SenderId, b.SolutionName, a.CourseId, b.DocumentId
                            from EventLogs a
                            inner join SaveEvents b on b.EventLogId=a.Id
                            inner join CodeDocuments c on c.Id=b.DocumentId
                            where a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);

                var codedoc =
                        connection.Query<CodeDocument>(
                            @"select Id, [FileName], Content from CodeDocuments where Id=@id",
                            new { @Id = savedlog.DocumentId }).SingleOrDefault();
                Assert.IsTrue(codedoc != null);
                Assert.IsTrue(codedoc.FileName == log.Document.FileName);
                Assert.IsTrue(codedoc.Content == log.Document.Content);
            }
        }

        [TestMethod]
        public void CanPostSubmitEvent()
        {
            // Arrange
            var log = new SubmitEvent
            {
                SolutionName = "dummy submit solution",
                CourseId = 4,
                AssignmentId = 1,
                SenderId = 1,
                Sender = new User
                {
                    FirstName = "Test",
                    LastName = "User"
                }
            };

            // Act
            var result = Posts.SaveEvent(log);

            // Assert
            using (var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var savedlog =
                    connection.Query<SubmitEvent>(
                        @"select b.EventLogId, a.EventTypeId, a.SenderId, b.SolutionName, a.CourseId, b.AssignmentId
                            from EventLogs a
                            inner join SubmitEvents b on b.EventLogId=a.Id
                            where a.Id=@id",
                        new { @Id = result }).SingleOrDefault();

                Assert.IsTrue(savedlog != null);
                Assert.IsTrue(savedlog.SenderId == log.SenderId);
                Assert.IsTrue(savedlog.EventType == log.EventType);
                Assert.IsTrue(savedlog.SolutionName == log.SolutionName);
                Assert.IsTrue(savedlog.CourseId == log.CourseId);
                Assert.IsTrue(savedlog.AssignmentId == log.AssignmentId);
            }
        }

        [TestMethod]
        public void TestSubmission()
        {
            //TODO: update this to work with the new filesytem helper tool
            /*var submit = CreateSubmitEvent();
            string path = Directory.GetCurrentDirectory();
            Posts.SaveToFileSystem(submit, path);
            var fullpath = path + "\\Courses\\4\\Assignments\\3\\1\\Test User\\testfile.txt";
            File.SetAttributes(fullpath, FileAttributes.Normal);
            bool success = File.Exists(fullpath);
            File.Delete(fullpath);
            Directory.Delete(path+"\\Courses", true);
            Assert.IsTrue(success);*/
        }

        #region setup helpers
        private static SubmitEvent CreateSubmitEvent()
        {
            var submit = new SubmitEvent
            {
                //SolutionName = "C:/SubmissionTest/Source/TestSolution.sln",
                CourseId = 4,
                AssignmentId = 3,
                SenderId = 1,
                Sender = new User
                {
                    FirstName = "Test",
                    LastName = "User"
                }
            };
            //submit.GetSolutionBinary();
            string path = "testfile.txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("CourseId = 4");
                sw.WriteLine("AssignmentId = 3");
                sw.WriteLine("SenderId = 1");
                sw.WriteLine("Name = Test User");
            }
            var stream = new MemoryStream();
            using (var zip = new ZipFile()){
                zip.AddFile(path);
                zip.Save(stream);
                stream.Position = 0;

                submit.CreateSolutionBinary(stream.ToArray());
            }
            File.Delete(path);
            return submit;
        }
        #endregion
    }
}
