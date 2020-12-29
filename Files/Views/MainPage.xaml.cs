﻿using Files.Common;
using Files.Controllers;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Files.Helpers.PathNormalization;

namespace Files.Views
{
    /// <summary>
    /// The root page of Files
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public static IMultitaskingControl MultitaskingControl { get; set; }

        private TabItem _SelectedTabItem;

        public TabItem SelectedTabItem
        {
            get
            {
                return _SelectedTabItem;
            }
            set
            {
                _SelectedTabItem = value;
                NotifyPropertyChanged(nameof(SelectedTabItem));
            }
        }

        public static ObservableCollection<TabItem> AppInstances = new ObservableCollection<TabItem>();
        public static ObservableCollection<INavigationControlItem> SideBarItems = new ObservableCollection<INavigationControlItem>();

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }
            AllowDrop = true;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            if (eventArgs.NavigationMode != NavigationMode.Back)
            {
                App.AppSettings = new SettingsViewModel();
                App.InteractionViewModel = new InteractionViewModel();
                App.SidebarPinnedController = new SidebarPinnedController();

                Helpers.ThemeHelper.Initialize();

                if (eventArgs.Parameter == null || (eventArgs.Parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
                {
                    try
                    {
                        if (App.AppSettings.ResumeAfterRestart)
                        {
                            App.AppSettings.ResumeAfterRestart = false;

                            foreach (string tabArgsString in App.AppSettings.LastSessionPages)
                            {
                                var tabArgs = TabItemArguments.Deserialize(tabArgsString);
                                await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                            }

                            if (!App.AppSettings.ContinueLastSessionOnStartUp)
                            {
                                App.AppSettings.LastSessionPages = null;
                            }
                        }
                        else if (App.AppSettings.OpenASpecificPageOnStartup)
                        {
                            if (App.AppSettings.PagesOnStartupList != null)
                            {
                                foreach (string path in App.AppSettings.PagesOnStartupList)
                                {
                                    await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
                                }
                            }
                            else
                            {
                                await AddNewTabAsync();
                            }
                        }
                        else if (App.AppSettings.ContinueLastSessionOnStartUp)
                        {
                            if (App.AppSettings.LastSessionPages != null)
                            {
                                foreach (string tabArgsString in App.AppSettings.LastSessionPages)
                                {
                                    var tabArgs = TabItemArguments.Deserialize(tabArgsString);
                                    await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                                }
                                var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "NewTab".GetLocalized() };
                                App.AppSettings.LastSessionPages = new string[] { defaultArg.Serialize() };
                            }
                            else
                            {
                                await AddNewTabAsync();
                            }
                        }
                        else
                        {
                            await AddNewTabAsync();
                        }
                    }
                    catch (Exception)
                    {
                        await AddNewTabAsync();
                    }
                }
                else
                {
                    if (eventArgs.Parameter is string navArgs)
                    {
                        await AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
                    }
                    else if (eventArgs.Parameter is TabItemArguments tabArgs)
                    {
                        await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                    }
                }

                // Check for required updates
                AppUpdater updater = new AppUpdater();
                updater.CheckForUpdatesAsync();

                // Initial setting of SelectedTabItem
                Frame rootFrame = Window.Current.Content as Frame;
                var mainView = rootFrame.Content as MainPage;
                mainView.SelectedTabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            }
        }

        public static async Task AddNewTabAsync()
        {
            await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
        }

        public static async void AddNewTabAtIndex(object sender, RoutedEventArgs e)
        {
            await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
        }

        public static async void DuplicateTabAtIndex(object sender, RoutedEventArgs e)
        {
            var tabItem = ((FrameworkElement)sender).DataContext as TabItem;
            var index = AppInstances.IndexOf(tabItem);

            if (AppInstances[index].TabItemArguments != null)
            {
                var tabArgs = AppInstances[index].TabItemArguments;
                await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg, index + 1);
            }
            else
            {
                await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
            }
        }

        public static async void MoveTabToNewWindow(object sender, RoutedEventArgs e)
        {
            var tabItem = ((FrameworkElement)sender).DataContext as TabItem;
            var index = AppInstances.IndexOf(tabItem);
            var tabItemArguments = AppInstances[index].TabItemArguments;

            MultitaskingControl.Items.RemoveAt(index);

            if (tabItemArguments != null)
            {
                await Interacts.Interaction.OpenTabInNewWindowAsync(tabItemArguments.Serialize());
            }
            else
            {
                await Interacts.Interaction.OpenPathInNewWindowAsync("NewTab".GetLocalized());
            }
        }

        public static async Task AddNewTabByParam(Type type, object tabViewItemArgs, int atIndex = -1)
        {
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            TabItem tabItem = new TabItem()
            {
                Header = null,
                IconSource = fontIconSource,
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = tabViewItemArgs
            };
            tabItem.Control.ContentChanged += Control_ContentChanged;
            await UpdateTabInfo(tabItem, tabViewItemArgs);
            AppInstances.Insert(atIndex == -1 ? AppInstances.Count : atIndex, tabItem);
        }

        public static async Task AddNewTabByPathAsync(Type type, string path, int atIndex = -1)
        {
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            if (string.IsNullOrEmpty(path))
            {
                path = "NewTab".GetLocalized();
            }

            TabItem tabItem = new TabItem()
            {
                Header = null,
                IconSource = fontIconSource,
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = path
            };
            tabItem.Control.ContentChanged += Control_ContentChanged;
            await SetSelectedTabInfoAsync(tabItem, path);
            AppInstances.Insert(atIndex == -1 ? AppInstances.Count : atIndex, tabItem);
        }

        private static async Task SetSelectedTabInfoAsync(TabItem selectedTabItem, string currentPath, string tabHeader = null)
        {
            selectedTabItem.AllowStorageItemDrop = true;

            string tabLocationHeader;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            Microsoft.UI.Xaml.Controls.IconSource tabIcon;
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            if (currentPath == null || currentPath == "SidebarSettings/Text".GetLocalized())
            {
                tabLocationHeader = "SidebarSettings/Text".GetLocalized();
                fontIconSource.Glyph = "\xeb5d";
            }
            else if (currentPath == null || currentPath == "NewTab".GetLocalized() || currentPath == "Home")
            {
                tabLocationHeader = "NewTab".GetLocalized();
                fontIconSource.Glyph = "\xe90c";
            }
            else if (currentPath.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarDesktop".GetLocalized();
                fontIconSource.Glyph = "\xe9f1";
            }
            else if (currentPath.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarDownloads".GetLocalized();
                fontIconSource.Glyph = "\xe91c";
            }
            else if (currentPath.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarDocuments".GetLocalized();
                fontIconSource.Glyph = "\xEA11";
            }
            else if (currentPath.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarPictures".GetLocalized();
                fontIconSource.Glyph = "\xEA83";
            }
            else if (currentPath.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarMusic".GetLocalized();
                fontIconSource.Glyph = "\xead4";
            }
            else if (currentPath.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarVideos".GetLocalized();
                fontIconSource.Glyph = "\xec0d";
            }
            else if (currentPath.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                fontIconSource.FontFamily = Application.Current.Resources["RecycleBinIcons"] as FontFamily;
                fontIconSource.Glyph = "\xEF87";
            }
            else if (App.AppSettings.OneDrivePath != null && currentPath.Equals(App.AppSettings.OneDrivePath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "OneDrive";
                fontIconSource.Glyph = "\xe9b7";
            }
            else if (App.AppSettings.OneDriveCommercialPath != null && currentPath.Equals(App.AppSettings.OneDriveCommercialPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "OneDrive Commercial";
                fontIconSource.Glyph = "\xe9b7";
            }
            else
            {
                // If path is a drive's root
                if (NormalizePath(Path.GetPathRoot(currentPath)) == NormalizePath(currentPath))
                {
                    if (NormalizePath(currentPath) != NormalizePath("A:") && NormalizePath(currentPath) != NormalizePath("B:"))
                    {
                        var remDriveNames = (await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.DisplayName);
                        var matchingDriveName = remDriveNames.FirstOrDefault(x => NormalizePath(currentPath).Contains(x.ToUpperInvariant()));

                        if (matchingDriveName == null)
                        {
                            fontIconSource.Glyph = "\xeb8b";
                            tabLocationHeader = NormalizePath(currentPath);
                        }
                        else
                        {
                            fontIconSource.Glyph = "\xec0a";
                            tabLocationHeader = matchingDriveName;
                        }
                    }
                    else
                    {
                        fontIconSource.Glyph = "\xeb4a";
                        tabLocationHeader = NormalizePath(currentPath);
                    }
                }
                else
                {
                    fontIconSource.Glyph = "\xea55";
                    tabLocationHeader = currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();
                }
            }
            if (tabHeader != null)
            {
                tabLocationHeader = tabHeader;
            }
            tabIcon = fontIconSource;
            selectedTabItem.Header = tabLocationHeader;
            selectedTabItem.IconSource = tabIcon;
        }

        private static async void Control_ContentChanged(object sender, TabItemArguments e)
        {
            var matchingTabItem = MainPage.AppInstances.SingleOrDefault(x => x.Control == sender);
            if (matchingTabItem == null)
            {
                return;
            }
            await UpdateTabInfo(matchingTabItem, e.NavigationArg);
        }

        private static async Task UpdateTabInfo(TabItem tabItem, object navigationArg)
        {
            if (navigationArg is PaneNavigationArguments paneArgs)
            {
                var leftHeader = paneArgs.LeftPaneNavPathParam != null ? new DirectoryInfo(paneArgs.LeftPaneNavPathParam).Name : null;
                var rightHeader = paneArgs.RightPaneNavPathParam != null ? new DirectoryInfo(paneArgs.RightPaneNavPathParam).Name : null;
                if (leftHeader != null && rightHeader != null)
                {
                    await SetSelectedTabInfoAsync(tabItem, paneArgs.LeftPaneNavPathParam, $"{leftHeader} | {rightHeader}");
                }
                else
                {
                    await SetSelectedTabInfoAsync(tabItem, paneArgs.LeftPaneNavPathParam);
                }
            }
            else if (navigationArg is string pathArgs)
            {
                await SetSelectedTabInfoAsync(tabItem, pathArgs);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            int indexToSelect = 0;

            switch (sender.Key)
            {
                case VirtualKey.Number1:
                    indexToSelect = 0;
                    break;

                case VirtualKey.Number2:
                    indexToSelect = 1;
                    break;

                case VirtualKey.Number3:
                    indexToSelect = 2;
                    break;

                case VirtualKey.Number4:
                    indexToSelect = 3;
                    break;

                case VirtualKey.Number5:
                    indexToSelect = 4;
                    break;

                case VirtualKey.Number6:
                    indexToSelect = 5;
                    break;

                case VirtualKey.Number7:
                    indexToSelect = 6;
                    break;

                case VirtualKey.Number8:
                    indexToSelect = 7;
                    break;

                case VirtualKey.Number9:
                    // Select the last tab
                    indexToSelect = AppInstances.Count - 1;
                    break;
            }

            // Only select the tab if it is in the list
            if (indexToSelect < AppInstances.Count)
            {
                App.InteractionViewModel.TabStripSelectedIndex = indexToSelect;
            }
            args.Handled = true;
        }

        private async void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (AppInstances.Count == 1)
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else
            {
                if (App.InteractionViewModel.TabStripSelectedIndex >= AppInstances.Count)
                {
                    AppInstances.RemoveAt(AppInstances.Count - 1);
                }
                else
                {
                    AppInstances.RemoveAt(App.InteractionViewModel.TabStripSelectedIndex);
                }
            }
            args.Handled = true;
        }

        private async void AddNewInstanceAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            if (!shift)
            {
                await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
            }
            else // ctrl + shif + t, restore recently closed tab
            {
                if (MultitaskingControl.RecentlyClosedTabs.Any())
                {
                    var lastTab = MultitaskingControl.RecentlyClosedTabs.Last();
                    MultitaskingControl.RecentlyClosedTabs.Remove(lastTab);
                    await AddNewTabByParam(lastTab.TabItemArguments.InitialPageType, lastTab.TabItemArguments.NavigationArg);
                }
            }
            args.Handled = true;
        }

        private async void OpenNewWindowAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var filesUWPUri = new Uri("files-uwp:");
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            MultitaskingControl = HorizontalMultitaskingControl;
        }
    }
}