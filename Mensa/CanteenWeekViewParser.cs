using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Mensa
{
    internal class CanteenWeekViewParser
    {
        private static readonly Regex CATEGORY_COLUMN_REGEX = new Regex(@"<th class=.category.>");
        private static readonly Regex MEAL_SWITCH_REGEX = new Regex(@"<\/p>");
        private static readonly Regex DAY_SWITCH_REGEX = new Regex(@"<\/td>");
        private static readonly Regex NAME_REGEX = new Regex(@"<strong>(.+)<\/strong>");
        private static readonly Regex ADDITIVE_REPLACE_REGEX = new Regex(@"<span class=tooltip title=([^>]+)>(\d+)<\/span>");
        private static readonly Regex PRICE_REGEX = new Regex(@"([\d,]+).€ \/ ([\d,]+).€");
        private static readonly Regex BONUS_REGEX = new Regex(@"<img .+ title=.([^""]+).+\/>");

        public static Meal[] GetMeals(int year, int week, string source)
        {
            var meals = new List<Meal>();

            var lines = source
                .Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .ToArray();

            var currentWeekday = DayOfWeek.Monday;
            var name = "";
            var prices = new double[2];
            var boniTexts = new List<string>();
            var additives = new Dictionary<int, string>();

            foreach (var line in lines)
            {
                if (CATEGORY_COLUMN_REGEX.IsMatch(line))
                {
                    currentWeekday = DayOfWeek.Monday;
                }
                else if (MEAL_SWITCH_REGEX.IsMatch(line))
                {
                    if (string.IsNullOrWhiteSpace(name) || prices[0] == 0 || prices[1] == 0)
                        continue;

                    var day = CalendarHelper.DateOfWeekDayISO8601(year, week, currentWeekday);
                    var meal = new Meal(name, day, prices[0], prices[1]);

                    meal.Additives = additives;

                    meal.Pig = boniTexts.Any(o => o == "mit Schwein");
                    meal.Beef = boniTexts.Any(o => o == "mit Rind");
                    meal.Fish = boniTexts.Any(o => o == "mit Fisch");
                    meal.Poultry = boniTexts.Any(o => o == "mit Geflügel");

                    meal.LactoseFree = boniTexts.Any(o => o == "laktosefrei");
                    meal.Vegetarian = boniTexts.Any(o => o == "vegetarisch");
                    meal.Vegan = boniTexts.Any(o => o == "Vegan");

                    meals.Add(meal);

                    name = "";
                    prices = new double[2];
                    boniTexts = new List<string>();
                    additives = new Dictionary<int, string>();
                }
                else if (DAY_SWITCH_REGEX.IsMatch(line))
                {
                    currentWeekday++;
                    name = "";
                    prices = new double[2];
                    boniTexts = new List<string>();
                    additives = new Dictionary<int, string>();
                }
                else if (NAME_REGEX.IsMatch(line))
                {
                    var nameHtml = NAME_REGEX.Match(line).Groups[1].Value;
                    additives = ADDITIVE_REPLACE_REGEX.Matches(nameHtml)
                        .OfType<Match>()
                        .Select(o => new KeyValuePair<int, string>(int.Parse(o.Groups[2].Value), o.Groups[1].Value))
                        .Distinct()
                        .ToDictionary();

                    name = ADDITIVE_REPLACE_REGEX.Replace(nameHtml, "$2")
                        .Replace("<strong>", "")
                        .Replace("</strong>", "")
                        .Replace("  ", " ")
                        .Replace(") ,", "),")
                        .Trim();
                }
                else if (PRICE_REGEX.IsMatch(line))
                {
                    var match = PRICE_REGEX.Match(line);
                    var numberFormat = CultureInfo.GetCultureInfo("de-DE").NumberFormat;
                    var price1 = double.Parse(match.Groups[1].Value, numberFormat);
                    var price2 = double.Parse(match.Groups[2].Value, numberFormat);
                    prices = new double[] { price1, price2 };
                }
                else if (BONUS_REGEX.IsMatch(line))
                {
                    var bonus = BONUS_REGEX.Match(line).Groups[1].Value;
                    boniTexts.Add(bonus);
                }

                else
                {

                }
            }

            return meals.ToArray();
        }
    }
}
