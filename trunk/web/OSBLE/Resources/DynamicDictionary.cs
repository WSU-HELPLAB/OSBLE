using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Dynamic;

namespace OSBLE.Resources
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

        /// <summary>
        /// Merges two <see cref="DynamicDictionary"/> objects.  In the case of duplicate keys, the 
        /// keys existing in other will overwrite the calling object's keys.
        /// </summary>
        /// <param name="other">The other dictionary to merge</param>
        /// <returns></returns>
        public void Merge(DynamicDictionary other)
        {
            foreach (string key in other.Keys)
            {
                this[key] = other[key];
            }
        }

        /// <summary>
        /// Merges two <see cref="DynamicDictionary"/> objects.  In the case of duplicate keys, the values in
        /// <paramref name="second"/> will overwrite those in <paramref name="first"/>;
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static DynamicDictionary Merge(DynamicDictionary first, DynamicDictionary second)
        {
            DynamicDictionary merged = new DynamicDictionary();
            merged.Merge(first);
            merged.Merge(second);
            return merged;
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

            dictionary.TryGetValue(name, out result);
            return true;
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