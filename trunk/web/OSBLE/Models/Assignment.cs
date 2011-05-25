using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public enum FileLocationType
    {
        /// <summary>
        /// This means the Location string is the 'file'
        /// </summary>
        Text,

        /// <summary>
        /// This means the Location string is a URL
        /// </summary>
        WebLink,

        /// <summary>
        /// This means the Location string points to a file on the OSBLE file system
        /// </summary>
        UploadedFile
    }

    public enum DelieverableType
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

        /// <summary>
        /// This file type is not supported by the Osble Review process and thus a Review process will not be allowed for this
        /// file
        /// </summary>
        [FileExtensions(new string[] { ".*" })]
        Other
    }

    public class FileExtensions : Attribute
    {
        private List<string> extensions;

        public List<string> Extensions
        {
            get
            {
                return extensions;
            }
        }

        public FileExtensions(string[] fileExtensions)
        {
            this.extensions = new List<string>(fileExtensions);
        }
    }

    public class Assignment : AbstractGradable
    {
        public class FileLocation
        {
            public string Location { get; set; }

            public FileLocationType LocationType { get; set; }
        }

        public class Delieverable
        {
            public DelieverableType Type { get; set; }

            public string Name { get; set; }
        }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        public DateTime ReleaseDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public FileLocation Description { get; set; }

        [Required]
        public List<Delieverable> Delieverables { get; set; }

        [Required]
        public bool isGradeable { get; set; }

        [Required]
        public bool isTeam { get; set; }

        [Required]
        public int PossiblePoints { get; set; }

        [Required]
        public bool InstructorCanReview { get; set; }

        //NEED RUBRIC
    }
}