using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OSBLEPlus.Logic.Utility.Lookups
{
    public class GetEnumDisplayName
    {
        /// <summary>
        /// Displays Description Tag attribute of any enum
        /// </summary>
        /// <param name="en">An enumerable with description labels</param>
        /// <returns></returns>
        public static string GetDisplayName(Enum en)
        {
            Type type = en.GetType();

            MemberInfo[] memInfo = type.GetMember(en.ToString());
            if (memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                    return ((DescriptionAttribute)attrs[0]).Description;
            }

            return en.ToString();
        }   
    }
}
