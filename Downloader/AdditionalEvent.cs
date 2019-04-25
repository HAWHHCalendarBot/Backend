using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Downloader
{
    public class AdditionalEvent
    {
        public string name { get; set; }
        public string room { get; set; }

        public string date { get; set; }
        public string month { get; set; }
        public string year { get; set; }
        public string starttime { get; set; }
        public string endtime { get; set; }

        public DateTime StartDate => new DateTime(int.Parse(year), int.Parse(month), int.Parse(date));
        public DateTime StartTime => StartDate.Add(TimeSpan.Parse(starttime));
        public DateTime EndTime => StartDate.Add(TimeSpan.Parse(endtime));

        public EventEntry GetEventEntry()
        {
            return new EventEntry(name, StartTime, EndTime)
            {
                Description = "Dies ist eine zusätzliche, inoffizielle Veranstaltung: https://github.com/HAWHHCalendarBot/AdditionalEvents",
                Location = room
            };
        }
    }
}
