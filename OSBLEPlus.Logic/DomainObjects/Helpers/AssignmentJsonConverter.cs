using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OSBLE.Interfaces;
using OSBLEPlus.Logic.DomainObjects.Profiles;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public class AssignmentJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ICourse));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(ICourse))
                return JObject.Load(reader).ToObject<ProfileCourse>(serializer);

            return null;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
