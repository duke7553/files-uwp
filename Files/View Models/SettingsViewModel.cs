﻿using Files.Common;
using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.View_Models
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ApplicationDataContainer _roamingSettings;
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public DrivesManager DrivesManager { get; }

        public SettingsViewModel()
        {
            _roamingSettings = ApplicationData.Current.RoamingSettings;

            DetectOneDrivePreference();
            DetectAcrylicPreference();
            DetectDateTimeFormat();
            PinSidebarLocationItems();
            DetectRecycleBinPreference();
            DetectQuickLook();
            DetectGridViewSize();

            DrivesManager = new DrivesManager();

            //DetectWSLDistros();
            LoadTerminalApps();

            // Send analytics
            Analytics.TrackEvent("DisplayedTimeStyle " + DisplayedTimeStyle.ToString());
            Analytics.TrackEvent("ThemeValue " + ThemeHelper.RootTheme.ToString());
            Analytics.TrackEvent("PinOneDriveToSideBar " + PinOneDriveToSideBar.ToString());
            Analytics.TrackEvent("PinRecycleBinToSideBar " + PinRecycleBinToSideBar.ToString());
            Analytics.TrackEvent("DoubleTapToRenameFiles " + DoubleTapToRenameFiles.ToString());
            Analytics.TrackEvent("ShowFileExtensions " + ShowFileExtensions.ToString());
            Analytics.TrackEvent("ShowConfirmDeleteDialog " + ShowConfirmDeleteDialog.ToString());
            Analytics.TrackEvent("AcrylicSidebar " + AcrylicEnabled.ToString());
        }

        public async void DetectQuickLook()
        {
            // Detect QuickLook
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "StartupTasks";
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        private void PinSidebarLocationItems()
        {
            AddDefaultLocations();
            PopulatePinnedSidebarItems();
        }

        private void AddDefaultLocations()
        {
            App.sideBarItems.Add(new LocationItem { Text = ResourceController.GetTranslation("SidebarHome"), Glyph = "\uE737", IsDefaultLocation = true, Path = "Home" });
        }

        public List<string> LinesToRemoveFromFile = new List<string>();

        private async void PopulatePinnedSidebarItems()
        {
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;

            StorageFile pinnedItemsFile;
            try
            {
                pinnedItemsFile = await cacheFolder.GetFileAsync(SidebarPinnedModel.JsonFileName);
            }
            catch (FileNotFoundException)
            {
                try
                {
                    var oldPinnedItemsFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
                    var oldPinnedItems = await FileIO.ReadLinesAsync(oldPinnedItemsFile);
                    await oldPinnedItemsFile.DeleteAsync();
                    foreach (var line in oldPinnedItems)
                    {
                        if (!App.SidebarPinned.Items.Contains(line))
                        {
                            App.SidebarPinned.Items.Add(line);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    App.SidebarPinned.AddDefaultItems();
                }

                pinnedItemsFile = await cacheFolder.CreateFileAsync(SidebarPinnedModel.JsonFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(pinnedItemsFile, JsonConvert.SerializeObject(App.SidebarPinned, Formatting.Indented));
            }

            try
            {
                App.SidebarPinned = JsonConvert.DeserializeObject<SidebarPinnedModel>(await FileIO.ReadTextAsync(pinnedItemsFile));
            }
            catch (Exception)
            {
                await pinnedItemsFile.DeleteAsync();
                App.SidebarPinned.AddDefaultItems();
                App.SidebarPinned.Save();
            }

            App.SidebarPinned.AddAllItemsToSidebar();
        }

        private async void DetectWSLDistros()
        {
            try
            {
                var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
                if ((await distroFolder.GetFoldersAsync()).Count > 0)
                {
                    AreLinuxFilesSupported = false;
                }

                foreach (StorageFolder folder in await distroFolder.GetFoldersAsync())
                {
                    Uri logoURI = null;
                    if (folder.DisplayName.Contains("ubuntu", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/ubuntupng.png");
                    }
                    else if (folder.DisplayName.Contains("kali", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/kalipng.png");
                    }
                    else if (folder.DisplayName.Contains("debian", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/debianpng.png");
                    }
                    else if (folder.DisplayName.Contains("opensuse", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/opensusepng.png");
                    }
                    else if (folder.DisplayName.Contains("alpine", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/alpinepng.png");
                    }
                    else
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/genericpng.png");
                    }

                    App.sideBarItems.Add(new WSLDistroItem() { Text = folder.DisplayName, Path = folder.Path, Logo = logoURI });
                }
            }
            catch (Exception)
            {
                // WSL Not Supported/Enabled
                AreLinuxFilesSupported = false;
            }
        }

        private void DetectDateTimeFormat()
        {
            if (localSettings.Values[LocalSettings.DateTimeFormat] != null)
            {
                if (localSettings.Values[LocalSettings.DateTimeFormat].ToString() == "Application")
                {
                    DisplayedTimeStyle = TimeStyle.Application;
                }
                else if (localSettings.Values[LocalSettings.DateTimeFormat].ToString() == "System")
                {
                    DisplayedTimeStyle = TimeStyle.System;
                }
            }
            else
            {
                localSettings.Values[LocalSettings.DateTimeFormat] = "Application";
            }
        }

        private TimeStyle _DisplayedTimeStyle = TimeStyle.Application;

        public TimeStyle DisplayedTimeStyle
        {
            get => _DisplayedTimeStyle;
            set
            {
                Set(ref _DisplayedTimeStyle, value);
                if (value.Equals(TimeStyle.Application))
                {
                    localSettings.Values[LocalSettings.DateTimeFormat] = "Application";
                }
                else if (value.Equals(TimeStyle.System))
                {
                    localSettings.Values[LocalSettings.DateTimeFormat] = "System";
                }
            }
        }

        public TerminalFileModel TerminalsModel { get; set; }

        public StorageFile TerminalsModelFile;

        private async void LoadTerminalApps()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var localSettingsFolder = await localFolder.CreateFolderAsync("settings", CreationCollisionOption.OpenIfExists);
            try
            {
                TerminalsModelFile = await localSettingsFolder.GetFileAsync("terminal.json");
            }
            catch (FileNotFoundException)
            {
                var defaultFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/terminal/terminal.json"));

                TerminalsModelFile = await localSettingsFolder.CreateFileAsync("terminal.json");
                await FileIO.WriteBufferAsync(TerminalsModelFile, await FileIO.ReadBufferAsync(await defaultFile));
            }

            var content = await FileIO.ReadTextAsync(TerminalsModelFile);

            try
            {
                TerminalsModel = JsonConvert.DeserializeObject<TerminalFileModel>(content);
            }
            catch (JsonSerializationException)
            {
                var defaultFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/terminal/terminal.json"));

                TerminalsModelFile = await localSettingsFolder.CreateFileAsync("terminal.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBufferAsync(TerminalsModelFile, await FileIO.ReadBufferAsync(await defaultFile));
                var defaultContent = await FileIO.ReadTextAsync(TerminalsModelFile);
                TerminalsModel = JsonConvert.DeserializeObject<TerminalFileModel>(defaultContent);
            }

            var windowsTerminal = new TerminalModel()
            {
                Name = "Windows Terminal",
                Path = "wt.exe",
                Arguments = "-d .",
                Icon = ""
            };

            var fluentTerminal = new TerminalModel()
            {
                Name = "Fluent Terminal",
                Path = "flute.exe",
                Arguments = "",
                Icon = ""
            };

            bool isWindowsTerminalAddedOrRemoved = await TerminalsModel.AddOrRemoveTerminal(windowsTerminal, "Microsoft.WindowsTerminal_8wekyb3d8bbwe");
            bool isFluentTerminalAddedOrRemoved = await TerminalsModel.AddOrRemoveTerminal(fluentTerminal, "53621FSApps.FluentTerminal_87x1pks76srcp");
            if (isWindowsTerminalAddedOrRemoved || isFluentTerminalAddedOrRemoved)
            {
                await FileIO.WriteTextAsync(TerminalsModelFile, JsonConvert.SerializeObject(TerminalsModel, Formatting.Indented));
            }
        }

        private FormFactorMode _FormFactor = FormFactorMode.Regular;

        public FormFactorMode FormFactor
        {
            get => _FormFactor;
            set => Set(ref _FormFactor, value);
        }

        public string OneDrivePath = Environment.GetEnvironmentVariable("OneDrive");

        private async void DetectOneDrivePreference()
        {
            if (localSettings.Values["PinOneDrive"] == null) { localSettings.Values["PinOneDrive"] = true; }

            if ((bool)localSettings.Values["PinOneDrive"] == true)
            {
                PinOneDriveToSideBar = true;
            }
            else
            {
                PinOneDriveToSideBar = false;
            }

            try
            {
                await StorageFolder.GetFolderFromPathAsync(OneDrivePath);
            }
            catch (Exception)
            {
                PinOneDriveToSideBar = false;
            }
        }
        public bool OpenPropertiesInMultipleWindows
        {
            get => Get(false);
            set => Set(value);
        }
        public bool ShowFileOwner
        {
            get => Get(false);
            set => Set(value);
        }
        private bool _PinOneDriveToSideBar = true;

        public bool PinOneDriveToSideBar
        {
            get => _PinOneDriveToSideBar;
            set
            {
                if (value != _PinOneDriveToSideBar)
                {
                    Set(ref _PinOneDriveToSideBar, value);
                    if (value == true)
                    {
                        localSettings.Values["PinOneDrive"] = true;
                        var oneDriveItem = new DriveItem()
                        {
                            Text = "OneDrive",
                            Path = OneDrivePath,
                            Type = Filesystem.DriveType.VirtualDrive,
                        };
                        App.sideBarItems.Add(oneDriveItem);
                    }
                    else
                    {
                        localSettings.Values["PinOneDrive"] = false;
                        foreach (INavigationControlItem item in App.sideBarItems.ToList())
                        {
                            if (item is DriveItem && item.ItemType == NavigationControlItemType.OneDrive)
                            {
                                App.sideBarItems.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        // Any distinguishable path here is fine
        // Currently is the command to open the folder from cmd ("cmd /c start Shell:RecycleBinFolder")
        public string RecycleBinPath = @"Shell:RecycleBinFolder";

        private void DetectRecycleBinPreference()
        {
            if (localSettings.Values["PinRecycleBin"] == null) { localSettings.Values["PinRecycleBin"] = false; }

            if ((bool)localSettings.Values["PinRecycleBin"] == true)
            {
                PinRecycleBinToSideBar = true;
            }
            else
            {
                PinRecycleBinToSideBar = false;
            }
        }

        private bool _PinRecycleBinToSideBar = false;

        public bool PinRecycleBinToSideBar
        {
            get => _PinRecycleBinToSideBar;
            set
            {
                if (value != _PinRecycleBinToSideBar)
                {
                    Set(ref _PinRecycleBinToSideBar, value);
                    if (value == true)
                    {
                        localSettings.Values["PinRecycleBin"] = true;
                        var recycleBinItem = new LocationItem
                        {
                            Text = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                            Font = Application.Current.Resources["RecycleBinIcons"] as FontFamily,
                            Glyph = "\uEF87",
                            IsDefaultLocation = true,
                            Path = RecycleBinPath
                        };
                        // Add recycle bin to sidebar, title is read from LocalSettings (provided by the fulltrust process)
                        // TODO: the very first time the app is launched localized name not available
                        App.sideBarItems.Insert(App.sideBarItems.Where(item => item is LocationItem).Count(), recycleBinItem);
                    }
                    else
                    {
                        localSettings.Values["PinRecycleBin"] = false;
                        foreach (INavigationControlItem item in App.sideBarItems.ToList())
                        {
                            if (item is LocationItem && item.Path == RecycleBinPath)
                            {
                                App.sideBarItems.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        public string DesktopPath = UserDataPaths.GetDefault().Desktop;

        public string DocumentsPath = UserDataPaths.GetDefault().Documents;

        public string DownloadsPath = UserDataPaths.GetDefault().Downloads;

        public string PicturesPath = UserDataPaths.GetDefault().Pictures;

        public string MusicPath = UserDataPaths.GetDefault().Music;

        public string VideosPath = UserDataPaths.GetDefault().Videos;

        private string _TempPath = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Environment", "TEMP", null);

        public string TempPath
        {
            get => _TempPath;
            set => Set(ref _TempPath, value);
        }

        private string _AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public string AppDataPath
        {
            get => _AppDataPath;
            set => Set(ref _AppDataPath, value);
        }

        private string _HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public string HomePath
        {
            get => _HomePath;
            set => Set(ref _HomePath, value);
        }

        private string _WinDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        public string WinDirPath
        {
            get => _WinDirPath;
            set => Set(ref _WinDirPath, value);
        }

        public bool DoubleTapToRenameFiles
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowFileExtensions
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowConfirmDeleteDialog
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AreLinuxFilesSupported
        {
            get => Get(false);
            set => Set(value);
        }

        public bool OpenNewTabPageOnStartup
        {
            get => Get(true);
            set => Set(value);
        }

        public bool OpenASpecificPageOnStartup
        {
            get => Get(false);
            set => Set(value);
        }

        public string OpenASpecificPageOnStartupPath
        {
            get => Get("");
            set => Set(value);
        }

        private void DetectAcrylicPreference()
        {
            if (localSettings.Values["AcrylicEnabled"] == null) { localSettings.Values["AcrylicEnabled"] = true; }
            AcrylicEnabled = (bool)localSettings.Values["AcrylicEnabled"];
        }

        private bool _AcrylicEnabled = true;

        public bool AcrylicEnabled
        {
            get => _AcrylicEnabled;
            set
            {
                if (value != _AcrylicEnabled)
                {
                    Set(ref _AcrylicEnabled, value);
                    localSettings.Values["AcrylicEnabled"] = value;
                }
            }
        }

        public event EventHandler ThemeModeChanged;

        public RelayCommand UpdateThemeElements => new RelayCommand(() =>
        {
            ThemeModeChanged?.Invoke(this, EventArgs.Empty);
        });

        public AcrylicTheme AcrylicTheme { get; set; }

        public int LayoutMode
        {
            get => Get(0); // List View
            set => Set(value);
        }

        public Type GetLayoutType()
        {
            Type type = null;
            switch (LayoutMode)
            {
                case 0:
                    type = typeof(GenericFileBrowser);
                    break;

                case 1:
                    type = typeof(GridViewBrowser);
                    break;

                case 2:
                    type = typeof(GridViewBrowser);
                    break;

                default:
                    type = typeof(GenericFileBrowser);
                    break;
            }
            return type;
        }

        public event EventHandler LayoutModeChangeRequested;

        public RelayCommand ToggleLayoutModeGridViewLarge => new RelayCommand(() =>
        {
            LayoutMode = 2; // Grid View

            GridViewSize = 375; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeGridViewMedium => new RelayCommand(() =>
        {
            LayoutMode = 2; // Grid View

            GridViewSize = 250; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeGridViewSmall => new RelayCommand(() =>
        {
            LayoutMode = 2; // Grid View

            GridViewSize = 125; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeTiles => new RelayCommand(() =>
        {
            LayoutMode = 1; // Tiles View

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeListView => new RelayCommand(() =>
        {
            LayoutMode = 0; // List View

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        private void DetectGridViewSize()
        {
            _GridViewSize = Get(125, "GridViewSize"); // Get GridView Size
        }

        private int _GridViewSize = 125; // Default Size

        public int GridViewSize
        {
            get => _GridViewSize;
            set
            {
                if (value < _GridViewSize) // Size down
                {
                    if (LayoutMode == 1) // Size down from tiles to list
                    {
                        LayoutMode = 0;
                        Set(0, "LayoutMode");
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode == 2 && value < 125) // Size down from grid to tiles
                    {
                        LayoutMode = 1;
                        Set(1, "LayoutMode");
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode != 0) // Resize grid view
                    {
                        _GridViewSize = (value >= 125) ? value : 125; // Set grid size to allow immediate UI update
                        Set(value);

                        if (LayoutMode != 2) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutMode = 2;
                            Set(2, "LayoutMode");
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }

                        GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
                else // Size up
                {
                    if (LayoutMode == 0) // Size up from list to tiles
                    {
                        LayoutMode = 1;
                        Set(1, "LayoutMode");
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else // Size up from tiles to grid
                    {
                        _GridViewSize = (LayoutMode == 1) ? 125 : (value <= 375) ? value : 375; // Set grid size to allow immediate UI update
                        Set(_GridViewSize);

                        if (LayoutMode != 2) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutMode = 2;
                            Set(2, "LayoutMode");
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }

                        if (value < 375) // Don't request a grid resize if it is already at the max size (375)
                            GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler GridViewSizeChangeRequested;

        public void Dispose()
        {
            DrivesManager.Dispose();
        }

        public bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = null)
        {
            propertyName = propertyName != null && propertyName.StartsWith("set_", StringComparison.InvariantCultureIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            TValue originalValue = default;

            if (_roamingSettings.Values.ContainsKey(propertyName))
            {
                originalValue = Get(originalValue, propertyName);

                _roamingSettings.Values[propertyName] = value;
                if (!base.Set(ref originalValue, value, propertyName)) return false;
            }
            else
            {
                _roamingSettings.Values[propertyName] = value;
            }

            return true;
        }

        public TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = null)
        {
            var name = propertyName ??
                       throw new ArgumentNullException(nameof(propertyName), "Cannot store property of unnamed.");

            name = name.StartsWith("get_", StringComparison.InvariantCultureIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            if (_roamingSettings.Values.ContainsKey(name))
            {
                var value = _roamingSettings.Values[name];

                if (!(value is TValue tValue))
                {
                    if (value is IConvertible)
                    {
                        tValue = (TValue)Convert.ChangeType(value, typeof(TValue));
                    }
                    else
                    {
                        var valueType = value.GetType();
                        var tryParse = typeof(TValue).GetMethod("TryParse", BindingFlags.Instance | BindingFlags.Public);

                        if (tryParse == null) return default;

                        var stringValue = value.ToString();
                        tValue = default;

                        var tryParseDelegate =
                            (TryParseDelegate<TValue>)Delegate.CreateDelegate(valueType, tryParse, false);

                        tValue = (tryParseDelegate?.Invoke(stringValue, out tValue) ?? false) ? tValue : default;
                    }

                    Set(tValue, propertyName); // Put the corrected value in settings.
                    return tValue;
                }

                return tValue;
            }

            return defaultValue;
        }

        private delegate bool TryParseDelegate<TValue>(string inValue, out TValue parsedValue);
    }
}
