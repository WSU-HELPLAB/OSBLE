namespace System.ServiceModel.DomainServices.EntityFramework
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Data.Entity;
    using System.Diagnostics.Contracts;
    using System.ServiceModel.DomainServices.Server;

    // taken from http://social.msdn.microsoft.com/Forums/en-US/adonetefx/thread/57793bec-abc6-4520-ac1d-a63e40239aed/
    public static class DbContextExtensions
    {
        public static void Insert<T>(this DbContext context, T entity) where T : class
        {
            Contract.Requires(context != null);
            Contract.Requires(entity != null);

            // Note: changing the state to Added on a detached entity is the same as calling
            // Add on the DbSet.
            context.Entry(entity).State = EntityState.Added;
        }

        public static void Update<T>(this DbContext context, T entity) where T : class
        {
            Contract.Requires(context != null);
            Contract.Requires(entity != null);

            // Note: changing the state to Modified on a detached entity is the same as
            // calling Attach and then setting state to Modified.
            context.Entry(entity).State = EntityState.Modified;
        }

        public static void Update<T>(this DbContext context, T current, T original) where T : class
        {
            Contract.Requires(context != null);
            Contract.Requires(current != null);
            Contract.Requires(original != null);

            var entry = context.Entry(current);
            entry.State = EntityState.Unchanged;
            entry.OriginalValues.SetValues(original);

            var properties = TypeDescriptor.GetProperties(typeof(T));
            var attributes = TypeDescriptor.GetAttributes(typeof(T));

            foreach (var propertyName in entry.CurrentValues.PropertyNames)
            {
                var descriptor = properties[propertyName];
                if (descriptor != null &&
                    descriptor.Attributes[typeof(RoundtripOriginalAttribute)] == null &&
                    attributes[typeof(RoundtripOriginalAttribute)] == null &&
                    descriptor.Attributes[typeof(ExcludeAttribute)] == null)
                {
                    entry.Property(propertyName).IsModified = true;
                }
            }

            // Not sure what is going on here.  If you get to this point and the state is not Modified,
            // then it means all the values of current are the same as those of original and no property
            // was set to Modified explicitly in the loop above.  This means that there should be nothing
            // to write to the database.  If you then set the state to Modified it means all the properties
            // will be marked as Modified which means they will ALL be written to the database.  So it seems
            // likely that the following lines should not be here.
            if (entry.State != EntityState.Modified)
            {
                entry.State = EntityState.Modified;
            }
        }

        public static void Delete<T>(this DbContext context, T entity) where T : class
        {
            Contract.Requires(context != null);
            Contract.Requires(entity != null);

            context.Entry(entity).State = EntityState.Deleted;
        }
    }
}