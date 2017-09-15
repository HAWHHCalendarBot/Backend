using System;
using System.Collections.Generic;
using System.Text;

namespace Mensa
{
    internal class Canteen
    {
        private const string WEEK_BASE_URL = "http://speiseplan.studierendenwerk-hamburg.de/de/";

        public readonly int ID;
        public readonly string Name;

        public Canteen(int id, string name)
        {
            ID = id;
            Name = name;
        }

        public Uri GetWeekUri(int year, int week)
        {
            return new Uri(WEEK_BASE_URL + ID + "/" + year + "/" + week + "/");
        }
    }
}
