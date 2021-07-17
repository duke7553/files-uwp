using Files.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Helpers.FileListCache
{
    internal class FileListCacheController : IFileListCache
    {
        private static FileListCacheController instance;

        public static FileListCacheController GetInstance()
        {
            return instance ??= new FileListCacheController();
        }

        private readonly IFileListCache persistentAdapter;

        private FileListCacheController()
        {
            persistentAdapter = new PersistentSQLiteCacheAdapter();
        }

        private readonly Dictionary<string, object> fileNamesCache = new Dictionary<string, object>();

        public async Task<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken)
        {
            var displayName = fileNamesCache.Get(path, (string)null);
            if (displayName == null)
            {
                displayName = await persistentAdapter.ReadFileDisplayNameFromCache(path, cancellationToken);
                if (displayName != null)
                {
                    fileNamesCache[path] = displayName;
                }
            }
            return displayName;
        }

        public Task SaveFileDisplayNameToCache(string path, string displayName)
        {
            if (displayName == null)
            {
                fileNamesCache.Remove(path);
                return persistentAdapter.SaveFileDisplayNameToCache(path, displayName);
            }
            fileNamesCache[path] = displayName;

            // save entry to persistent cache in background
            return persistentAdapter.SaveFileDisplayNameToCache(path, displayName);
        }
    }
}