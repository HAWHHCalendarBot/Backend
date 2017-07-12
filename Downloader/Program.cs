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
        private const int WAITTIME_BETWEEN_TWO_DOWNLOADS_IN_MINUTES = 100;


        private static readonly DirectoryInfo EVENT_DIRECTORY = new DirectoryInfo(Environment.CurrentDirectory).CreateSubdirectory("eventfiles");

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
            var changedFiles = await eventsByName.Select(o => SaveEventFile(o)).WhenAll();
            if (changedFiles.CountCreated() > 0)
                Log(changedFiles.OnlyCreated().ToArrayString("Created"));
            if (changedFiles.CountChanged() > 0)
                Log(changedFiles.OnlyChanged().ToArrayString("Changed"));
            Log("Unchanged " + changedFiles.CountUnchanged());

            Log("Delete not anymore existing event files...");
            var fileNamesOfEvents = eventsByName
                .Select(o => o.Key)
                .Select(EventEntry.GetFilename)
                .ToArray();
            var unneeded = EVENT_DIRECTORY.EnumerateFiles("*.json")
                .Where(o => !fileNamesOfEvents.Contains(o.Name))
                .ToArray();
            if (unneeded.Any())
            {
                Log(unneeded.ToArrayString("Delete"));

                foreach (var item in unneeded)
                {
                    item.Delete();
                }

                Log("Deleted");
            }
            else
            {
                Log("Nothing to delete");
            }

            Log("Save Event list file...");
            var eventList = string.Join("\n", eventsByName.Select(o => o.Key));
            var eventFileList = FilesystemHelper.GenerateFileInfo(EVENT_DIRECTORY, "all.txt");
            await FilesystemHelper.WriteAllTextAsyncOnlyWhenChanged(eventFileList, eventList);
            Log("Saved Event list");
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

        private static async Task<ChangedObject> SaveEventFile(IEnumerable<EventEntry> events)
        {
            var filename = events.First().Filename;
            var eventFile = FilesystemHelper.GenerateFileInfo(EVENT_DIRECTORY, filename);
            var content = GenerateFileJSONContent(events);

            var result = await FilesystemHelper.WriteAllTextAsyncOnlyWhenChanged(eventFile, content);
            return new ChangedObject(filename, result.ChangeState);
        }

        private static string GenerateFileJSONContent(IEnumerable<EventEntry> events)
        {
            return JsonHelper.ConvertToJson(events);
        }
    }
}