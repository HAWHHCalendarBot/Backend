using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parser
{
    public class Userconfig
    {
        public Chat chat { get; set; }
        public Config config { get; set; }

        public override string ToString()
        {
            return "Userconfig " + chat.ToString();
        }
    }

    public class Chat
    {
        public int id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string username { get; set; }
        public string type { get; set; }

        public override string ToString()
        {
            return string.Format("Chat {0} {1} {2}", type, id, first_name);
        }
    }

    public class Config
    {
        public string[] additionalEvents { get; set; }
        public Change[] changes { get; set; }
        public string[] events { get; set; }

        public bool admin { get; set; }
        public bool showRemovedEvents { get; set; }
        public bool stisysUpdate { get; set; }
    }

    public class Change
    {
        public string name { get; set; }
        public string date { get; set; }
        internal DateTime DateParsed => DateTime.Parse(date);

        public bool remove { get; set; }
        public string room { get; set; }
        public string starttime { get; set; }
        public string endtime { get; set; }
    }
}
