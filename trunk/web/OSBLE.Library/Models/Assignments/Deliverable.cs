using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace OSBLE.Models.Assignments
{
    public enum DeliverableType
    {
        [FileExtensions(new string[] { ".c", ".cpp", ".h", ".cs", ".py", ".java" })]
        Code,

        [FileExtensions(new string[] { ".txt" })]
        Text,

        [FileExtensions(new string[] { ".cpml" })]
        ChemProV,

        [FileExtensions(new string[] { ".wmv", ".mp4" })]
        Video,

        [FileExtensions(new string[] { ".zip" })]
        Zip,

        [FileExtensions(new string[] { ".txt" })]
        InBrowserText,

        [FileExtensions(new string[] {".ppt", ".pptx"})]
        PowerPoint,

        [FileExtensions(new string[] { ".doc", ".docx" })]
        WordDocument,

        [FileExtensions(new string[] { ".pdf" })]
        PDF,

        [FileExtensions(new string[] { ".xls", ".xlsx"})]
        ExcelSpreadSheet,
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
        public Deliverable()
        {
        }

        public Deliverable(Deliverable other)
        {
            if (other == null)
            {
                return;
            }
            this.AssignmentID = other.AssignmentID;
            this.Type = other.Type;
            this.Name = other.Name;
        }

        [Key]
        [Required]
        [Column(Order = 0)]
        public int AssignmentID { get; set; }
        public Assignment Assignment { get; set; }

        [Key]
        [Required(ErrorMessage = "The deliverable must have a name")]
        [Column(Order = 1)]
        [Display(Name = "File Name")]
        [RegularExpression(@"^[a-zA-Z0-9\._\-]*$",
            ErrorMessage = "File names can only contain alphanumerics, '-', '_', and '.'")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Type")]
        public int Type { get; set; }

        [NotMapped]
        public DeliverableType DeliverableType
        {
            get
            {
                return (DeliverableType)this.Type;
            }
            set
            {
                this.Type = (int)value;
            }
        }

        public string[] FileExtensions
        {
            get
            {
                return Deliverable.GetFileExtensions(this.DeliverableType);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", Name, DeliverableType.ToString());
        }

        public static string[] GetFileExtensions(DeliverableType deliverableType)
        {
            Type type = deliverableType.GetType();

            FieldInfo fi = type.GetField(deliverableType.ToString());

            //we get the attributes of the selected language
            FileExtensions[] attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

            //make sure we have more than (should be exactly 1)
            if (attrs.Length > 0 && attrs[0] is FileExtensions)
            {
                return attrs[0].Extensions;
            }
            else
            {
                //throw and exception if not decorated with any attrs because it is a requirement
                throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
            }
        }

    }
}
