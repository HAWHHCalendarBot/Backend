using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Downloader
{
    class Program
    {
        private static readonly Uri[] SOURCE_URIS = new string[] {
            "https://userdoc.informatik.haw-hamburg.de/doku.php?id=stundenplan:ics_public",
            "http://www.etech.haw-hamburg.de/Stundenplan/ICS/"
        }.Select(o => new Uri(o)).ToArray();

        private static readonly Regex ICS_LINK_REGEX = new Regex(@"href=""(\S+\.ics)""");
        private static readonly Encoding HAW_ICS_ENCODING = Encoding.GetEncoding("iso-8859-1");


        private static readonly DirectoryInfo EVENT_DIRECTORY = new DirectoryInfo(Environment.CurrentDirectory).CreateSubdirectory("eventjsons");

        static void Main(string[] args)
        {
            Log("Downloader started.");

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

                Log("finished with this one... now wait.");
                System.Threading.Thread.Sleep(1000 * 60 * 100); // 100 Minuten warten
            }
        }

        private static void Log(string text)
        {
            Console.WriteLine(text);
        }

        private static async Task DoStuff()
        {
            Log("start download at " + DateTime.Now);
            var uris = await GetEventFileUrisFromBaseUriList(SOURCE_URIS);
            uris = uris.ToArray();
            Log("got list of uris: " + uris.Length);

            var content = await uris.Select(o => o.GetContent(HAW_ICS_ENCODING)).WhenAll();
            Log("got ics files: " + content.Length);
            var formattedContent = content.Select(s => s.Replace("\r\n", "\n"));
            var events = formattedContent
                .SelectMany(o => IcsFileContentParser.GetEvents(o))
                .Distinct()
                .ToArray();
            Log("Events: " + events.Length);

            var eventsByName = events.GroupBy(o => o.Name);
            Log("EventsByName: " + eventsByName.Count());

            Log("Save Events to " + EVENT_DIRECTORY.FullName);
            var saveTasks = eventsByName.Select(o => SaveEventFile(o.Key, o));
            await saveTasks.WaitAll();
            Log("Saved");
        }

        private static async Task<Uri[]> GetEventFileUrisFromBaseUriList(IEnumerable<Uri> baseUriList)
        {
            var tasks = baseUriList.Select(GetEventFileUrisFromBaseUri);
            var urisInArray = await tasks.WhenAll();
            var uris = urisInArray
                .SelectMany(o => o)
                .ToArray();
            return uris;
        }

        private static async Task<Uri[]> GetEventFileUrisFromBaseUri(Uri baseUri)
        {
            return GetEventFileUrisOfPageSource(await baseUri.GetContent(), baseUri);
        }

        private static Uri[] GetEventFileUrisOfPageSource(string pageSource, Uri baseUri)
        {
            var matches = ICS_LINK_REGEX.Matches(pageSource).OfType<Match>();
            var uris = matches
                .Select(o => o.Groups[1].Value)
                .Select(o => System.Net.WebUtility.HtmlDecode(o))
                .Select(o => new Uri(baseUri, o));

            return uris.ToArray();
        }

        private static async Task SaveEventFile(string name, IEnumerable<EventEntry> events)
        {
            var eventFile = new FileInfo(EVENT_DIRECTORY.FullName + Path.DirectorySeparatorChar + name + ".json");
            var content = GenerateFileJSONContent(events);

            if (eventFile.Exists)
            {
                var currentContent = await File.ReadAllTextAsync(eventFile.FullName);

                if (currentContent == content)
                    return;
            }

            await File.WriteAllTextAsync(eventFile.FullName, content);
        }

        private static string GenerateFileJSONContent(IEnumerable<EventEntry> events)
        {
            var jsonEvents = events.Select(o => o.GenerateJson());

            var jsonString = "[" + string.Join(",", jsonEvents) + "]";
            return jsonString;
        }
    }
}