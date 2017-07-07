using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalendarBackendLib
{
    public class EventEntry
    {
        public readonly string Name;
        public readonly string Location;
        public readonly string Description;
        public readonly DateTime StartTime;
        public readonly DateTime EndTime;
        public TimeSpan Duration => EndTime - StartTime;

        public EventEntry(string name, DateTime startTime, DateTime endTime, string location = "", string description = "")
        {
            Name = name;
            Location = location;
            Description = description;
            StartTime = startTime;
            EndTime = endTime;
        }

        public EventEntry(string name, DateTime startTime, TimeSpan duration, string location = "", string description = "")
            : this(name, startTime, startTime + duration, location, description)
        { }


        public string GetIcsVEventBlock()
        {
            const string icsDateTimeFormat = "yyyyMMddTHHmmss";

            var content = new Dictionary<string, string>();
            content.Add("BEGIN", "VEVENT");

            content.Add("SUMMARY", Name);
            content.Add("LOCATION", Location);
            content.Add("DTSTART;TZID=Europe/Berlin", StartTime.ToString(icsDateTimeFormat));
            content.Add("DTEND;TZID=Europe/Berlin", EndTime.ToString(icsDateTimeFormat));

            if (!string.IsNullOrWhiteSpace(Description))
                content.Add("DESCRIPTION", Description);

            content.Add("UID", GetHashString() + "@calendarbot.hawhh.de");
            content.Add("END", "VEVENT");

            var asText = string.Join("\n", content.Select(o => o.Key + ":" + o.Value));
            return asText;
        }


        public bool Equals(EventEntry other)
        {
            if (other == null)
            {
                return false;
            }

            if (Name != other.Name) return false;
            if (StartTime != other.StartTime) return false;
            if (EndTime != other.EndTime) return false;
            if (Description != other.Description) return false;
            if (Location != other.Location) return false;

            return true;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            return Equals(obj as EventEntry);
        }

        public override int GetHashCode()
        {
            return StartTime.GetHashCode();
        }

        public string GetHashString()
        {
            var nameHash = Name.GetHashCode().ToString("x");

            var dateHash = StartTime.Date.GetHashCode().ToString("x");

            var startHash = DoubleToHex(StartTime.TimeOfDay.TotalMinutes);
            var endHash = DoubleToHex(EndTime.TimeOfDay.TotalMinutes);

            var locationHash = Location.GetHashCode().ToString("x");
            var descriptionHash = Description.GetHashCode().ToString("x");

            return nameHash + dateHash + startHash + endHash + locationHash + descriptionHash;
        }

        private string DoubleToHex(double value)
        {
            var intVal = Convert.ToInt64(value);
            return intVal.ToString("x");
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, StartTime);
        }
    }
}
