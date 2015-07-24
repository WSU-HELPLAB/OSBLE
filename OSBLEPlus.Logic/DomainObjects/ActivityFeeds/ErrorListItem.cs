using System;
using System.Text.RegularExpressions;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class ErrorListItem
    {
        public int Id { get; set; }

        public int Column { get; set; }

        public int Line { get; set; }

        public string File { get; set; }

        public string Project { get; set; }

        public string Description { get; set; }

        private string _criticalErrorName;

        public string CriticalErrorName
        {
            get
            {
                //storing result prevents multiple regex matches (should speed up execution time)
                if (_criticalErrorName == null)
                {
                    var pattern = "error ([^:]+)";
                    var match = Regex.Match(Description, pattern);

                    //ignore bad matches
                    _criticalErrorName = match.Groups.Count == 2 ? match.Groups[1].Value.ToLower().Trim() : string.Empty;
                }
                return _criticalErrorName;
            }
        }
    }
}
