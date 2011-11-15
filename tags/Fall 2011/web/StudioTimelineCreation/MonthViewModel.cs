using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace CreateNewAssignment
{
    public class MonthViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public event EventHandler MonthChanged = delegate { };
        public event MouseButtonEventHandler MouseRightButtonDown = delegate { };

        public static readonly string[] MonthNamesInOrder = new string[]
        {
            "January",
            "February",
            "March",
            "April",
            "May",
            "June",
            "July",
            "August",
            "September",
            "October",
            "November",
            "December"
        };

        private List<CalendarDayItemView> calendarDays;
        MonthView thisView = new MonthView();
        MonthModel thisModel = new MonthModel();

        public string MonthYearString
        {
            get
            {
                return thisModel.Month;
            }
        }

        public DateTime MonthYear
        {
            get
            {
                return thisModel.MonthYear;
            }
            set
            {
                thisModel.MonthYear = value;
                thisModel.Month = GetMonthName(thisModel.MonthYear.Month) + " " + thisModel.MonthYear.Year.ToString();
                setCalanderDates();

                PropertyChanged(this, new PropertyChangedEventArgs("MonthYearString"));
                PropertyChanged(this, new PropertyChangedEventArgs("MonthYear"));
                MonthChanged(this, EventArgs.Empty);
            }
        }

        public MonthView GetView()
        {
            return thisView;
        }

        public MonthViewModel()
        {
            thisView.DataContext = this;

            var days = from p
                       in thisView.MonthLayout.Children
                       where p is CalendarDayItemView
                       select p as CalendarDayItemView;

            calendarDays = new List<CalendarDayItemView>(days);

            foreach (CalendarDayItemView day in calendarDays)
            {
                day.MouseRightButtonDown += new MouseButtonEventHandler(day_MouseRightButtonDown);
            }
        }

        public CalendarDayItemView GetCalendarDayItemView(DateTime dateTime)
        {
            //Check to make sure we are in the right month and year
            if (MonthYear.Month == dateTime.Month && MonthYear.Year == dateTime.Year)
            {
                //Make sure the the day we are looking for is a valid day i.e cant be Fed 30th (but based on month and year)
                if (dateTime.Day <= DateTime.DaysInMonth(MonthYear.Year, MonthYear.Month))
                {
                    //dateTime.Day will gives us an index lets 5 but we got to offset it by the number empty days in our calendar minus one to make it an index
                    return calendarDays[dateTime.Day + (int)(new DateTime(MonthYear.Year, MonthYear.Month, 1)).DayOfWeek - 1];
                }
            }
            return null;
        }

        private void day_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //just pass it up
            MouseRightButtonDown(sender, e);
        }

        private string GetMonthName(int month)
        {
            month = month - 1 % 12;
            return MonthNamesInOrder[month];
        }

        private void setCalanderDates()
        {
            //finds the first day of the month and the corresponding CalanderDayItemView
            DateTime dateTime = new DateTime(MonthYear.Year, MonthYear.Month, 1);
            int i = 0;
            while ((int)dateTime.DayOfWeek != i)
            {
                //clear out whatever might be there
                calendarDays[i].ClearData();

                //we aren't using it so make like it aint there
                calendarDays[i].IsEnabled = false;
                i++;
            }

            //sets the CalanderDayItemView text to the correct day
            for (int j = 1; j <= DateTime.DaysInMonth(dateTime.Year, dateTime.Month); j++)
            {
                //clear out whatever might be there
                calendarDays[i].ClearData();

                calendarDays[i].IsEnabled = true;
                calendarDays[i].DateString = j.ToString();
                calendarDays[i].MyDate = new DateTime(dateTime.Year, dateTime.Month, j);
                i++;
            }

            while (i < calendarDays.Count)
            {
                //clear out whatever might be there
                calendarDays[i].ClearData();

                //we aren't using it so make like it aint there
                calendarDays[i].IsEnabled = false;
                i++;
            }
        }
    }
}