using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLE.Models
{
    /// <summary>
    /// EventFilterSetting.NULL is used for internal bookkeeping.  It cannot be set by the user.
    /// </summary>
    [Flags]
    public enum EventFilterSetting : int
    {
        NULL = 1,
        BuildEvent = 2,
        ExceptionEvent = 4,
        FeedPostEvent = 8,
        AskForHelpEvent = 16,
        SubmitEvent = 32,
        LogCommentEvent = 64,
        HelpfulMarkGivenEvent = 128
    };

    public class UserFeedSetting
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual UserProfile User { get; set; }
        public DateTime SettingsDate { get; set; }
        public int CourseFilter { get; set; }
        public CourseRole CourseRoleFilter { get; set; }
        public int EventFilterSettings { get; set; }

        [NotMapped]
        public CourseRole CourseRole
        {
            get
            {
                
                return CourseRoleFilter;
            }
            set
            {
                CourseRoleFilter = value;
            }
        }
        public UserFeedSetting()
        {
            SettingsDate = DateTime.UtcNow;
        }

        public UserFeedSetting(UserFeedSetting other)
            : this()
        {
            this.Id = other.Id;
            this.EventFilterSettings = other.EventFilterSettings;
            this.UserId = other.UserId;
            this.SettingsDate = other.SettingsDate;
            this.CourseFilter = -1;
            this.CourseRole = other.CourseRole;
        }

        public bool HasSetting(EventFilterSetting setting)
        {
            int result = EventFilterSettings & (int)setting;
            return result == (int)setting;
        }

        public static EventType FeedOptionToOsbideEvent(EventFilterSetting option)
        {
            EventType evt;

            switch (option)
            {
                case EventFilterSetting.AskForHelpEvent:
                    evt = EventType.AskForHelpEvent;
                    break;
                case EventFilterSetting.BuildEvent:
                    evt = EventType.BuildEvent;
                    break;
                case EventFilterSetting.ExceptionEvent:
                    evt = EventType.ExceptionEvent;
                    break;
                case EventFilterSetting.FeedPostEvent:
                    evt = EventType.FeedPostEvent;
                    break;
                case EventFilterSetting.SubmitEvent:
                    evt = EventType.SubmitEvent;
                    break;
                case EventFilterSetting.HelpfulMarkGivenEvent:
                    evt = EventType.HelpfulMarkGivenEvent;
                    break;
                case EventFilterSetting.LogCommentEvent:
                    evt = EventType.LogCommentEvent;
                    break;
                default:
                    evt = EventType.Null;
                    break;
            }

            return evt;
        }

        public List<EventFilterSetting> ActiveSettings
        {
            get
            {
                List<EventFilterSetting> allSettings = Enum.GetValues(typeof(EventFilterSetting)).Cast<EventFilterSetting>().ToList();
                List<EventFilterSetting> userSettings = new List<EventFilterSetting>();
                foreach (EventFilterSetting setting in allSettings)
                {
                    if (HasSetting(setting) == true)
                    {
                        userSettings.Add(setting);
                    }
                }
                return userSettings;
            }
        }

        public bool HasSetting(EventType evt)
        {
            EventFilterSetting option = EventFilterSetting.NULL;
            if (evt == EventType.AskForHelpEvent)
            {
                option = EventFilterSetting.AskForHelpEvent;
            }
            else if (evt == EventType.BuildEvent)
            {
                option = EventFilterSetting.BuildEvent;
            }
            else if (evt == EventType.ExceptionEvent)
            {
                option = EventFilterSetting.ExceptionEvent;
            }
            else if (evt == EventType.FeedPostEvent)
            {
                option = EventFilterSetting.FeedPostEvent;
            }
            else if (evt == EventType.HelpfulMarkGivenEvent)
            {
                option = EventFilterSetting.HelpfulMarkGivenEvent;
            }
            else if (evt == EventType.LogCommentEvent)
            {
                option = EventFilterSetting.LogCommentEvent;
            }
            else if (evt == EventType.SubmitEvent)
            {
                option = EventFilterSetting.SubmitEvent;
            }
            return HasSetting(option);
        }

        public void SetSetting(EventType evt, bool value)
        {
            EventFilterSetting option = EventFilterSetting.NULL;
            if (evt == EventType.AskForHelpEvent)
            {
                option = EventFilterSetting.AskForHelpEvent;
            }
            else if (evt == EventType.BuildEvent)
            {
                option = EventFilterSetting.BuildEvent;
            }
            else if (evt == EventType.ExceptionEvent)
            {
                option = EventFilterSetting.ExceptionEvent;
            }
            else if (evt == EventType.FeedPostEvent)
            {
                option = EventFilterSetting.FeedPostEvent;
            }
            else if (evt == EventType.HelpfulMarkGivenEvent)
            {
                option = EventFilterSetting.HelpfulMarkGivenEvent;
            }
            else if (evt == EventType.LogCommentEvent)
            {
                option = EventFilterSetting.LogCommentEvent;
            }
            else if (evt == EventType.SubmitEvent)
            {
                option = EventFilterSetting.SubmitEvent;
            }
            if (option != EventFilterSetting.NULL)
            {
                SetSetting(option, value);
            }
        }

        public void SetSetting(EventFilterSetting setting, bool value)
        {
            switch (value)
            {
                case true:
                    AddSetting(setting);
                    break;
                case false:
                    RemoveSetting(setting);
                    break;
            }
        }

        protected void AddSetting(EventFilterSetting setting)
        {
            EventFilterSettings = (byte)(EventFilterSettings | (byte)setting);
        }

        protected void RemoveSetting(EventFilterSetting setting)
        {
            //~ is a bitwise not in c#
            //Doing a bitwise AND on a NOTed level should result in the level being removed
            EventFilterSettings = (byte)(EventFilterSettings & (~(byte)setting));
        }

    }
}
