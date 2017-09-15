using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mensa
{
    class Program
    {
        private static readonly Uri BASE_URL = new Uri("http://www.studierendenwerk-hamburg.de/studierendenwerk/de/essen/speiseplaene/");
        private const int WAITTIME_BETWEEN_TWO_DOWNLOADS_IN_MINUTES = 100;

        private static readonly DirectoryInfo MEAL_DIRECTORY = new DirectoryInfo(Environment.CurrentDirectory).CreateSubdirectory("meals");

        static void Main(string[] args)
        {
            Log("Mensa Downloader started.");

            while (true)
            {
                try
                {
                    Log("");
                    var task = DoStuff();

                    task.Wait();
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                }

                Log("finished with this one... now wait " + WAITTIME_BETWEEN_TWO_DOWNLOADS_IN_MINUTES + " minutes.");
                System.Threading.Thread.Sleep(1000 * 60 * WAITTIME_BETWEEN_TWO_DOWNLOADS_IN_MINUTES);
            }
        }

        private static void Log(string text)
        {
            Console.WriteLine(text);
        }

        private static async Task DoStuff()
        {
            Log("Load canteens...");
            var canteens = await LoadCanteens(BASE_URL);
            Log("Got " + canteens.Count() + " canteens");

            var weekOfYear = CalendarHelper.WeekOfDate(DateTime.Now);
            var weeksToLoad = new int[] { weekOfYear, weekOfYear + 1 };
            
            Log("Save Meals...");
            await canteens
               .SelectMany(canteen => weeksToLoad.Select(week => SaveMealsOfCanteen(canteen, DateTime.Now.Year, week)))
               .WaitAll();
        }

        private static async Task<Canteen[]> LoadCanteens(Uri baseUrl)
        {
            var source = await baseUrl.GetContent();
            return LoadCanteensFromSource(source);
        }

        private static Canteen[] LoadCanteensFromSource(string baseUrlSource)
        {
            var regex = new Regex(@"<p>- <a href=""[^""]+\/(\d+)""(?: target=""_blank"")?>([^<]+)<");
            var matches = regex.Matches(baseUrlSource).OfType<Match>();

            return matches
                .Select(m => new Canteen(int.Parse(m.Groups[1].Value), m.Groups[2].Value))
                .ToArray();
        }

        private static async Task<Meal[]> LoadMealsOfCanteen(Canteen canteen, int year, int week)
        {
            var source = await canteen.GetWeekUri(year, week).GetContent();
            return CanteenWeekViewParser.GetMeals(year, week, source);
        }

        private static async Task SaveMealsOfCanteen(Canteen canteen, int year, int week)
        {
            var folder = MEAL_DIRECTORY.CreateSubdirectory(canteen.Name);
            var meals = await LoadMealsOfCanteen(canteen, year, week);

            var byDate = meals.GroupBy(o => o.Date);
            await byDate
                .Select(o => SaveMealsOfCanteenDay(canteen, o.Key, folder, o))
                .WaitAll();
        }

        private static async Task SaveMealsOfCanteenDay(Canteen canteen, DateTime day, DirectoryInfo folder, IEnumerable<Meal> meals)
        {
            var json = JsonHelper.ConvertToJson(meals);
            var filename = day.ToString("yyyyMMdd");
            var file = FilesystemHelper.GenerateFileInfo(folder, filename, ".json");

            await File.WriteAllTextAsync(file.FullName, json);
        }
    }
}
