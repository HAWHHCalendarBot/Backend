using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBackendLib
{
    public static class FilesystemHelper
    {
        public static FileInfo GenerateFileInfo(DirectoryInfo directory, string filename, string extension)
        {
            return GenerateFileInfo(directory, filename + extension);
        }

        public static FileInfo GenerateFileInfo(DirectoryInfo directory, string filenameWithExtension)
        {
            return new FileInfo(directory.FullName + Path.DirectorySeparatorChar + filenameWithExtension);
        }

        public static async Task<ChangedObject<FileInfo>> WriteAllTextAsyncOnlyWhenChanged(FileInfo file, string content)
        {
            file.Refresh();
            if (file.Exists)
            {
                var currentFileContent = await File.ReadAllTextAsync(file.FullName);
                if (currentFileContent == content)
                    return new ChangedObject<FileInfo>(file, ChangeState.Unchanged);
            }

            var changeState = file.Exists ? ChangeState.Changed : ChangeState.Created;
            await File.WriteAllTextAsync(file.FullName, content);
            return new ChangedObject<FileInfo>(file, changeState);
        }

        public static IEnumerable<FileInfo> GetFilesChangedAfterDate(DirectoryInfo directory, DateTime modifiedAfter, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return directory.EnumerateFiles(searchPattern, searchOption)
                .Where(o => o.LastWriteTime > modifiedAfter);
        }
    }
}
