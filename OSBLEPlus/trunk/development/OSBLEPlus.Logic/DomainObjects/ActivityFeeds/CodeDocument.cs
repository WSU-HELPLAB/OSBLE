using System;
using System.Collections.Generic;
using System.Linq;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class CodeDocument
    {
        public int Id { get; set; }
        public string FileName { get; set; }

        private string _content = string.Empty;

        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                Lines = TextToList(Content);
            }
        }

        public List<CodeDocumentBreakPoint> BreakPoints { get; set; }
        public List<CodeDocumentErrorListItem> ErrorItems { get; set; }

        public static List<string> TextToList(string text)
        {
            return text.Split(new[] {"\r\n", "\n", Environment.NewLine}, StringSplitOptions.None).ToList();
        }

        public List<string> Lines { get; private set; }

        public CodeDocument()
        {
            BreakPoints = new List<CodeDocumentBreakPoint>();
            ErrorItems = new List<CodeDocumentErrorListItem>();
            Lines = new List<string>();
        }
    }
}