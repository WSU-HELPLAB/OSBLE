using System;

namespace ReviewInterfaceBase.HelperClasses
{
    public static class HelperClass
    {
        public static string ConvertTimeSpanToString(TimeSpan timeSpan)
        {
            //Milliseconds go to the 100 place and since we can barely see centiseconds no reason for it a break our format
            //so convert it centiseconds by dividing by ten and rounding the reminder.
            int centiseconds = (int)Math.Round(timeSpan.Milliseconds / 10.0, 0);

            //represent the time in a common string format
            string[] str = new string[] { timeSpan.Hours.ToString(), timeSpan.Minutes.ToString(), timeSpan.Seconds.ToString(), centiseconds.ToString() };

            int i = 0;
            while (i < str.Length)
            {
                if (str[i].Length < 2)
                {
                    str[i] = str[i].Insert(0, "0");
                }
                i++;
            }
            return String.Join(":", str);
        }
    }
}