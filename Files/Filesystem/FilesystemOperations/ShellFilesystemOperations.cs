﻿using Files.Common;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Files.Interacts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.Filesystem
{
    public class ShellFilesystemOperations : IFilesystemOperations
    {
        #region Private Members

        private IShellPage associatedInstance;

        private ItemManipulationModel itemManipulationModel => associatedInstance.SlimContentPage?.ItemManipulationModel;

        private FilesystemOperations filesystemOperations;

        private RecycleBinHelpers recycleBinHelpers;

        #endregion Private Members

        #region Constructor

        public ShellFilesystemOperations(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
            filesystemOperations = new FilesystemOperations(associatedInstance);
            recycleBinHelpers = new RecycleBinHelpers(this.associatedInstance);
        }

        #endregion Constructor

        public async Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, NameCollisionOption collision, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await CopyAsync(source.FromStorageItem(),
                                                    destination,
                                                    collision,
                                                    progress,
                                                    errorCode,
                                                    cancellationToken);
        }

        public async Task<IStorageHistory> CopyAsync(IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await CopyItemsAsync(source.CreateEnumerable(), destination.CreateEnumerable(), collision.ConvertBack().CreateEnumerable(), progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> CopyItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, IEnumerable<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await CopyItemsAsync(source.Select((item) => item.FromStorageItem()).ToList(), destination, collisions, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> CopyItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, IEnumerable<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            if (associatedInstance.ServiceConnection == null || source.Any(x => string.IsNullOrWhiteSpace(x.Path)) || destination.Any(x => string.IsNullOrWhiteSpace(x)))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.CopyItemsAsync(source, destination, collisions, progress, errorCode, cancellationToken);
            }

            source = source.Where((src, index) => collisions.ElementAt(index) != FileNameConflictResolveOptionType.Skip);

            EventHandler<Dictionary<string, object>> handler = (s, e) => OnProgressUpdated(s, e, progress);
            associatedInstance.ServiceConnection.RequestReceived += handler;

            var (status, response) = await associatedInstance.ServiceConnection.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "FileOperation" },
                { "fileop", "CopyItem" },
                { "operationID", Guid.NewGuid().ToString() },
                { "filepath", string.Join('|', source.Select(s => s.Path)) },
                { "destpath", string.Join('|', destination) },
                { "overwrite", collisions.All(x => x == FileNameConflictResolveOptionType.ReplaceExisting) }
            });
            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));

            if (associatedInstance.ServiceConnection != null)
            {
                associatedInstance.ServiceConnection.RequestReceived -= handler;
            }

            if (result)
            {
                progress?.Report(100.0f);
                var copiedItems = JsonConvert.DeserializeObject<IEnumerable<string>>(response["CopiedItems"] as string);
                errorCode?.Report(FileSystemStatusCode.Success);
                if (collisions.All(x => x != FileNameConflictResolveOptionType.ReplaceExisting) && copiedItems != null && copiedItems.Count() == source.Count())
                {
                    return new StorageHistory(FileOperationType.Copy, source,
                        copiedItems.Select((item, index) => StorageItemHelpers.FromPathAndType(item, source.ElementAt(index).ItemType)));
                }
                return null; // Cannot undo overwrite operation
            }

            errorCode?.Report(FileSystemStatusCode.Generic);
            progress?.Report(100.0f);
            return null;
        }

        private void OnProgressUpdated(object sender, Dictionary<string, object> message, IProgress<float> progress)
        {
            if (message.ContainsKey("OperationID"))
            {
                var value = (long)message["Progress"];
                progress?.Report(value);
            }
        }

        public async Task<(IStorageHistory, IStorageItem)> CreateAsync(IStorageItemWithPath source, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            // TODO
            return await filesystemOperations.CreateAsync(source, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return await DeleteAsync(source.FromStorageItem(),
                                                      progress,
                                                      errorCode,
                                                      permanently,
                                                      cancellationToken);
        }

        public async Task<IStorageHistory> DeleteAsync(IStorageItemWithPath source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return await DeleteItemsAsync(source.CreateEnumerable(), progress, errorCode, permanently, cancellationToken);
        }

        public async Task<IStorageHistory> DeleteItemsAsync(IEnumerable<IStorageItem> source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return await DeleteItemsAsync(source.Select((item) => item.FromStorageItem()), progress, errorCode, permanently, cancellationToken);
        }

        public async Task<IStorageHistory> DeleteItemsAsync(IEnumerable<IStorageItemWithPath> source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            if (associatedInstance.ServiceConnection == null || source.Any(x => string.IsNullOrWhiteSpace(x.Path)))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.DeleteItemsAsync(source, progress, errorCode, permanently, cancellationToken);
            }

            var deleleFilePaths = source.Select(s => s.Path);

            var deleteFromRecycleBin = source.Any() ? recycleBinHelpers.IsPathUnderRecycleBin(source.ElementAt(0).Path) : false;
            permanently |= deleteFromRecycleBin;

            if (deleteFromRecycleBin)
            {
                // Recycle bin also stores a file starting with $I for each item
                deleleFilePaths = deleleFilePaths.Concat(source.Select(x => Path.Combine(Path.GetDirectoryName(x.Path), Path.GetFileName(x.Path).Replace("$R", "$I"))));
            }

            EventHandler<Dictionary<string, object>> handler = (s, e) => OnProgressUpdated(s, e, progress);
            associatedInstance.ServiceConnection.RequestReceived += handler;

            var (status, response) = await associatedInstance.ServiceConnection.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "FileOperation" },
                { "fileop", "DeleteItem" },
                { "operationID", Guid.NewGuid().ToString() },
                { "filepath", string.Join('|', deleleFilePaths) },
                { "permanently", permanently }
            });
            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));

            if (associatedInstance.ServiceConnection != null)
            {
                associatedInstance.ServiceConnection.RequestReceived -= handler;
            }

            if (result)
            {
                progress?.Report(100.0f);
                var deletedItems = JsonConvert.DeserializeObject<IEnumerable<string>>(response["DeletedItems"] as string);
                var recycledItems = JsonConvert.DeserializeObject<IEnumerable<string>>(response["RecycledItems"] as string);
                errorCode?.Report(FileSystemStatusCode.Success);
                if (deletedItems != null)
                {
                    foreach (var item in deletedItems)
                    {
                        await associatedInstance.FilesystemViewModel.RemoveFileOrFolderAsync(item);
                    }
                }
                if (!permanently && recycledItems != null && recycledItems.Count() == source.Count())
                {
                    return new StorageHistory(FileOperationType.Recycle, source, recycledItems.Select((item, index) => StorageItemHelpers.FromPathAndType(item, source.ElementAt(index).ItemType)));
                }
                return new StorageHistory(FileOperationType.Delete, source, null);
            }

            errorCode?.Report(FileSystemStatusCode.Generic);
            progress?.Report(100.0f);
            return null;
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, NameCollisionOption collision, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await MoveAsync(source.FromStorageItem(),
                                                    destination,
                                                    collision,
                                                    progress,
                                                    errorCode,
                                                    cancellationToken);
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await MoveItemsAsync(source.CreateEnumerable(), destination.CreateEnumerable(), collision.ConvertBack().CreateEnumerable(), progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, IEnumerable<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await MoveItemsAsync(source.Select((item) => item.FromStorageItem()).ToList(), destination, collisions, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> MoveItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, IEnumerable<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            if (associatedInstance.ServiceConnection == null || source.Any(x => string.IsNullOrWhiteSpace(x.Path)) || destination.Any(x => string.IsNullOrWhiteSpace(x)))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.MoveItemsAsync(source, destination, collisions, progress, errorCode, cancellationToken);
            }

            source = source.Where((src, index) => collisions.ElementAt(index) != FileNameConflictResolveOptionType.Skip);

            EventHandler<Dictionary<string, object>> handler = (s, e) => OnProgressUpdated(s, e, progress);
            associatedInstance.ServiceConnection.RequestReceived += handler;

            var (status, response) = await associatedInstance.ServiceConnection.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "FileOperation" },
                { "fileop", "MoveItem" },
                { "operationID", Guid.NewGuid().ToString() },
                { "filepath", string.Join('|', source.Select(s => s.Path)) },
                { "destpath", string.Join('|', destination) },
                { "overwrite", collisions.All(x => x == FileNameConflictResolveOptionType.ReplaceExisting) }
            });
            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));

            if (associatedInstance.ServiceConnection != null)
            {
                associatedInstance.ServiceConnection.RequestReceived -= handler;
            }

            if (result)
            {
                progress?.Report(100.0f);
                var movedItems = JsonConvert.DeserializeObject<IEnumerable<string>>(response["MovedItems"] as string);
                errorCode?.Report(FileSystemStatusCode.Success);
                if (collisions.All(x => x != FileNameConflictResolveOptionType.ReplaceExisting) && movedItems != null && movedItems.Count() == source.Count())
                {
                    return new StorageHistory(FileOperationType.Move, source,
                        movedItems.Select((item, index) => StorageItemHelpers.FromPathAndType(item, source.ElementAt(index).ItemType)));
                }
                return null; // Cannot undo overwrite operation
            }

            errorCode?.Report(FileSystemStatusCode.Generic);
            progress?.Report(100.0f);
            return null;
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await RenameAsync(StorageItemHelpers.FromStorageItem(source), newName, collision, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            if (associatedInstance.ServiceConnection == null || string.IsNullOrWhiteSpace(source.Path))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);
            }

            var (status, response) = await associatedInstance.ServiceConnection.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "FileOperation" },
                { "fileop", "RenameItem" },
                { "operationID", Guid.NewGuid().ToString() },
                { "filepath", source.Path },
                { "newName", newName },
                { "overwrite", collision == NameCollisionOption.ReplaceExisting }
            });
            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));

            if (result)
            {
                var renamedItems = JsonConvert.DeserializeObject<IEnumerable<string>>(response["RenamedItems"] as string);
                errorCode?.Report(FileSystemStatusCode.Success);
                if (collision != NameCollisionOption.ReplaceExisting && renamedItems != null && renamedItems.Count() == 1)
                {
                    return new StorageHistory(FileOperationType.Rename, source,
                        StorageItemHelpers.FromPathAndType(renamedItems.Single(), source.ItemType));
                }
                return null; // Cannot undo overwrite operation
            }

            errorCode?.Report(FileSystemStatusCode.Generic);
            return null;
        }

        public async Task<IStorageHistory> RestoreFromTrashAsync(IStorageItemWithPath source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            if (associatedInstance.ServiceConnection == null || string.IsNullOrWhiteSpace(source.Path) || string.IsNullOrWhiteSpace(destination))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.RestoreFromTrashAsync(source, destination, progress, errorCode, cancellationToken);
            }

            EventHandler<Dictionary<string, object>> handler = (s, e) => OnProgressUpdated(s, e, progress);
            associatedInstance.ServiceConnection.RequestReceived += handler;

            var (status, response) = await associatedInstance.ServiceConnection.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "FileOperation" },
                { "fileop", "MoveItem" },
                { "operationID", Guid.NewGuid().ToString() },
                { "filepath", source.Path },
                { "destpath", destination },
                { "overwrite", false }
            });
            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));

            if (associatedInstance.ServiceConnection != null)
            {
                associatedInstance.ServiceConnection.RequestReceived -= handler;
            }

            if (result)
            {
                progress?.Report(100.0f);
                var movedItems = JsonConvert.DeserializeObject<IEnumerable<string>>(response["MovedItems"] as string);
                errorCode?.Report(FileSystemStatusCode.Success);
                if (movedItems != null && movedItems.Count() == 1)
                {
                    // Recycle bin also stores a file starting with $I for each item
                    await DeleteAsync(StorageItemHelpers.FromPathAndType(
                        Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I")), source.ItemType), 
                        null, null, true, cancellationToken);
                    return new StorageHistory(FileOperationType.Restore, source,
                        StorageItemHelpers.FromPathAndType(movedItems.Single(), source.ItemType));
                }
                return null; // Cannot undo overwrite operation
            }

            errorCode?.Report(FileSystemStatusCode.Generic);
            progress?.Report(100.0f);
            return null;
        }

        #region IDisposable

        public void Dispose()
        {
            filesystemOperations?.Dispose();
            recycleBinHelpers?.Dispose();

            filesystemOperations = null;
            recycleBinHelpers = null;
            associatedInstance = null;
        }

        #endregion IDisposable
    }
}
