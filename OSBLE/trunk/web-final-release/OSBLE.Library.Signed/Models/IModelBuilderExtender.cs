using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;

namespace OSBLE.Models
{
    public interface IModelBuilderExtender
    {
        void BuildRelationship(DbModelBuilder modelBuilder);
    }
}
