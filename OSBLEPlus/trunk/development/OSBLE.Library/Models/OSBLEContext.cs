using System.Data.Entity;

namespace OSBLE.Models
{
    public class OSBLEContext : ContextBase
    {
        // You can add custom code to this file. Changes will not be overwritten.

        public OSBLEContext()
            : base("OSBLEData")
        {
            Database.SetInitializer<OSBLEContext>(null);
        }

        public const string UnGradableCatagory = "Un-Graded Assignments";
        public const string ProfessionalSchool = "Professional";
    }
}
