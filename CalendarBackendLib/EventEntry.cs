using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalendarBackendLib
{
    public class EventEntry : ICloneable
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string PrettyName { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
        public string EventNameOnFilesystem => GetEventnameOnFilesystem(Name);
        public string Filename => GetFilename(Name);

        [Obsolete("Use parameterized Constructor")]
        public EventEntry()
        { }

        public EventEntry(string name, DateTime startTime, DateTime endTime)
        {
            Name = name;
            StartTime = startTime;
            EndTime = endTime;
        }

        public EventEntry(string name, DateTime startTime, TimeSpan duration)
            : this(name, startTime, startTime + duration)
        { }


        public static string GetEventnameOnFilesystem(string eventname)
        {
            return eventname.Replace('/', '-');
        }

        public static string GetFilename(string eventname)
        {
            return GetEventnameOnFilesystem(eventname) + ".json";
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

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, StartTime);
        }

        public object Clone()
        {
            return new EventEntry(Name, StartTime, EndTime)
            {
                Description = Description,
                Location = Location,
                PrettyName = PrettyName
            };
        }
    }
}
