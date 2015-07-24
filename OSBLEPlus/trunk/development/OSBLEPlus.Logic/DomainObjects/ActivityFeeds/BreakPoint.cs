using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class BreakPoint : IComparable, IEquatable<BreakPoint>
    {
        public int Id { get; set; }

        public string Condition { get; set; }

        public string File { get; set; }

        public int FileColumn { get; set; }

        public int FileLine { get; set; }

        public int FunctionColumnOffset { get; set; }

        public int FunctionLineOffset { get; set; }

        public string FunctionName { get; set; }

        public string Name { get; set; }

        public bool Enabled { get; set; }

        public BreakPoint()
        {
        }

        public int CompareTo(object obj)
        {
            var other = obj as BreakPoint;

            if (File == null && other == null) return 1;

            //same file?
            var result = string.Compare(File, other.File, StringComparison.OrdinalIgnoreCase);
            if (result != 0)
            {
                return result;
            }

            //same column?
            result = FileColumn.CompareTo(other.FileColumn);
            if (result != 0)
            {
                return result;
            }

            //same line?
            result = FileLine.CompareTo(other.FileLine);
            if (result != 0)
            {
                return result;
            }

            //same file, same column, same line: must be same breakpoint
            return 0;
        }

        public bool Equals(BreakPoint other)
        {
            return CompareTo(other) == 0;
        }
    }
}
