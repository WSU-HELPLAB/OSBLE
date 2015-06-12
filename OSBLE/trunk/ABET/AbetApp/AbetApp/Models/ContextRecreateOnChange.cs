using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace AbetApp.Models
{
    public class ContextRecreateOnChange : DropCreateDatabaseIfModelChanges<LocalContext>
    {
    }
}