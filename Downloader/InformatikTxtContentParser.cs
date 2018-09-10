using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Downloader
{
    internal class InformatikTxtContentParser
    {
        private static readonly Regex YEAR_REGEX = new Regex(@"Stundenplan.+(\d{4}) Vers.+vom.+");
        private static readonly Regex WEEKS_REGEX = new Regex(@"^[\d-]+(?:, [\d-]+)*$");
        private static readonly Regex EVENT_REGEX = new Regex(@"^(.+),([^,]*),([^,]*),([^,]+),(\d{1,2}:\d{2}),(\d{1,2}:\d{2})$");
        private static readonly Regex[] IGNORE_LINES = new Regex[] {
            YEAR_REGEX,
            new Regex("Semestergruppe.+"),
            new Regex("Name,Dozent,Raum,Tag,Anfang,Ende")
        };

        private static readonly string[] WEEKDAYS = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };

        public static EventEntry[] GetEvents(string informatikTxtContent)
        {
            var events = new List<EventEntry>();

            var allLines = informatikTxtContent
                .Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var year = int.Parse(YEAR_REGEX.Match(allLines.First()).Groups[1].Value);

            var lines = allLines
                .Where(line => IGNORE_LINES.All(r => !r.IsMatch(line)))
                .ToArray();

            var currentWeeks = new int[0];

            foreach (var line in lines)
            {
                if (WEEKS_REGEX.IsMatch(line))
                {
                    currentWeeks = GetWeeksFromWeekList(line);
                    continue;
                }

                var match = EVENT_REGEX.Match(line);
                if (match != null)
                {
                    events.AddRange(GetEventsFromMatch(year, currentWeeks, match));
                    continue;
                }

                throw new NotImplementedException();
            }

            return events
                .Distinct()
                .ToArray();
        }

        private static int[] GetWeeksFromWeekList(string weekList)
        {
            var entries = weekList
                .Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim());

            var directWeeks = entries
                .Where(o => !o.Contains('-'))
                .Select(o => int.Parse(o))
                .ToArray();

            var weekgroups = entries
                .Where(o => o.Contains('-'))
                .SelectMany(o =>
                {
                    var splitted = o.Split('-').Select(s => int.Parse(s));
                    var start = splitted.First();
                    var count = splitted.Last() - start + 1;
                    return Enumerable.Range(start, count);
                })
                .ToArray();

            var result = directWeeks
                .Concat(weekgroups)
                .Distinct()
                .OrderBy(o => o)
                .ToArray();

            return result;
        }

        private static IEnumerable<EventEntry> GetEventsFromMatch(int year, int[] weeks, Match match)
        {
            return weeks.Select(week => GetEventFromMatch(year, week, match));
        }

        private static EventEntry GetEventFromMatch(int year, int week, Match match)
        {
            var name = match.Groups[1].Value.Trim();
            var dozent = match.Groups[2].Value.Trim();
            var room = match.Groups[3].Value.Trim();
            var weekday = match.Groups[4].Value.Trim();
            var startTime = TimeSpan.Parse(match.Groups[5].Value);
            var endTime = TimeSpan.Parse(match.Groups[6].Value);

            var dayOfWeek = (DayOfWeek)Array.IndexOf(WEEKDAYS, weekday);
            var date = CalendarHelper.DateOfWeekDayISO8601(year, week, dayOfWeek);
            var start = date.Add(startTime);
            var end = date.Add(endTime);

            var desc = string.IsNullOrWhiteSpace(dozent) ? "" : "Prof: " + dozent;

            return new EventEntry(name, start, end)
            {
                Description = desc,
                Location = room
            };
        }
    }
}
