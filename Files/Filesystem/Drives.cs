using ByteSizeLib;
using Files.Common;
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
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class DrivesManager : ObservableObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public SettingsViewModel AppSettings => App.AppSettings;
        private List<DriveItem> _Drives = new List<DriveItem>();
        public IReadOnlyList<DriveItem> Drives => _Drives.AsReadOnly();
        private bool _ShowUserConsentOnInit = false;

        public bool ShowUserConsentOnInit
        {
            get => _ShowUserConsentOnInit;
            set => SetProperty(ref _ShowUserConsentOnInit, value);
        }

        private DeviceWatcher _deviceWatcher;
        private bool _driveEnumInProgress;

        public DrivesManager()
        {
            EnumerateDrives();
        }

        private async void EnumerateDrives()
        {
            _driveEnumInProgress = true;
            if (await GetDrivesAsync(_Drives))
            {
                if (!_Drives.Any(d => d.Type != DriveType.Removable))
                {
                    // Only show consent dialog if the exception is UnauthorizedAccessException
                    // and the drives list is empty (except for Removable drives which don't require FileSystem access)
                    ShowUserConsentOnInit = true;
                }
            }
            StartDeviceWatcher();
            await GetVirtualDrivesListAsync();
            _driveEnumInProgress = false;
        }

        private void StartDeviceWatcher()
        {
            _deviceWatcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
            _deviceWatcher.Added += DeviceAdded;
            _deviceWatcher.Removed += DeviceRemoved;
            _deviceWatcher.Updated += DeviceUpdated;
            _deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            _deviceWatcher.Start();
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    if (MainPage.sideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "SidebarDrives".GetLocalized()) == null)
                    {
                        MainPage.sideBarItems.Add(new HeaderTextItem() { Text = "SidebarDrives".GetLocalized() });
                    }
                    foreach (DriveItem drive in _Drives)
                    {
                        if (!MainPage.sideBarItems.Contains(drive))
                        {
                            MainPage.sideBarItems.Add(drive);
                            DrivesWidget.itemsAdded.Add(drive);
                        }
                    }
                    foreach (INavigationControlItem item in MainPage.sideBarItems.ToList())
                    {
                        if (item is DriveItem && !_Drives.Contains(item))
                        {
                            MainPage.sideBarItems.Remove(item);
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
                if (MainPage.sideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "SidebarDrives".GetLocalized()) == null)
                {
                    MainPage.sideBarItems.Add(new HeaderTextItem() { Text = "SidebarDrives".GetLocalized() });
                }
                foreach (DriveItem drive in _Drives)
                {
                    if (!MainPage.sideBarItems.Contains(drive))
                    {
                        MainPage.sideBarItems.Add(drive);
                        DrivesWidget.itemsAdded.Add(drive);
                    }
                }
                foreach (INavigationControlItem item in MainPage.sideBarItems.ToList())
                {
                    if (item is DriveItem && !_Drives.Contains(item))
                    {
                        MainPage.sideBarItems.Remove(item);
                        DrivesWidget.itemsAdded.Remove(item);
                    }
                }
            });
            CoreApplication.MainView.Activated -= MainView_Activated;
        }

        private void DeviceAdded(DeviceWatcher sender, DeviceInformation args)
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
            if (_Drives.Any(x => string.IsNullOrEmpty(root.Path) ? x.Path.Contains(root.Name) : x.Path == root.Path))
            {
                return;
            }

            var driveItem = new DriveItem(root, DriveType.Removable);

            Logger.Info($"Drive added: {driveItem.Path}, {driveItem.Type}");

            // Update the collection on the ui-thread.
            _Drives.Add(driveItem);
            DeviceWatcher_EnumerationCompleted(null, null);
        }

        private void DeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var drives = DriveInfo.GetDrives().Select(x => x.Name);

            foreach (var drive in _Drives)
            {
                if (drive.Type == DriveType.VirtualDrive || drives.Contains(drive.Path))
                {
                    continue;
                }

                Logger.Info($"Drive removed: {drive.Path}");

                // Update the collection on the ui-thread.
                _Drives.Remove(drive);
                DeviceWatcher_EnumerationCompleted(null, null);
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
                    Logger.Warn($"{res.ErrorCode.ToString()}: Attemting to add the device, {drive.Name}, failed at the StorageFolder initialization step. This device will be ignored.");
                    continue;
                }
                else if (!res)
                {
                    Logger.Warn($"{res.ErrorCode.ToString()}: Attemting to add the device, {drive.Name}, failed at the StorageFolder initialization step. This device will be ignored.");
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

        public async Task GetVirtualDrivesListAsync()
        {
            var connection = await InitializeAppServiceConnection();
            if (connection != null)
            {
                var response = await connection.SendMessageAsync(new ValueSet() {
                        { "Arguments", "CloudProviders" } });
                if (response.Status == AppServiceResponseStatus.Success && response.Message.ContainsKey("DetectedCloudProviders"))
                {
                    var providers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CloudProvider>>((string)response.Message["DetectedCloudProviders"]);
                    foreach (var provider in providers)
                    {
                        var cloudProviderItem = new DriveItem()
                        {
                            Text = provider.Name,
                            Path = provider.SyncFolder,
                            Type = Filesystem.DriveType.VirtualDrive,
                        };
                        _Drives.Add(cloudProviderItem);
                    }
                }
                connection.Dispose();
                DeviceWatcher_EnumerationCompleted(null, null);
            }
        }

        public async Task<AppServiceConnection> InitializeAppServiceConnection()
        {
            var ServiceConnection = new AppServiceConnection();
            ServiceConnection.AppServiceName = "FilesInteropService";
            ServiceConnection.PackageFamilyName = Package.Current.Id.FamilyName;

            AppServiceConnectionStatus status = await ServiceConnection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                // TODO: error handling
                ServiceConnection?.Dispose();
                return null;
            }
            ServiceConnection.RequestReceived += (s, e) => { };

            // Launch fulltrust process
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            await Task.Delay(TimeSpan.FromSeconds(2));
            return ServiceConnection;
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
            if (_deviceWatcher != null)
            {
                if (_deviceWatcher.Status == DeviceWatcherStatus.Started || _deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    _deviceWatcher.Stop();
                }
            }
        }

        public void ResumeDeviceWatcher()
        {
            if (!_driveEnumInProgress)
            {
                this.Dispose();
                this.StartDeviceWatcher();
            }
        }
    }
}