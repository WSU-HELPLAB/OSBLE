using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;

namespace OSBLE.Models
{
    /// <summary>
    /// Provides an interface that will allow a model to specify advanced relationships between
    /// itslef and other models.
    /// </summary>
    public interface IModelBuilderExtender
    {
        /// <summary>
        /// When implemented by a database model, will be called when constructing the DB
        /// context.
        /// </summary>
        /// <param name="modelBuilder"></param>
        void BuildRelationship(DbModelBuilder modelBuilder);
    }
}
