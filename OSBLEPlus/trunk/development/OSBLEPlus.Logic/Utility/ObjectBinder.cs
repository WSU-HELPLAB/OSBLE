using System;
using System.Reflection;

namespace OSBLEPlus.Logic.Utility
{
    public sealed class ObjectBinder : System.Runtime.Serialization.SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            var currentAssembly = Assembly.GetExecutingAssembly().FullName;

            // In this case we are always using the current assembly
            assemblyName = currentAssembly;

            // Get the type using the typeName and assemblyName
            return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName));
        }
    }
}