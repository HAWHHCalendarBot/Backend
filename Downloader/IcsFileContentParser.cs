using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Downloader
{
    internal static class IcsFileContentParser
    {
        private static readonly Regex EVENT_REGEX = new Regex(@"BEGIN:VEVENT\nSUMMARY:(.+)\r?\nLOCATION:(.+)\nUID:(.+)\nDTSTART;TZID=Europe\/Berlin:(.+)\nDTEND;TZID=Europe\/Berlin:(.+)\nEND:VEVENT");

        internal static EventEntry[] GetEvents(string icsFileContent)
        {
            var events = EVENT_REGEX.Matches(icsFileContent)
                .OfType<Match>()
                .Select(GetEventEntryFromEventRegexMatch)
                .ToArray();

            return events;
        }

        private static EventEntry GetEventEntryFromEventRegexMatch(Match match)
        {
            var name = match.Groups[1].Value;
            var locationMixed = match.Groups[2].Value;
            var uid = match.Groups[3].Value;

            var start = GetDateTimeFromIcsTimeString(match.Groups[4].Value);
            var end = GetDateTimeFromIcsTimeString(match.Groups[5].Value);

            var locationOffset = locationMixed.IndexOf("  Stand ");
            var location = locationMixed.Substring(0, locationOffset);

            return new EventEntry(name, start, end, location);
        }

        private static DateTime GetDateTimeFromIcsTimeString(string isoString)
        {
            var modified = isoString;

            modified = modified.Insert(13, ":");
            modified = modified.Insert(11, ":");

            modified = modified.Insert(6, "/");
            modified = modified.Insert(4, "/");


            return DateTime.Parse(modified);
        }
    }
}
