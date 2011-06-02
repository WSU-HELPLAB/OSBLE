using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Gradables.StudioAssignment.Activities
{
    public interface IAssignmentActivity
    {
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Release Date")]
        DateTime ReleaseDate { get; set; }
    }
}