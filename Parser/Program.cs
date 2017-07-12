using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Parser
{
    class Program
    {
        private static readonly DirectoryInfo BASE_DIRECTORY = new DirectoryInfo(Environment.CurrentDirectory);
        private static readonly DirectoryInfo CALENDAR_DIRECTORY = BASE_DIRECTORY.CreateSubdirectory("calendars");
        private static readonly DirectoryInfo EVENT_DIRECTORY = BASE_DIRECTORY.CreateSubdirectory("eventjsons");
        private static readonly DirectoryInfo USERCONFIG_DIRECTORY = BASE_DIRECTORY.CreateSubdirectory("userconfig");

        static void Main(string[] args)
        {
            try
            {
                var task = Main();
                task.Wait();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private static async Task Main()
        {
            var lastGeneration = DateTime.Now;
            Log("Generate all Userconfigs...");
            await GenerateAllUserconfigs();
            Log("All generated");

            Log("Start main loop...");
            while (true)
            {
                try
                {
                    var preScan = DateTime.Now;
                    var changedUserconfigFiles = FilesystemHelper.GetFilesChangedAfterDate(USERCONFIG_DIRECTORY, lastGeneration);
                    var changedUserconfigIds = changedUserconfigFiles.Select(o => o.Name.Replace(".json", "")).Select(o => Convert.ToInt32(o));
                    var changedEventFiles = FilesystemHelper.GetFilesChangedAfterDate(EVENT_DIRECTORY, lastGeneration);

                    if (changedUserconfigFiles.Any())
                        Log(changedUserconfigFiles.ToArrayString("Userconfig Files changed"));

                    if (changedEventFiles.Any())
                        Log(changedEventFiles.ToArrayString("Event Files changed"));

                    if (changedUserconfigFiles.Any() || changedEventFiles.Any())
                    {
                        var allUserconfigs = await GetAllUserconfigs();

                        var directChangedUserconfigs = allUserconfigs.Where(o => changedUserconfigIds.Contains(o.chat.id));
                        var indirectChangedUserconfigs = changedEventFiles.SelectMany(o => GetUserconfigsThatNeedEventFile(allUserconfigs, o));

                        var allChangedUserconfigs = directChangedUserconfigs.Concat(indirectChangedUserconfigs).Distinct();
                        await GenerateSetOfUserconfigs(allChangedUserconfigs);
                        lastGeneration = preScan;
                    }

                    DeleteNotAnymoreNeededCalendars();
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                }

                System.Threading.Thread.Sleep(1000 * 5); // 5 Seconds
            }
        }

        private static void Log(string text)
        {
            Console.WriteLine(text);
        }

        private static Userconfig[] GetUserconfigsThatNeedEventFile(Userconfig[] userconfigs, FileInfo eventFile)
        {
            return userconfigs
                .Where(o => o.config.events
                    .Select(EventEntry.GetFilename)
                    .Contains(eventFile.Name)
                )
                .ToArray();
        }

        private static async Task GenerateAllUserconfigs()
        {
            var userconfigs = await GetAllUserconfigs();
            await GenerateSetOfUserconfigs(userconfigs);
        }

        private static void DeleteNotAnymoreNeededCalendars()
        {
            var generatedCalendars = USERCONFIG_DIRECTORY.EnumerateFiles()
                .Select(o => o.Name.Replace(".json", ".ics"))
                .ToArray();

            var calendarsToDelete = CALENDAR_DIRECTORY.EnumerateFiles()
                .Where(o => !generatedCalendars.Contains(o.Name))
                .ToArray();

            if (calendarsToDelete.Length == 0)
                return;

            Log(calendarsToDelete.ToArrayString("delete"));
            foreach (var item in calendarsToDelete)
            {
                item.Delete();
            }
            Log("Delete finished.");
        }

        private static async Task GenerateSetOfUserconfigs(IEnumerable<Userconfig> userconfigs)
        {
            Log("Generate " + userconfigs.Count() + " userconfigs...");

            var changeStates = await userconfigs
                .Select(GenerateCalendar)
                .WhenAll();

            if (changeStates.CountCreated() > 0)
                Log(changeStates.OnlyCreated().ToArrayString("Created"));
            if (changeStates.CountChanged() > 0)
                Log(changeStates.OnlyChanged().ToArrayString("Changed"));
            if (changeStates.CountUnchanged() > 0)
                Log(changeStates.OnlyUnchanged().ToArrayString("Unchanged"));
        }

        #region GenerateCalendar

        private static async Task<ChangedObject> GenerateCalendar(Userconfig config)
        {
            return await GenerateCalendar(config.chat.first_name, config.chat.id.ToString(), config.config.events);
        }

        private static async Task<ChangedObject> GenerateCalendar(string name, string filename, params string[] eventnames)
        {
            var events = await LoadEventsByName(eventnames);
            var icsContent = IcsGenerator.GenerateIcsContent(name, events);
            var file = FilesystemHelper.GenerateFileInfo(CALENDAR_DIRECTORY, filename, ".ics");

            var result = await FilesystemHelper.WriteAllTextAsyncOnlyWhenChanged(file, icsContent);
            return new ChangedObject(name, result.ChangeState);
        }

        #endregion

        #region From Filesystem

        private static async Task<Userconfig[]> GetAllUserconfigs()
        {
            var userconfigs = await USERCONFIG_DIRECTORY.EnumerateFiles("*.json")
                .Select(o => JsonHelper.ConvertFromJsonAsync<Userconfig>(o))
                .WhenAll();

            return userconfigs;
        }

        private static async Task<EventEntry[]> LoadEventsByName(params string[] eventnames)
        {
            var filenamesOfEvents = eventnames.Select(EventEntry.GetFilename).ToArray();

            var eventsArrays = await EVENT_DIRECTORY.EnumerateFiles("*.json")
                .Where(o => filenamesOfEvents.Contains(o.Name))
                .Select(o => JsonHelper.ConvertFromJsonAsync<EventEntry[]>(o))
                .WhenAll();

            var events = eventsArrays
                .SelectMany(o => o)
                .ToArray();

            return events;
        }

        #endregion
    }
}
