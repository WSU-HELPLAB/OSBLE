using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class FeedPostEvent : ActivityEvent
    {
        private const string HashRegex = "#[A-Za-z][A-Za-z0-9]+";
        private const string MentionRegex = "@[A-Za-z][A-Za-z0-9]+";

        public string Comment { get; set; }
        public FeedPostEvent() { } // NOTE!! This is required by Dapper ORM

        public override string GetInsertScripts()
        {
            var sql = string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, CreatedDate, SenderId) VALUES ({0}, '{1}', '{2}', {3}, {6})
INSERT INTO dbo.FeedPostEvents (EventLogId, EventDate, SolutionName, Comment)
VALUES (SCOPE_IDENTITY(), '{1}', '{4}', '{5}')", EventTypeId, EventDate, DateTime.Now, SenderId, SolutionName, Comment, BatchId);

            var hashTags = GetMentionTags();
            var userTags = GetMentionTags();
            if (hashTags.Count > 0 || userTags.Count > 0)
            {
                sql =
                    string.Format(
                        "{0}{1}SET {2}=SCOPE_IDENTITY(){1}EXEC dbo.InsertPostTags @postId={2}, @usertags='{3}',@hashtags='{4}'",
                        sql, Environment.NewLine, StringConstants.SqlHelperScopeIdentityName, string.Join(",", userTags), string.Join(",", hashTags));
            }

            return sql;
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
