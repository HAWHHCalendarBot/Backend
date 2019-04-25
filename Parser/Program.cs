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
        private static readonly DirectoryInfo EVENT_DIRECTORY = BASE_DIRECTORY.CreateSubdirectory("eventfiles");
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
                    var changedUserconfigFiles = FilesystemHelper.GetFilesChangedAfterDate(USERCONFIG_DIRECTORY, lastGeneration, "*.json");
                    var changedUserconfigIds = changedUserconfigFiles.Select(o => o.Name.Replace(".json", "")).Select(o => Convert.ToInt32(o));
                    var changedEventFiles = FilesystemHelper.GetFilesChangedAfterDate(EVENT_DIRECTORY, lastGeneration, "*.json");

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

                    await DeleteNotAnymoreNeededCalendars();
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

        private static FileInfo GetCalendarFileOfUser(Userconfig config)
        {
            var filename = config.chat.id.ToString();
            if (!string.IsNullOrWhiteSpace(config.config.calendarfileSuffix))
            {
                filename += "-" + config.config.calendarfileSuffix;
            }
            var file = FilesystemHelper.GenerateFileInfo(CALENDAR_DIRECTORY, filename, ".ics");
            return file;
        }

        private static Userconfig[] GetUserconfigsThatNeedEventFile(Userconfig[] userconfigs, FileInfo eventFile)
        {
            return userconfigs
                .Where(o => o.config.events.Concat(o.config.additionalEvents ?? new string[0])
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

        private static async Task DeleteNotAnymoreNeededCalendars()
        {
            var userconfigs = await GetAllUserconfigs();
            var neededFiles = userconfigs
                .Select(o => GetCalendarFileOfUser(o))
                .Select(o => o.Name)
                .ToArray();

            var unneededFiles = CALENDAR_DIRECTORY.EnumerateFiles()
                .Where(o => !neededFiles.Contains(o.Name))
                .ToArray();

            if (unneededFiles.Length == 0)
                return;

            Log(unneededFiles.ToArrayString("delete"));
            foreach (var item in unneededFiles)
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
            var events = config.config.events.Concat(config.config.additionalEvents ?? new string[0]).Distinct().ToArray();
            var file = GetCalendarFileOfUser(config);
            return await GenerateCalendar(config.chat.first_name, file, events, config.config.changes, config.config.removedEvents);
        }

        private static async Task<ChangedObject> GenerateCalendar(string name, FileInfo file, string[] eventnames, IEnumerable<Change> changes, string removedEvents)
        {
            var allEvents = await LoadEventsByName(eventnames);
            var events = ApplyChanges(allEvents, changes, removedEvents);

            var icsContent = IcsGenerator.GenerateIcsContent(name, events);

            var result = await FilesystemHelper.WriteAllTextAsyncOnlyWhenChanged(file, icsContent);
            return new ChangedObject(name, result.ChangeState);
        }

        private static EventEntry[] ApplyChanges(IEnumerable<EventEntry> events, IEnumerable<Change> changes, string removedEvents)
        {
            if (changes == null)
            {
                return events
                    .ToArray();
            }

            var modifyChanges = changes
                .Where(o => !o.add)
                .ToArray();

            var addChanges = changes
                .Where(o => o.add)
                .ToArray();

            var additionalEvents = addChanges
                .Select(o => AddChange(o))
                .ToArray();

            var result = events
               .Select(e =>
                {
                    var changeOfEvent = modifyChanges.SingleOrDefault(o => o.name == e.Name && o.DateParsed == e.StartTime);
                    return ApplyChange(e, changeOfEvent, removedEvents);
                })
                .Concat(additionalEvents)
                .Where(o => o != null)
                .ToArray();

            return result;
        }

        private static EventEntry AddChange(Change change)
        {
            var name = change.name;
            var startTime = change.DateParsed;
            var endTime = change.DateParsed.Date.Add(TimeSpan.Parse(change.endtime));

            var eventEntry = new EventEntry(name, startTime, endTime);
            eventEntry.Description += "Diese Veranstaltung wurde nachträglich von dir hinzugefügt. Nutze 'Veranstaltungsänderungen' im Bot um dies anzupassen.\nhttps://t.me/HAWHHCalendarBot";
            eventEntry.Location = change.room ?? string.Empty;

            eventEntry.PrettyName = "➕ " + name;

            if (!string.IsNullOrWhiteSpace(change.namesuffix))
            {
                eventEntry.PrettyName += " " + change.namesuffix;
            }
            return eventEntry;
        }

        private static EventEntry ApplyChange(EventEntry original, Change change, string removedEvents)
        {
            if (change == null) return original;

            var changedEvent = (EventEntry)original.Clone();

            if (!string.IsNullOrWhiteSpace(original.Description))
                changedEvent.Description += "\n\n";
            changedEvent.Description += "Diese Veranstaltung wurde nachträglich von dir verändert. Nutze 'Veranstaltungsänderungen' im Bot um dies anzupassen.\nhttps://t.me/HAWHHCalendarBot";

            if (change.remove)
            {
                switch (removedEvents)
                {
                    case "emoji":
                        changedEvent.PrettyName = "🚫 " + original.Name;
                        break;
                    case "remove":
                        return null;
                    case "cancelled":
                    default:
                        changedEvent.Status = EventStatus.Cancelled;
                        break;
                }
                return changedEvent;
            }

            changedEvent.PrettyName = "✏️ " + original.Name;

            if (!string.IsNullOrWhiteSpace(change.namesuffix))
            {
                changedEvent.PrettyName += " " + change.namesuffix;
            }
            if (!string.IsNullOrWhiteSpace(change.room))
            {
                changedEvent.Location = change.room;
            }

            if (!string.IsNullOrWhiteSpace(change.starttime))
            {
                changedEvent.StartTime = original.StartTime.Date.Add(TimeSpan.Parse(change.starttime));
            }
            if (!string.IsNullOrWhiteSpace(change.endtime))
            {
                changedEvent.EndTime = original.EndTime.Date.Add(TimeSpan.Parse(change.endtime));
            }

            return changedEvent;
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
