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
}