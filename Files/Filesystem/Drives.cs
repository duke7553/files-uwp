using Files.View_Models;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class DrivesManager : ObservableObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public SettingsViewModel AppSettings => App.AppSettings;
        public IList<DriveItem> Drives { get; } = new List<DriveItem>();
        private bool showUserConsentOnInit = false;

        public bool ShowUserConsentOnInit
        {
            get => showUserConsentOnInit;
            set => SetProperty(ref showUserConsentOnInit, value);
        }

        private DeviceWatcher deviceWatcher;
        private bool driveEnumInProgress;

        public DrivesManager()
        {
            EnumerateDrives();
        }

        private async void EnumerateDrives()
        {
            driveEnumInProgress = true;
            if (await GetDrivesAsync(Drives))
            {
                if (!Drives.Any(d => d.Type != DriveType.Removable))
                {
                    // Only show consent dialog if the exception is UnauthorizedAccessException
                    // and the drives list is empty (except for Removable drives which don't require FileSystem access)
                    ShowUserConsentOnInit = true;
                }
            }
            GetVirtualDrivesList(Drives);
            StartDeviceWatcher();
            driveEnumInProgress = false;
        }

        private void StartDeviceWatcher()
        {
            deviceWatcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
            deviceWatcher.Added += DeviceAdded;
            deviceWatcher.Removed += DeviceRemoved;
            deviceWatcher.Updated += DeviceUpdated;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Start();
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    if (MainPage.SideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "SidebarDrives".GetLocalized()) == null)
                    {
                        MainPage.SideBarItems.Add(new HeaderTextItem()
                        {
                            Text = "SidebarDrives".GetLocalized()
                        });
                    }
                    foreach (DriveItem drive in Drives)
                    {
                        if (!MainPage.SideBarItems.Contains(drive))
                        {
                            MainPage.SideBarItems.Add(drive);
                            DrivesWidget.itemsAdded.Add(drive);
                        }
                    }
                    foreach (INavigationControlItem item in MainPage.SideBarItems.ToList())
                    {
                        if (item is DriveItem && !Drives.Contains(item))
                        {
                            MainPage.SideBarItems.Remove(item);
                            DrivesWidget.itemsAdded.Remove(item);
                        }
                    }
                });
            }
            catch (Exception)       // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += MainView_Activated;
            }
        }

        private async void MainView_Activated(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (MainPage.SideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "SidebarDrives".GetLocalized()) == null)
                {
                    MainPage.SideBarItems.Add(new HeaderTextItem()
                    {
                        Text = "SidebarDrives".GetLocalized()
                    });
                }
                foreach (DriveItem drive in Drives)
                {
                    if (!MainPage.SideBarItems.Contains(drive))
                    {
                        MainPage.SideBarItems.Add(drive);
                        DrivesWidget.itemsAdded.Add(drive);
                    }
                }
                foreach (INavigationControlItem item in MainPage.SideBarItems.ToList())
                {
                    if (item is DriveItem && !Drives.Contains(item))
                    {
                        MainPage.SideBarItems.Remove(item);
                        DrivesWidget.itemsAdded.Remove(item);
                    }
                }
            });
            CoreApplication.MainView.Activated -= MainView_Activated;
        }

        private async void DeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            var deviceId = args.Id;
            StorageFolder root = null;
            try
            {
                root = StorageDevice.FromId(deviceId);
            }
            catch (Exception ex) when (
                ex is UnauthorizedAccessException
                || ex is ArgumentException)
            {
                Logger.Warn($"{ex.GetType()}: Attemting to add the device, {args.Name}, failed at the StorageFolder initialization step. This device will be ignored. Device ID: {args.Id}");
                return;
            }

            // If drive already in list, skip.
            if (Drives.Any(x => string.IsNullOrEmpty(root.Path) ? x.Path.Contains(root.Name) : x.Path == root.Path))
            {
                return;
            }

            var driveItem = new DriveItem(root, DriveType.Removable);

            Logger.Info($"Drive added: {driveItem.Path}, {driveItem.Type}");

            // Update the collection on the ui-thread.
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    Drives.Add(driveItem);
                    DeviceWatcher_EnumerationCompleted(null, null);
                });
            }
            catch (Exception)
            {
                // Ui-Thread not yet created.
                Drives.Add(driveItem);
            }
        }

        private async void DeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var drives = DriveInfo.GetDrives().Select(x => x.Name);

            foreach (var drive in Drives)
            {
                if (drive.Type == DriveType.VirtualDrive || drives.Contains(drive.Path))
                {
                    continue;
                }

                Logger.Info($"Drive removed: {drive.Path}");

                // Update the collection on the ui-thread.
                try
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        Drives.Remove(drive);
                        DeviceWatcher_EnumerationCompleted(null, null);
                    });
                }
                catch (Exception)
                {
                    // Ui-Thread not yet created.
                    Drives.Remove(drive);
                }
                return;
            }
        }

        private void DeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine("Devices updated");
        }

        private async Task<bool> GetDrivesAsync(IList<DriveItem> list)
        {
            // Flag set if any drive throws UnauthorizedAccessException
            bool unauthorizedAccessDetected = false;

            var drives = DriveInfo.GetDrives().ToList();

            var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
            List<string> supportedDevicesNames = new List<string>();
            foreach (var item in remDevices)
            {
                try
                {
                    supportedDevicesNames.Add(StorageDevice.FromId(item.Id).Name);
                }
                catch (Exception e)
                {
                    Logger.Warn("Can't get storage device name: " + e.Message + ", skipping...");
                }
            }

            foreach (DriveInfo driveInfo in drives.ToList())
            {
                if (!supportedDevicesNames.Contains(driveInfo.Name) && driveInfo.DriveType == System.IO.DriveType.Removable)
                {
                    drives.Remove(driveInfo);
                }
            }

            foreach (var drive in drives)
            {
                // If drive already in list, skip.
                if (list.Any(x => x.Path == drive.Name))
                {
                    continue;
                }

                var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
                if (res == FilesystemErrorCode.ERROR_UNAUTHORIZED)
                {
                    unauthorizedAccessDetected = true;
                    Logger.Warn($"{res.ErrorCode}: Attemting to add the device, {drive.Name}, failed at the StorageFolder initialization step. This device will be ignored.");
                    continue;
                }
                else if (!res)
                {
                    Logger.Warn($"{res.ErrorCode}: Attemting to add the device, {drive.Name}, failed at the StorageFolder initialization step. This device will be ignored.");
                    continue;
                }

                DriveType type = DriveType.Unknown;

                switch (drive.DriveType)
                {
                    case System.IO.DriveType.CDRom:
                        type = DriveType.CDRom;
                        break;

                    case System.IO.DriveType.Fixed:
                        if (Helpers.PathNormalization.NormalizePath(drive.Name) != Helpers.PathNormalization.NormalizePath("A:")
                            && Helpers.PathNormalization.NormalizePath(drive.Name) !=
                            Helpers.PathNormalization.NormalizePath("B:"))
                        {
                            type = DriveType.Fixed;
                        }
                        else
                        {
                            type = DriveType.FloppyDisk;
                        }
                        break;

                    case System.IO.DriveType.Network:
                        type = DriveType.Network;
                        break;

                    case System.IO.DriveType.NoRootDirectory:
                        type = DriveType.NoRootDirectory;
                        break;

                    case System.IO.DriveType.Ram:
                        type = DriveType.Ram;
                        break;

                    case System.IO.DriveType.Removable:
                        type = DriveType.Removable;
                        break;

                    case System.IO.DriveType.Unknown:
                        type = DriveType.Unknown;
                        break;

                    default:
                        type = DriveType.Unknown;
                        break;
                }

                var driveItem = new DriveItem(res.Result, type);

                Logger.Info($"Drive added: {driveItem.Path}, {driveItem.Type}");

                list.Add(driveItem);
            }

            return unauthorizedAccessDetected;
        }

        private void GetVirtualDrivesList(IList<DriveItem> list)
        {
            var setting = ApplicationData.Current.LocalSettings.Values["PinOneDrive"];
            if (setting == null || (bool)setting == true)
            {
                if (AppSettings.OneDrivePath != null)
                {
                    var oneDriveItem = new DriveItem()
                    {
                        Text = "OneDrive",
                        Path = AppSettings.OneDrivePath,
                        Type = DriveType.VirtualDrive,
                    };
                    list.Add(oneDriveItem);
                }

                if (AppSettings.OneDriveCommercialPath != null)
                {
                    var oneDriveItem = new DriveItem()
                    {
                        Text = "OneDrive Commercial",
                        Path = AppSettings.OneDriveCommercialPath,
                        Type = Filesystem.DriveType.VirtualDrive,
                    };
                    list.Add(oneDriveItem);
                }
            }
        }

        public static async Task<StorageFolderWithPath> GetRootFromPathAsync(string devicePath)
        {
            if (!Path.IsPathRooted(devicePath))
            {
                return null;
            }
            var rootPath = Path.GetPathRoot(devicePath);
            if (devicePath.StartsWith("\\\\?\\")) // USB device
            {
                // Check among already discovered drives
                StorageFolder matchingDrive = App.AppSettings.DrivesManager.Drives.FirstOrDefault(x =>
                    Helpers.PathNormalization.NormalizePath(x.Path) == Helpers.PathNormalization.NormalizePath(rootPath))?.Root;
                if (matchingDrive == null)
                {
                    // Check on all removable drives
                    var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
                    foreach (var item in remDevices)
                    {
                        try
                        {
                            var root = StorageDevice.FromId(item.Id);
                            if (Helpers.PathNormalization.NormalizePath(rootPath).Replace("\\\\?\\", "") == root.Name.ToUpperInvariant())
                            {
                                matchingDrive = root;
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore this..
                        }
                    }
                }
                if (matchingDrive != null)
                {
                    return new StorageFolderWithPath(matchingDrive, rootPath);
                }
            }
            else if (devicePath.StartsWith("\\\\")) // Network share
            {
                rootPath = rootPath.LastIndexOf("\\") > 1 ? rootPath.Substring(0, rootPath.LastIndexOf("\\")) : rootPath; // Remove share name
                return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(rootPath), rootPath);
            }
            // It's ok to return null here, on normal drives StorageFolder.GetFolderFromPathAsync works
            return null;
        }

        public void Dispose()
        {
            if (deviceWatcher != null)
            {
                if (deviceWatcher.Status == DeviceWatcherStatus.Started || deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    deviceWatcher.Stop();
                }
            }
        }

        public void ResumeDeviceWatcher()
        {
            if (!driveEnumInProgress)
            {
                this.Dispose();
                this.StartDeviceWatcher();
            }
        }
    }
}