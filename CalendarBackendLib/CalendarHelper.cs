using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CalendarBackendLib
{
    public class CalendarHelper
    {
        public static DateTime DateOfWeekDayISO8601(int year, int week, DayOfWeek weekday)
        {
            var firstDayOfWeek = FirstDateOfWeekISO8601(year, week);
            var weekdayNumber = (int)weekday - 1; // Monday is the first day, so it has to be 0
            return firstDayOfWeek.AddDays(weekdayNumber);
        }

        // https://stackoverflow.com/questions/662379/calculate-date-from-week-number
        // yaay, works with the week 54+ stuff ootb
        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }
    }
}
