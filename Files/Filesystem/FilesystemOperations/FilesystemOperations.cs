﻿using Files.Common;
using Files.Extensions;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.Filesystem
{
    public enum ImpossibleActionResponseTypes
    {
        Skip,
        Abort
    }

    public class FilesystemOperations : IFilesystemOperations
    {
        #region Private Members

        private IShellPage associatedInstance;

        private RecycleBinHelpers recycleBinHelpers;

        #endregion

        #region Constructor

        public FilesystemOperations(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
            this.recycleBinHelpers = new RecycleBinHelpers(this.associatedInstance);
        }

        #endregion

        #region IFilesystemOperations

        public async Task<IStorageHistory> CreateAsync(PathWithType source, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            try
            {
                switch (source.ItemType)
                {
                    case FilesystemItemType.File:
                        {
                            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(source.Path));
                            await folder.CreateFileAsync(Path.GetFileName(source.Path));

                            break;
                        }

                    case FilesystemItemType.Directory:
                        {
                            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(source.Path));
                            await folder.CreateFolderAsync(Path.GetFileName(source.Path));

                            break;
                        }

                    case FilesystemItemType.Symlink:
                        {
                            Debugger.Break();
                            throw new NotImplementedException();
                        }

                    default:
                        Debugger.Break();
                        break;
                }

                errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS);
                return new StorageHistory(FileOperationType.CreateNew, source.CreateEnumerable(), null);
            }
            catch (Exception e)
            {
                errorCode?.Report(FilesystemTasks.GetErrorCode(e));
                return null;
            }
        }

        public async Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            return await CopyAsync(new PathWithType(source.Path, source.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory), destination, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> CopyAsync(PathWithType source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
            {
                errorCode?.Report(FilesystemErrorCode.ERROR_UNAUTHORIZED);
                progress?.Report(100.0f);

                // Do not paste files and folders inside the recycle bin
                await DialogDisplayHelper.ShowDialogAsync("ErrorDialogThisActionCannotBeDone".GetLocalized(), "ErrorDialogUnsupportedOperation".GetLocalized());
                return null;
            }

            IStorageItem copiedItem = null;
            long itemSize = await FilesystemHelpers.GetItemSize(await source.Path.ToStorageItem());
            bool reportProgress = false; // TODO: The default value is false

            if (source.ItemType == FilesystemItemType.Directory)
            {
                if (string.IsNullOrWhiteSpace(source.Path) || Path.GetDirectoryName(destination).IsSubPathOf(source.Path)) // We check if user tried to copy anything above the source.ItemPath 
                {
                    ImpossibleActionResponseTypes responseType = ImpossibleActionResponseTypes.Abort;

                    /*if (ShowDialog)
                    {
                        /// Currently following implementation throws exception until it is resolved keep it disabled
                        Binding themeBind = new Binding();
                        themeBind.Source = ThemeHelper.RootTheme;
                        ContentDialog dialog = new ContentDialog()
                        {
                            Title = ResourceController.GetTranslation("ErrorDialogThisActionCannotBeDone"),
                            Content = ResourceController.GetTranslation("ErrorDialogTheDestinationFolder") + " (" + destinationPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last() + ") " + ResourceController.GetTranslation("ErrorDialogIsASubfolder") + " (" + item.Name + ")",
                            PrimaryButtonText = ResourceController.GetTranslation("ErrorDialogSkip"),
                            CloseButtonText = ResourceController.GetTranslation("ErrorDialogCancel"),
                            PrimaryButtonCommand = new RelayCommand(() => { responseType = ImpossibleActionResponseTypes.Skip; }),
                            CloseButtonCommand = new RelayCommand(() => { responseType = ImpossibleActionResponseTypes.Abort; }),
                        };
                        BindingOperations.SetBinding(dialog, FrameworkElement.RequestedThemeProperty, themeBind);
                        await dialog.ShowAsync();
                    }*/
                    if (responseType == ImpossibleActionResponseTypes.Skip)
                    {
                        progress?.Report(100.0f);
                        errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS | FilesystemErrorCode.ERROR_INPROGRESS);
                    }
                    else if (responseType == ImpossibleActionResponseTypes.Abort)
                    {
                        progress?.Report(100.0f);
                        errorCode?.Report(FilesystemErrorCode.ERROR_INPROGRESS | FilesystemErrorCode.ERROR_GENERIC);
                    }

                    return null;
                }
                else
                {
                    if (reportProgress)
                    {
                        progress?.Report((float)(itemSize * 100.0f / itemSize));
                    }

                    await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(destination))
                        .OnSuccess(t => FilesystemHelpers.CloneDirectoryAsync((IStorageFolder)source.Path.ToStorageItem(), t, Path.GetFileName(source.Path)))
                        .OnSuccess(t =>
                        {
                            copiedItem = t;
                        });
                }
            }
            else if (source.ItemType == FilesystemItemType.File)
            {
                if (reportProgress)
                {
                    progress?.Report((float)(itemSize * 100.0f / itemSize));
                }

                FilesystemResult<StorageFolder> fsResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(destination));

                if (fsResult)
                {
                    StorageFile file = (StorageFile)await source.Path.ToStorageItem();

                    FilesystemResult<StorageFile> fsResultCopy = new FilesystemResult<StorageFile>(null, FilesystemErrorCode.ERROR_GENERIC);

                    if (file != null)
                        fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(fsResult.Result, Path.GetFileName(source.Path), NameCollisionOption.GenerateUniqueName).AsTask());

                    if (fsResultCopy)
                    {
                        copiedItem = fsResultCopy.Result;
                    }
                    else if (fsResultCopy.ErrorCode == FilesystemErrorCode.ERROR_UNAUTHORIZED || fsResultCopy.ErrorCode == FilesystemErrorCode.ERROR_GENERIC)
                    {
                        // Try again with CopyFileFromApp
                        if (NativeFileOperationsHelper.CopyFileFromApp(source.Path, destination, true))
                        {
                            copiedItem = await source.Path.ToStorageItem(); // Dangerous - the provided item may be different than output result!
                        }
                        else
                        {
                            Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                        }
                    }
                    else
                    {
                        errorCode?.Report(fsResultCopy.ErrorCode);
                    }
                }
            }

            if (Path.GetDirectoryName(destination) == associatedInstance.FilesystemViewModel.WorkingDirectory)
            {
                if (copiedItem != null)
                {
                    List<ListedItem> copiedListedItems = associatedInstance.FilesystemViewModel.FilesAndFolders.Where(listedItem => copiedItem.Path.Contains(listedItem.ItemPath)).ToList();

                    if (copiedListedItems.Count > 0)
                    {
                        associatedInstance.ContentPage.AddSelectedItemsOnUi(copiedListedItems);
                        associatedInstance.ContentPage.FocusSelectedItems();
                    }
                }
            }

            progress?.Report(100.0f);

            return new StorageHistory(FileOperationType.Copy, source, new PathWithType((copiedItem != null ? (!string.IsNullOrWhiteSpace(copiedItem.Path) ? copiedItem.Path : destination) : destination), source.ItemType));
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            return await MoveAsync(new PathWithType(source.Path, source.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory), destination, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> MoveAsync(PathWithType source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            IStorageHistory history = await CopyAsync(source, destination, progress, errorCode, cancellationToken);

            FilesystemResult fsResultDelete = (FilesystemResult)false;
            if (string.IsNullOrWhiteSpace(source.Path))
            {
                // Can't move (only copy) files from MTP devices because:
                // StorageItems returned in DataPackageView are read-only
                // The item.Path property will be empty and there's no way of retrieving a new StorageItem with R/W access
                errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS | FilesystemErrorCode.ERROR_INPROGRESS);
            }

            await DeleteAsync(source, progress, errorCode, true, cancellationToken);

            progress?.Report(100.0f);

            return new StorageHistory(FileOperationType.Move, history.Source, history.Destination);
        }

        public async Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return await DeleteAsync(new PathWithType(source.Path, source.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory), progress, errorCode, permanently, cancellationToken);
        }

        public async Task<IStorageHistory> DeleteAsync(PathWithType source, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            bool deleteFromRecycleBin = await recycleBinHelpers.IsRecycleBinItem(source.Path);

            FilesystemResult fsResult = FilesystemErrorCode.ERROR_INPROGRESS;

            errorCode?.Report(fsResult);
            progress?.Report(0.0f);

            if (source.ItemType == FilesystemItemType.File)
            {
                fsResult = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }
            else if (source.ItemType == FilesystemItemType.Directory)
            {
                fsResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }

            errorCode?.Report(fsResult);

            if (fsResult == FilesystemErrorCode.ERROR_UNAUTHORIZED)
            {
                if (!permanently)
                {
                    // Try again with fulltrust process
                    if (associatedInstance.FilesystemViewModel.Connection != null)
                    {
                        AppServiceResponse response = await associatedInstance.FilesystemViewModel.Connection.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "MoveToBin" },
                            { "filepath", source.Path }
                        });
                        fsResult = (FilesystemResult)(response.Status == AppServiceResponseStatus.Success);
                    }
                }
                else
                {
                    // Try again with DeleteFileFromApp
                    if (!NativeFileOperationsHelper.DeleteFileFromApp(source.Path))
                    {
                        Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        fsResult = (FilesystemResult)true;
                    }
                }
            }
            else if (fsResult == FilesystemErrorCode.ERROR_INUSE)
            {
                // TODO: retry or show dialog
                await DialogDisplayHelper.ShowDialogAsync("FileInUseDeleteDialog/Title".GetLocalized(), "FileInUseDeleteDialog/Text".GetLocalized());
            }

            if (deleteFromRecycleBin)
            {
                // Recycle bin also stores a file starting with $I for each item
                string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I"));
                await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
                    .OnSuccess(t => t.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
            }
            errorCode?.Report(fsResult);
            progress?.Report(100.0f);

            if (fsResult)
            {
                await associatedInstance.FilesystemViewModel.RemoveFileOrFolderAsync(source.Path);

                if (!permanently)
                {
                    // Enumerate Recycle Bin
                    List<ShellFileItem> items = await this.recycleBinHelpers.EnumerateRecycleBin();
                    List<ShellFileItem> nameMatchItems;

                    // Get name matching files
                    if (Path.GetExtension(source.Path) == ".lnk" || Path.GetExtension(source.Path) == ".url") // We need to check if it is a shortcut file
                    {
                        nameMatchItems = items.Where((item) => item.FilePath == Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileNameWithoutExtension(source.Path))).ToList();
                    }
                    else
                    {
                        nameMatchItems = items.Where((item) => item.FilePath == source.Path).ToList();
                    }

                    // Get newest file
                    ShellFileItem item = nameMatchItems.Where((item) => item.RecycleDate != null).OrderBy((item) => item.RecycleDate).FirstOrDefault();

                    return new StorageHistory(FileOperationType.Recycle, source, new PathWithType(item.RecyclePath, source.ItemType));
                }

                return new StorageHistory(FileOperationType.Delete, source, null);
            }
            else
            {
                // Stop at first error
                return null;
            }
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            if (Path.GetFileName(source.Path) == newName && collision == NameCollisionOption.FailIfExists)
            {
                errorCode?.Report(FilesystemErrorCode.ERROR_ALREADYEXIST);
                return null;
            }

            if (!string.IsNullOrWhiteSpace(newName)
                && !FilesystemHelpers.ContainsRestrictedCharacters(newName)
                && !FilesystemHelpers.ContainsRestrictedFileName(newName))
            {
                try
                {
                    FilesystemItemType itemType = source.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory;
                    string originalSource = source.Path;
                    await source.RenameAsync(newName, collision);

                    errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS);
                    return new StorageHistory(FileOperationType.Rename, new PathWithType(originalSource, itemType), new PathWithType(source.Path, itemType));
                }
                catch (Exception e)
                {
                    errorCode?.Report(FilesystemTasks.GetErrorCode(e));
                    return null;
                }
            }

            return null;
        }

        public async Task<IStorageHistory> RenameAsync(PathWithType source, string newName, NameCollisionOption collision, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            if (Path.GetFileName(source.Path) == newName && collision == NameCollisionOption.FailIfExists)
            {
                errorCode?.Report(FilesystemErrorCode.ERROR_ALREADYEXIST);
                return null;
            }

            if (!string.IsNullOrWhiteSpace(newName)
                && !FilesystemHelpers.ContainsRestrictedCharacters(newName)
                && !FilesystemHelpers.ContainsRestrictedFileName(newName))
            {
                try
                {
                    IStorageItem itemToRename = await source.Path.ToStorageItem();
                    await itemToRename.RenameAsync(newName, collision);

                    errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS);
                    return new StorageHistory(FileOperationType.Rename, source, new PathWithType(itemToRename.Path, source.ItemType));
                }
                catch (Exception e)
                {
                    errorCode?.Report(FilesystemTasks.GetErrorCode(e));
                    return null;
                }
            }

            return null;
        }

        public async Task<IStorageHistory> RestoreFromTrashAsync(PathWithType source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            FilesystemResult fsResult = FilesystemErrorCode.ERROR_INPROGRESS;
            errorCode?.Report(fsResult);

            if (source.ItemType == FilesystemItemType.Directory)
            {
                FilesystemResult<StorageFolder> sourceFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path);
                fsResult = sourceFolder.ErrorCode;
                errorCode?.Report(fsResult);

                if (sourceFolder)
                {
                    fsResult = await FilesystemTasks.Wrap(async () => FilesystemHelpers.MoveDirectoryAsync(sourceFolder.Result, (StorageFolder)await Path.GetDirectoryName(destination).ToStorageItem(), Path.GetFileName(destination)))
                                .OnSuccess(t => sourceFolder.Result.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
                }
                errorCode?.Report(fsResult);
            }
            else
            {
                FilesystemResult<StorageFile> sourceFile = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path);
                fsResult = sourceFile.ErrorCode;
                errorCode?.Report(fsResult);

                if (sourceFile)
                {
                    fsResult = await FilesystemTasks.Wrap(async () => sourceFile.Result.MoveAsync((IStorageFolder)(await Path.GetDirectoryName(destination).ToStorageItem()), Path.GetFileName(destination), NameCollisionOption.GenerateUniqueName).AsTask());
                }
                errorCode?.Report(fsResult);
            }

            // Recycle bin also stores a file starting with $I for each item
            string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I"));
            await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
                .OnSuccess(iFile => iFile.DeleteAsync().AsTask());

            errorCode?.Report(fsResult);
            if (fsResult != FilesystemErrorCode.ERROR_SUCCESS)
            {
                if (((FilesystemErrorCode)fsResult).HasFlag(FilesystemErrorCode.ERROR_UNAUTHORIZED))
                {
                    await DialogDisplayHelper.ShowDialogAsync("AccessDeniedDeleteDialog/Title".GetLocalized(), "AccessDeniedDeleteDialog/Text".GetLocalized());
                }
                else if (((FilesystemErrorCode)fsResult).HasFlag(FilesystemErrorCode.ERROR_UNAUTHORIZED))
                {
                    await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
                }
                else if (((FilesystemErrorCode)fsResult).HasFlag(FilesystemErrorCode.ERROR_ALREADYEXIST))
                {
                    await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalized(), "ItemAlreadyExistsDialogContent".GetLocalized());
                }
            }

            //if (Path.GetFileName(source.ItemOriginalPath) != Path.GetFileName(destination)) // Different names
            //    return new StorageHistory(FileOperationType.Restore, source.ItemPath, Path.Combine(destination, Path.GetFileName(source.ItemOriginalPath)));
            //else
            return new StorageHistory(FileOperationType.Restore, source, new PathWithType(destination, source.ItemType));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            this.associatedInstance?.Dispose();
            this.recycleBinHelpers?.Dispose();

            this.recycleBinHelpers = null;
            this.associatedInstance = null;
        }

        #endregion
    }
}
