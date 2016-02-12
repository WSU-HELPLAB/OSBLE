using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

using OSBLEPlus.Logic.DomainObjects.Interface;

namespace OSBLEPlus.Logic.Utility
{
    public static class ActivitySerializationHelper
    {
        public static byte[] Serialize(this List<IActivityEvent> logs)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, logs);

                return stream.ToArray();
            }
        }

        public static List<IActivityEvent> Deserialize(this byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
            {
                return ((IActivityEvent[])formatter.Deserialize(stream)).ToList();
            }
        }
    }
}
