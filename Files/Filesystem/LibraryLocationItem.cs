﻿using Files.Common;
using Files.Helpers;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.Filesystem
{
    public class LibraryLocationItem : LocationItem
    {
        public LibraryLocationItem(ShellLibraryItem shellLibrary)
        {
            Section = SectionType.Library;
            Text = shellLibrary.DisplayName;
            Path = shellLibrary.FullPath;
            Icon = GlyphHelper.GetIconUri(shellLibrary.DefaultSaveFolder);
            DefaultSaveFolder = shellLibrary.DefaultSaveFolder;
            Folders = shellLibrary.Folders == null ? null : new ReadOnlyCollection<string>(shellLibrary.Folders);
            IsDefaultLocation = shellLibrary.IsPinned;
        }

        public string DefaultSaveFolder { get; }

        public ReadOnlyCollection<string> Folders { get; }

        public bool IsEmpty => DefaultSaveFolder == null || Folders == null || Folders.Count == 0;

        public async Task<bool> CheckDefaultSaveFolderAccess()
        {
            if (IsEmpty)
            {
                return false;
            }
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(DefaultSaveFolder));
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(DefaultSaveFolder, item));
            return res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(DefaultSaveFolder);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return obj is LibraryLocationItem other && string.Equals(Path, other.Path, System.StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() => Path.GetHashCode();
    }
}