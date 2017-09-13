using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mensa
{
    class Program
    {
        private static readonly Uri BASE_URL = new Uri("http://www.studierendenwerk-hamburg.de/studierendenwerk/de/essen/speiseplaene/");
        private const int WAITTIME_BETWEEN_TWO_DOWNLOADS_IN_MINUTES = 100;

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
            var canteens = await LoadCanteens(BASE_URL);
        }

        private static async Task<Canteen[]> LoadCanteens(Uri baseUrl)
        {
            var source = await baseUrl.GetContent();
            return LoadCanteensFromSource(source);
        }

        private static Canteen[] LoadCanteensFromSource(string baseUrlSource)
        {
            var regex = new Regex(@"<p>- <a href=""([^""]+)""(?: target=""_blank"")?>([^<]+)<");
            var matches = regex.Matches(baseUrlSource).OfType<Match>();

            return matches
                .Select(m => new Canteen(m.Groups[2].Value, new Uri(m.Groups[1].Value)))
                .ToArray();
        }
    }
}
