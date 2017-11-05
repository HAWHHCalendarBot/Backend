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

        private static readonly Uri INFORMATIK_TXT_URI = new Uri("https://www.haw-hamburg.de/fileadmin/user_upload/TI-I/Studium/Veranstaltungsplaene/Sem_I.txt");

        private static readonly DirectoryInfo BASE_DIRECTORY = new DirectoryInfo(Environment.CurrentDirectory);
        private static readonly DirectoryInfo ADDITIONALS_DIRECTORY = BASE_DIRECTORY.CreateSubdirectory("additionalEvents");

        private static readonly Regex ICS_LINK_REGEX = new Regex(@"href=""(\S+\.ics)""");
        private static readonly Encoding HAW_FILE_ENCODING = Encoding.GetEncoding("iso-8859-1");
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

            var icsEvents = await GetIcsEvents(SOURCE_URIS);
            Log("ICS Events: " + icsEvents.Length);

            // var informatikTxtEvents = await GetInformatikTxtEvents(INFORMATIK_TXT_URI);
            // Log("informatik.txt Events: " + informatikTxtEvents.Length);

            var additionals = await GetAdditionals(ADDITIONALS_DIRECTORY);

            var events = icsEvents
                // .Concat(informatikTxtEvents)
                .Concat(additionals)
                .Distinct()
                .OrderBy(o => o.Name)
                .ThenBy(o => o.StartTime)
                .ToArray();
            Log("Total Events: " + events.Length);

            var eventsByName = events.GroupBy(o => o.Name);
            Log("EventsByName: " + eventsByName.Count());

            Log("Save Events to " + EVENT_DIRECTORY.FullName);
            await SaveAllEventsCsv(events);

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

        private static async Task<EventEntry[]> GetIcsEvents(IEnumerable<Uri> baseUriList)
        {
            var uris = await GetEventFileUrisFromBaseUriList(SOURCE_URIS);
            uris = uris.ToArray();
            Log("got list of ics uris: " + uris.Length);

            var fileContent = await uris.Select(o => o.GetContent(HAW_FILE_ENCODING)).WhenAll();
            Log("got ics files: " + fileContent.Length);
            var formattedContent = fileContent.Select(s => s.Replace("\r\n", "\n"));
            var events = formattedContent
                .SelectMany(o => IcsFileContentParser.GetEvents(o))
                .Distinct()
                .ToArray();

            return events;
        }

        private static async Task<EventEntry[]> GetInformatikTxtEvents(Uri informatikTxtUri)
        {
            var content = await informatikTxtUri.GetContent(HAW_FILE_ENCODING);
            return InformatikTxtContentParser.GetEvents(content);
        }

        private static async Task<EventEntry[]> GetAdditionals(DirectoryInfo directoryOfAdditionals)
        {
            var addiTasks = await directoryOfAdditionals.EnumerateFiles()
                .Select(o => JsonHelper.ConvertFromJsonAsync<AdditionalEvent[]>(o))
                .WhenAll();

            var addis = addiTasks
                .SelectMany(o => o)
                .Select(o => o.GetEventEntry())
                .ToArray();

            return addis;
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

        private static async Task SaveAllEventsCsv(IEnumerable<EventEntry> events)
        {
            var file = FilesystemHelper.GenerateFileInfo(EVENT_DIRECTORY, "all", ".csv");
            var lines = events.Select(o => string.Join(';', new string[] { o.Name, o.Location, o.StartTime.ToString(), o.EndTime.ToString(), o.Description })).ToArray();
            var headline = string.Join(';', new string[] { "Name", "Location", "StartTime", "EndTime", "Description" });
            var content = headline + '\n' + string.Join('\n', lines);

            await File.WriteAllTextAsync(file.FullName, content);
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