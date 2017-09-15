using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CalendarBackendLib
{
    public class CalendarHelper
    {
        private static readonly Calendar CALENDAR = new GregorianCalendar(GregorianCalendarTypes.Localized);
        private const CalendarWeekRule WEEK_RULE = CalendarWeekRule.FirstFourDayWeek;
        private const DayOfWeek FIRST_DAY_OF_WEEK = DayOfWeek.Monday;

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
            int firstWeek = CALENDAR.GetWeekOfYear(firstThursday, WEEK_RULE, FIRST_DAY_OF_WEEK);

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
