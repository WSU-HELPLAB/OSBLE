using System;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public enum DeliverableType
    {
        [FileExtensions(new string[] { ".c", ".cpp", ".h", ".cs", ".py", ".java" })]
        Code,

        [FileExtensions(new string[] { ".txt" })]
        Text,

        [FileExtensions(new string[] { ".cpml" })]
        ChemProV,

        [FileExtensions(new string[] { ".xps" })]
        XPS,

        [FileExtensions(new string[] { ".wmv", ".mp4" })]
        Video,

        [FileExtensions(new string[] { ".zip" })]
        Zip,
        /// <summary>
        /// This file type is not supported by the Osble Review process and thus a Review process will not be allowed for this
        /// file
        /// </summary>
        [FileExtensions(new string[] { ".*" })]
        Other
    }

    /// <summary>
    /// This Attribute is meant to hold a list of file extensions
    /// </summary>
    public class FileExtensions : Attribute
    {
        private string[] extensions;

        public string[] Extensions
        {
            get
            {
                return extensions;
            }
        }

        public FileExtensions(string[] fileExtensions)
        {
            this.extensions = fileExtensions;
        }
    }

    public class Deliverable
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Type")]
        public DeliverableType Type { get; set; }

        [Required]
        [Display(Name = "File Name")]
        public string Name { get; set; }

        [Required]
        public int AssignmentActivityID { get; set; }

        public AssignmentActivity AssignmentActivity { get; set; }
    }
}