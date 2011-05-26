using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
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
    }
}