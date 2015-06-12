using System;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.HomePage
{
    public class Event : IModelBuilderExtender
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int PosterID { get; set; }

        public virtual CourseUser Poster { get; set; }

        [Required]
        [Display(Name = "Starting date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [NotMapped]
        [Display(Name = "Starting time")]
        [DataType(DataType.Time)]
        public DateTime StartTime
        {
            get
            {
                return StartDate;
            }
            set
            {
                //first, zero out the release date's time component
                this.StartDate = DateTime.Parse(StartDate.ToShortDateString());
                StartDate = StartDate.AddHours(value.Hour);
                StartDate = StartDate.AddMinutes(value.Minute);
            }
        }

        [Display(Name = "Ending date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [NotMapped]
        [Display(Name = "Ending time")]
        [DataType(DataType.Time)]
        public DateTime? EndTime
        {
            get
            {
                return EndDate;
            }
            set
            {
                if (EndDate.HasValue) //Only handle if EndDate is set
                {
                    //first, zero out the release date's time component
                    this.EndDate = DateTime.Parse(EndDate.Value.ToShortDateString());
                    EndDate = EndDate.Value.AddHours(value.Value.Hour);
                    EndDate = EndDate.Value.AddMinutes(value.Value.Minute);
                }
            }
        }

        [Required]
        [Display(Name = "Event Title")]
        [StringLength(100)]
        public string Title { get; set; }

        [Display(Name = "Description (Optional)")]
        [StringLength(500)]
        public string Description { get; set; }

        public bool Approved { get; set; }

        [NotMapped]
        public bool HideTime { get; set; }

        [NotMapped]
        public bool HideDelete { get; set; }

        [NotMapped]
        public bool NoDateTime { get; set; }

        public Event()
            : base()
        {
            StartDate = DateTime.UtcNow.Date;

            NoDateTime = false;

            HideDelete = false;
            HideTime = false;
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>()
                .HasRequired(m => m.Poster)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }
    //Young Chon: attempting to add ical information
    public class icalEvent : IModelBuilderExtender
    {

        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int PosterID { get; set; }

        public virtual CourseUser Poster { get; set; }

        [Required]
        [Display(Name = "Starting date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [NotMapped]
        [Display(Name = "Starting time")]
        [DataType(DataType.Time)]
        public DateTime StartTime
        {
            get
            {
                return StartDate;
            }
            set
            {
                //first, zero out the release date's time component
                this.StartDate = DateTime.Parse(StartDate.ToShortDateString());
                StartDate = StartDate.AddHours(value.Hour);
                StartDate = StartDate.AddMinutes(value.Minute);
            }
        }

        [Display(Name = "Ending date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [NotMapped]
        [Display(Name = "Ending time")]
        [DataType(DataType.Time)]
        public DateTime? EndTime
        {
            get
            {
                return EndDate;
            }
            set
            {
                if (EndDate.HasValue) //Only handle if EndDate is set
                {
                    //first, zero out the release date's time component
                    this.EndDate = DateTime.Parse(EndDate.Value.ToShortDateString());
                    EndDate = EndDate.Value.AddHours(value.Value.Hour);
                    EndDate = EndDate.Value.AddMinutes(value.Value.Minute);
                }
            }
        }

        [Required]
        [Display(Name = "Event Title")]
        [StringLength(100)]
        public string Title { get; set; }

        [Display(Name = "Description (Optional)")]
        [StringLength(500)]
        public string Description { get; set; }

        public bool Approved { get; set; }

        [NotMapped]
        public bool HideTime { get; set; }

        [NotMapped]
        public bool HideDelete { get; set; }

        [NotMapped]
        public bool NoDateTime { get; set; }


        // only supports version 2.0
        // will need to check for an rrule, if theres not then there is on 12 things per event, if there is then it is 13 things per event
        
        public icalEvent() : base()
        {
            _dtstart = null;
            _dtend = null;
            _rrule = null;
            _dtstamp = null;
            _uid = null;
            _created = null;
            _description = null;
            _lastmod = null;
            _loc = null;
            _sequence = null;
            _status = null;
            _summary = null;
            _transp = null;
        }
        public icalEvent
            (string dtstart, string dtend, string rrule, string dtstamp,
            string uid, string created, string description, string lastmod,
            string loc, string sequence, string status, string summary, string transp)
        {
            _dtstart = dtstart;
            _dtend = dtend;
            _rrule = rrule;
            _dtstamp = dtstamp;
            _uid = uid;
            _created = created;
            _description = description;
            _lastmod = lastmod;
            _loc = loc;
            _sequence = sequence;
            _status = status;
            _summary = summary;
            _transp = transp;
        }
        public icalEvent(Event existingOsble)
        {
            _dtstart = this.MakeStartDate(existingOsble.StartDate);
            _dtend = this.MakeEndDate(existingOsble.EndDate);
            _rrule = null;
            _dtstamp = null;
            _uid = null;
            _created = null;
            _description = null;
            _lastmod = null;
            _loc = null;
            _sequence = null;
            _status = null;
            _summary = null;
            _transp = null;
        }


        public string _dtstart, _dtend, _rrule, _dtstamp, _uid, _created, _description, _lastmod, _loc, _sequence, _status, _summary, _transp;

        public DateTime osbleStart, osbleEnd; //dates

        public string osbleTitle, osbleDescription;

        public int year, month, day, hour, min, sec;

        /* test print in console
        public void print()
        {
            Console.WriteLine(_dtstart + " \n"); //= dtstart;
            Console.WriteLine(_dtend + " \n"); //= dtend;
            Console.WriteLine(_rrule + " \n"); //= rrule;
            Console.WriteLine(_dtstamp + " \n");// = dtstamp;
            Console.WriteLine(_uid + " \n");// = uid;
            Console.WriteLine(_created + " \n");// = created;
            Console.WriteLine(_description + " \n");// = description;
            Console.WriteLine(_lastmod + " \n");// = lastmod;
            Console.WriteLine(_loc + " \n"); //= //loc;
            Console.WriteLine(_sequence + " \n"); //= sequence;
            Console.WriteLine(_status + " \n"); //= status;
            Console.WriteLine(_summary + " \n"); //= summary;
            Console.WriteLine(_transp + " \n"); //= transp;
        }*/

        public void getStartDateAndTime(string fmt)
        {
            _dtstart = fmt;
            string[] format = fmt.Split(':');                   // Splits fmt into [0]DTSTART, [1]TZID=..., [2]YYYYMMDDTHHMMSS

            //string[] dateAndTime = format[2].Split('T');        //format[2] = YYYYMMDDTHHMMSS -> dateAndTime [0]YYYYMMDD, [1]HHMMSS
            //string dateFromSource = dateAndTime[0];             // WARNING ATTEMPT TO MAKE: DATE[0-3]=YYYY, DATE[4-5]=MM, DATE[6-7]=DD  
            //string timeFromSource = dateAndTime[1];             // WARNING ATTEMPT TO MAKE: TIME[0-1]=HH, TIME[2-3]=DD, TIME[4-5]=SS
            //string s_year = dateFromSource.Substring(0, 4);     //s_year = YYYY
            //string s_month = dateFromSource.Substring(4, 2);    // s_month = MM
            //string s_day = dateFromSource.Substring(6, 2);      // s_day = DD
            //string s_hour = timeFromSource.Substring(0, 2);     // s_hour = HH
            //string s_min = timeFromSource.Substring(2, 2);      // s_min = MM
            //string s_sec = timeFromSource.Substring(4, 2);      // s_sec = SS
            //


            //// DATE
            //int year = int.Parse(s_year);
            //int month = int.Parse(s_month);
            //int day = int.Parse(s_day);
            //// TIME
            //int hour = int.Parse(s_hour);
            //int min = int.Parse(s_min);
            //int sec = int.Parse(s_sec);


        }

        public void getEventNameAndDiscription(string fmt)
        {

        }


        //need to be able to access osble event items, but that can be done later

        public void loadOsbleEventData()//
        {
            int i;
            string[] tok;
            //start == DTSTART:TZID=America/Los_Angeles:20131007T100000
            tok = _dtstart.Split(':'); //tok[0] == dstart, other than that, we dont know if tzid will be there
            for (i = 1; i < 3; i++)
            {
                if (tok[i].Substring(0, 1) != "T")
                {
                    break;
                }
            }
            //tok[i] contains the time
            osbleStart = this.convertIcsDateTime(tok[i].ToString());
            //end
            tok = _dtend.Split(':'); //tok[0] == dstart, other than that, we dont know if tzid will be there
            for (i = 1; i < 3; i++)
            {
                if (tok[i].Substring(0, 1) != "T")
                {
                    break;
                }
            }
            //tok[i] contains the time
            osbleEnd = this.convertIcsDateTime(tok[i].ToString());
            //title
            tok = _summary.Split(':');
            osbleTitle = tok[1].ToString();

            //description this section may be optional in ics
            if (_description == null)
                osbleDescription = null;
            else
            {
                tok = _description.Split(':');
                osbleDescription = tok[1].ToString();
            }

            //Console.WriteLine("Start: {0}\nEnd: {1}\nTitle: {2}\nDescription: {3}", osbleStart, osbleEnd, osbleTitle, osbleDescription);
            //Console.WriteLine("test start date: {0}\nstart time: {1}", osbleStart.Date, osbleStart.TimeOfDay);

        }

        public void convertToOsble() // need event object to pass in, and take Vevent and put into in (Event e)
        {
            //uncomment 
            //e.StartDate = osbleStart
            //but we need to make sure we get the item
        }

        //this conversion changes ics format for any date time

        public DateTime convertIcsDateTime(string fmt)
        {
            //19700308T020000  == fmt
            //there might be a Z at teh end indicating utc time is now
            //yyyymmddThhmmss
            //want 1970-03-08 02:00:00 
            string icsDate = fmt.Substring(0, 4) + "-" + fmt.Substring(4, 2) + "-" + fmt.Substring(6, 2) + " " + fmt.Substring(9, 2) + ":" +
                fmt.Substring(11, 2) + ":" + fmt.Substring(13, 2);

            DateTime convert;
            DateTime.TryParse(icsDate, out convert);
            return convert;

        }


        public string OSBLE_DateToIcal_Date(DateTime Date)
        {
           return (Date.Year.ToString() + Date.Month.ToString() + Date.Day.ToString() +
                "T" + Date.Hour.ToString() + Date.Minute.ToString() + "00"); //seconds set to zero still need to get time zone somehow 
        }

        public string MakeStartDate(DateTime StartDate)
        {
            return "DTSTART:TZID=America/Los_Angeles:" + OSBLE_DateToIcal_Date(StartDate);
        }

        public string MakeEndDate(DateTime? EndDate)
        {
            return "DTEND:TZID=America/Los_Angeles:" + OSBLE_DateToIcal_Date((DateTime)EndDate);
        }

        public string MakeDescription(string Description)
        {
            return "DESCRIPTION:" + Description;
        }

        public string MakeSummary(string Title)
        {
            return "SUMMARY:" + Title;
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<icalEvent>()
                .HasRequired(m => m.Poster)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }

}