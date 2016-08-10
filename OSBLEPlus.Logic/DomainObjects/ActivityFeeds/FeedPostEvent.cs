using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class FeedPostEvent : ActivityEvent
    {
        private const string HashRegex = "#[A-Za-z][A-Za-z0-9]+";
        private const string MentionRegex = "@[A-Za-z][A-Za-z0-9]+";

        public string Comment { get; set; }

        public FeedPostEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int) EventType.FeedPostEvent;
        }

        public FeedPostEvent(DateTime dateTimeValue) : this()
        {
            EventDate = dateTimeValue;
        }

        public override SqlCommand GetInsertCommand()
        {
            var cmd = new SqlCommand();

            var sql = string.Format(@"
DECLARE {0} INT
INSERT INTO dbo.EventLogs (EventTypeId, EventDate, DateReceived, SolutionName, SenderId, CourseId, EventVisibilityGroups, EventVisibleTo)
VALUES (@EventTypeId, @EventDate, @DateReceived, @SolutionName, @SenderId, @CourseId, @EventVisibilityGroups, @EventVisibleTo)
SELECT {0}=SCOPE_IDENTITY()
INSERT INTO dbo.FeedPostEvents (EventLogId, EventDate, SolutionName, Comment)
VALUES ({0}, @EventDate, @SolutionName, @Comment)
SELECT {0}", StringConstants.SqlHelperLogIdVar);

            cmd.Parameters.AddWithValue("@EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("@EventDate", EventDate);
            cmd.Parameters.AddWithValue("@DateReceived", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@SolutionName", string.IsNullOrWhiteSpace(SolutionName) ? string.Empty : SolutionName);
            cmd.Parameters.AddWithValue("@SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("@Comment", Comment);
            cmd.Parameters.AddWithValue("@EventVisibilityGroups", EventVisibilityGroups);
            cmd.Parameters.AddWithValue("@EventVisibleTo", EventVisibleTo);

            //This is overwriting the sql insert command that this is calling
            //We need to do this insert separately
            //var hashTags = GetMentionTags();
            //var userTags = GetMentionTags();
            //if (hashTags.Count > 0 || userTags.Count > 0)
            //{
            //    sql =
            //        string.Format(
            //            "{0}{1}SET {2}=SCOPE_IDENTITY(){1}EXEC dbo.InsertPostTags @postId={2}, @usertags, @hashtags",
            //            sql, Environment.NewLine, StringConstants.SqlHelperLogIdVar);

            //    cmd.Parameters.AddWithValue("@usertags", userTags);
            //    cmd.Parameters.AddWithValue("@hashtags", hashTags);
            //}

            cmd.CommandText = sql;

            return cmd;
        }

        public List<string> GetHashTags()
        {
            return
                Regex.Matches(Comment, HashRegex)
                    .Cast<Match>()
                    .Select(match => match.Value.Substring(1))
                    .ToList();
        }

        public List<string> GetMentionTags()
        {
            return
                Regex.Matches(Comment, MentionRegex)
                    .Cast<Match>()
                    .Select(match => match.Value.Substring(1))
                    .ToList();
        }
    }
}
