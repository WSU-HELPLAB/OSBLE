using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Annotate
{
    public class AnnotateDocumentReference
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string OsbleDocumentCode { get; set; }

        [Required]
        public string AnnotateDocumentCode { get; set; }

        [Required]
        public string AnnotateDocumentDate { get; set; }
    }
}
