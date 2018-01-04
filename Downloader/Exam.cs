using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Downloader
{
    public class Exam
    {
        private const string CSV_REGEX = @"^([^,]+),([^,]+),([^,]+),([^,]+),(""?[^""]+""?),([^,]+)";
        private const string DATE_FORMAT = "d.MM.yyyy";

        public string Name;
        public DateTime StartTime;
        public string Type;
        public string Prof;
        public string Room;

        public Exam() { }

        public Exam(string name, DateTime startTime, string type, string prof, string room)
        {
            Name = name;
            StartTime = startTime;
            Type = type;
            Prof = prof;
            Room = room;
        }

        public static Exam ParseFromCsvLine(string csvLine)
        {
            try
            {
                var match = new Regex(CSV_REGEX).Match(csvLine);

                var name = match.Groups[1].Value.Trim();
                var date = match.Groups[2].Value.Trim();
                var time = match.Groups[3].Value.Trim();
                var type = match.Groups[4].Value.Trim();
                var prof = match.Groups[5].Value.Trim().Trim('"').Trim();
                var room = match.Groups[6].Value.Trim();

                var startTime = DateTime.ParseExact(date, DATE_FORMAT, null).Add(TimeSpan.Parse(time));

                return new Exam(name, startTime, type, prof, room);
            }
            catch (Exception ex)
            {
                throw new Exception("Something failed with line " + csvLine, ex);
            }
        }

        public EventEntry ToEventEntry()
        {
            var description = Type + "\nProf: " + Prof;

            return new EventEntry(Name, StartTime, TimeSpan.Parse("2:00"))
            {
                Description = description,
                Location = Room,
                PrettyName = Name + " " + Type
            };
        }
    }
}
