using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parser
{
    internal class IcsGenerator
    {
        private const string ICS_PREFIX = @"BEGIN:VCALENDAR
VERSION:2.0
METHOD:PUBLISH
PRODID:https://calendarbot.hawhh.de
";

        private const string ICS_TIMEZONE = @"BEGIN:VTIMEZONE
TZID:Europe/Berlin
BEGIN:DAYLIGHT
TZOFFSETFROM:+0100
RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=-1SU
DTSTART:19810329T020000
TZNAME:CEST
TZOFFSETTO:+0200
END:DAYLIGHT
BEGIN:STANDARD
TZOFFSETFROM:+0200
RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU
DTSTART:19961027T030000
TZNAME:CET
TZOFFSETTO:+0100
END:STANDARD
END:VTIMEZONE
";

        private const string ICS_SUFFIX = @"
END:VCALENDAR
";

        public static string GenerateIcsContent(string calendarName, IEnumerable<EventEntry> events)
        {
            var icsContent = ICS_PREFIX.Replace("\r\n", "\n");
            icsContent += "X-WR-CALNAME:@HAWHHCalendarBot (" + calendarName + ")\n";
            icsContent += ICS_TIMEZONE.Replace("\r\n", "\n");

            icsContent += string.Join("\n", events.Select(GenerateIcsVEventOfEventEntry));
            icsContent += ICS_SUFFIX;

            icsContent = icsContent.Replace("\n", "\r\n");

            return icsContent;
        }

        private static string GenerateIcsVEventOfEventEntry(EventEntry e)
        {
            const string icsDateTimeFormat = "yyyyMMddTHHmmss";
            var name = string.IsNullOrWhiteSpace(e.PrettyName) ? e.Name : e.PrettyName;

            var content = new Dictionary<string, string>();
            content.Add("BEGIN", "VEVENT");
            content.Add("TRANSP", "OPAQUE");
            content.Add("STATUS", e.Status.ToString().ToUpper());

            content.Add("SUMMARY", name);
            content.Add("DTSTART;TZID=Europe/Berlin", e.StartDateTime.ToString(icsDateTimeFormat));
            content.Add("DTEND;TZID=Europe/Berlin", e.EndDateTime.ToString(icsDateTimeFormat));

            if (!string.IsNullOrWhiteSpace(e.Location))
                content.Add("LOCATION", e.Location.Replace(",", "\\,")); // , has to be escaped in the LOCATION field
            if (!string.IsNullOrWhiteSpace(e.Description))
                content.Add("DESCRIPTION", e.Description.Replace("\n", "\\n"));

            content.Add("URL;VALUE=URI", "https://t.me/HAWHHCalendarBot");
            content.Add("UID", GetHashString(e) + "@calendarbot.hawhh.de");
            content.Add("END", "VEVENT");

            var asText = string.Join("\n", content.Select(o => o.Key + ":" + o.Value));
            return asText;
        }

        private static string GetHashString(EventEntry e)
        {
            //TODO: this changes every time it generates
            var nameHash = StringToHexHash(e.Name);

            var dateHash = e.StartDateTime.Date.GetHashCode().ToString("x");

            var startHash = DoubleToHex(e.StartDateTime.TimeOfDay.TotalMinutes);
            var endHash = DoubleToHex(e.EndDateTime.TimeOfDay.TotalMinutes);

            var locationHash = StringToHexHash(e.Location);
            var descriptionHash = StringToHexHash(e.Description);

            return nameHash + dateHash + startHash + endHash + locationHash + descriptionHash;
        }

        private static string DoubleToHex(double value)
        {
            var intVal = Convert.ToInt64(value);
            return intVal.ToString("x");
        }

        private static string StringToHexHash(string content)
        {
            var val = (content ?? string.Empty)
                .Select(o => Convert.ToDouble((int)o))
                .Sum();

            return DoubleToHex(val);
        }
    }
}
