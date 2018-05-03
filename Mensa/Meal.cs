using System;
using System.Collections.Generic;
using System.Text;

namespace Mensa
{
    public class Meal
    {
        public string Name;
        public DateTime Date;
        public double PriceStudent;
        public double PriceAttendant;
        public double PriceGuest;
        public Dictionary<string, string> Additives;

        public bool Pig;
        public bool Beef;
        public bool Fish;
        public bool Poultry;

        public bool LactoseFree;
        public bool Vegetarian;
        public bool Vegan;

        [Obsolete("Use parameterized Constructor")]
        public Meal()
        { }

        public Meal(string name, DateTime date, double priceStudent, double priceAttendant, double priceGuest)
        {
            Name = name;
            Date = date;
            PriceStudent = priceStudent;
            PriceAttendant = priceAttendant;
            PriceGuest = priceGuest;

            Additives = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            return Date.ToShortDateString() + " " + PriceStudent + " " + Name;
        }
    }
}
