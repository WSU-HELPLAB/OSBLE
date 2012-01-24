﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class AssignmentType
    {
        [Key]
        public string Type { get; set; }

        public string Description { get; set; }

        public override string ToString()
        {
            return Type;
        }

        public string TypeWithoutSpaces
        {
            get
            {
                return Type.Replace(" ", string.Empty);
            }
        }

    }
}