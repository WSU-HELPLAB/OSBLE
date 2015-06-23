using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLEPlus.Logic.Utility
{
    public enum VsComponent
    {
        None,
        FeedOverview,
        FeedDetails,
        Chat,
        UserProfile,
        CreateAccount,
        AskTheProfessor,
        GenericComponent
    };

    public enum DebugActions
    {
        Undefined = -1,
        Start = 0,
        StepOver = 1,
        StepInto = 2,
        StepOut = 3,
        StopDebugging = 4,
        StartWithoutDebugging = 5,
    };

    public enum ErrorsConsidered
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        All = 6,
    }
    public enum TimeScale
    {
        Days = 1,
        Hours = 2,
        Minutes = 3,
    }
    public enum ProgrammingState
    {
        idle,
        debug_sem_u,
        debug_sem_n,
        run_sem_u,
        run_sem_n,
        run_last_success,
        edit_syn_u_sem_u,
        edit_syn_y_sem_u,
        edit_syn_y_sem_n,
        edit_syn_n_sem_u,
        edit_syn_n_sem_n,
    }
    public enum ProcedureType
    {
        ErrorQuotient = 1,
        WatwinScoring = 2,
        DataVisualization = 3,
        CalendarVisualization = 4,
    }
    public enum ResultViewType
    {
        Tabular = 1,
        Bar = 2,
        Scatter = 3,
        Bubble = 4,
    }
    public enum FileUploadSchema
    {
        CSV = 1,
        Survey = 2,
        Grade = 3,
    }
    public enum CategoryColumn
    {
        InstitutionId,
        Name,
        Gender,
        Age,
        Class,
        Ethnicity,
    }
    public enum AggregateFunction
    {
        Total,
        Avg,
    }

    public class EnumListItem
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
    public class Enum<T>
    {
        public static List<EnumListItem> Get()
        {
            return (from object e in Enum.GetValues(typeof(T)) select new EnumListItem { Value = (int)e, Text = (Enum.GetName(typeof(T), e)).ToDisplayText() }).ToList();
        }
    }

    public static class NameExtension
    {
        private static readonly List<string> LowerCaseWords = new List<string> { "Of", "To", "Per", "At", "In", "On" };
        public static string ToDisplayText(this string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var text = new StringBuilder(name.Length * 2);
            text.Append(name[0]);
            for (var idx = 1; idx < name.Length; idx++)
            {
                if (char.IsUpper(name[idx]))
                {
                    if (name[idx - 1] != ' ' && !char.IsUpper(name[idx - 1]) && idx < name.Length - 1 && !char.IsUpper(name[idx + 1]))
                    {
                        text.Append(' ');
                    }
                }
                text.Append(name[idx]);
            }

            var returnString = text.ToString();
            LowerCaseWords.ForEach(x =>
            {
                returnString = returnString.Replace(string.Format(" {0} ", x), string.Format(" {0} ", x.ToLower()));
            });

            return returnString;
        }
    }
}
