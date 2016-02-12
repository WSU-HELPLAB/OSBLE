using System.Collections.Generic;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public enum MeasureCategory
    {
        ProgrammingEffort,
        CompilationBehavior,
        ExecutionBehavior,
        SocialBehavior,
    }

    public enum MeasureType
    {
        //ProgrammingEffort category
        ActiveStudents,
        LinesOfCodeWritten,
        TimeSpent,

        //CompilationBehavior category
        NumberOfCompilations,
        NumberOfErrorsPerCompilation,

        //ExecutionBehavior category
        NumberOfNoDebugExecutions,
        NumberOfDebugExecutions,
        NumberOfBreakpointsSet,
        NumberOfRuntimeExceptions,

        //SocialBehavior category
        NumberOfPosts,
        NumberOfReplies,
        TimeToFirstReply,
    }

    public class MeasureDefinition
    {
        public MeasureType MeasureType { get; set; }
        public char DataPointShape { get; set; }
        public string Color { get; set; }
        public AggregateFunction? AggregateFunction { get; set; }
    }

    public static class MeasureDefinitions
    {
        public static Dictionary<MeasureCategory, List<MeasureDefinition>> All = new Dictionary<MeasureCategory, List<MeasureDefinition>>
        {
            {
                MeasureCategory.ProgrammingEffort,
                new List<MeasureDefinition>
                {
                    new MeasureDefinition{MeasureType = MeasureType.ActiveStudents, DataPointShape='S', Color="#CCFF33", AggregateFunction=AggregateFunction.Total},
                    new MeasureDefinition{MeasureType = MeasureType.LinesOfCodeWritten, DataPointShape='C', Color="#339933"},
                    new MeasureDefinition{MeasureType = MeasureType.TimeSpent, DataPointShape='T', Color="#66CC33"},
                }
            },
            {
                MeasureCategory.CompilationBehavior,
                new List<MeasureDefinition>
                {
                    new MeasureDefinition{MeasureType = MeasureType.NumberOfCompilations, DataPointShape='P', Color="#3399CC"},
                    new MeasureDefinition{MeasureType = MeasureType.NumberOfErrorsPerCompilation, DataPointShape='E', Color="#66CCFF"},
                }
            },
            {
                MeasureCategory.ExecutionBehavior,
                new List<MeasureDefinition>
                {
                    new MeasureDefinition{MeasureType = MeasureType.NumberOfNoDebugExecutions, DataPointShape='R', Color="#500000"},
                    new MeasureDefinition{MeasureType = MeasureType.NumberOfDebugExecutions, DataPointShape='D', Color="#A00000"},
                    new MeasureDefinition{MeasureType = MeasureType.NumberOfBreakpointsSet, DataPointShape='B', Color="#663366"},
                    new MeasureDefinition{MeasureType = MeasureType.NumberOfRuntimeExceptions, DataPointShape='X', Color="#996633"},
                }
            },
            {
                MeasureCategory.SocialBehavior,
                new List<MeasureDefinition>
                {
                    new MeasureDefinition{MeasureType = MeasureType.NumberOfPosts, DataPointShape='O', Color="#CC9933"},
                    new MeasureDefinition{MeasureType = MeasureType.NumberOfReplies, DataPointShape='I', Color="#CCCC33"},
                    new MeasureDefinition{MeasureType = MeasureType.TimeToFirstReply, DataPointShape='F', Color="#CC6633", AggregateFunction=AggregateFunction.Avg},
                }
            },
        };
    }
}
