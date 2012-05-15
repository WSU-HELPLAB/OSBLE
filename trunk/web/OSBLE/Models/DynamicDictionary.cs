using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Dynamic;

namespace OSBLE.Models
{
    public class DynamicDictionary : DynamicObject
    {
        // The inner dictionary.
        private Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }

        public Dictionary<string, object>.KeyCollection Keys
        {
            get
            {
                return dictionary.Keys;
            }
        }

        public Dictionary<string, object>.ValueCollection Values
        {
            get
            {
                return dictionary.Values;
            }
        }

        public object this[string s]
        {
            get
            {
                object result;
                dictionary.TryGetValue(s, out result);
                return result;
            }
            set
            {
                dictionary[s] = value;
            }
        }

        public object this[int i]
        {
            get
            {
                return this[i.ToString()];
            }
            set
            {
                this[i.ToString()] = value;
            }
        }

        // If you try to get a value of a property 
        // not defined in the class, this method is called.
        public override bool TryGetMember(
            GetMemberBinder binder, out object result)
        {
            // Converting the property name to lowercase
            // so that property names become case-insensitive.
            string name = binder.Name.ToLower();

            // If the property name is found in a dictionary,
            // set the result parameter to the property value and return true.
            // Otherwise, return false.
            return dictionary.TryGetValue(name, out result);
        }

        // If you try to set a value of a property that is
        // not defined in the class, this method is called.
        public override bool TrySetMember(
            SetMemberBinder binder, object value)
        {
            // Converting the property name to lowercase
            // so that property names become case-insensitive.
            dictionary[binder.Name.ToLower()] = value;

            // You can always add a value to a dictionary,
            // so this method always returns true.
            return true;
        }
    }
}