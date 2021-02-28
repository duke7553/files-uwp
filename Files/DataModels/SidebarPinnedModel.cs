﻿using Files.Common;
using Files.Controllers;
using Files.Filesystem;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.DataModels
{
    public class SidebarPinnedModel
    {
        private SidebarPinnedController controller;

        private LocationItem favoriteSection, homeSection, quickAccessSection;

        [JsonIgnore]
        public SettingsViewModel AppSettings => App.AppSettings;

        [JsonProperty("items")]
        public List<string> Items { get; set; } = new List<string>();

        [JsonProperty("quickAccessItems")]
        public List<string> QuickAccessItems { get; set; } = new List<string>();

        public void SetController(SidebarPinnedController controller)
        {
            this.controller = controller;
        }

        public SidebarPinnedModel()
        {
            homeSection = new LocationItem()
            {
                Text = "SidebarHome".GetLocalized(),
                Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                Glyph = "\uea80",
                IsDefaultLocation = true,
                Path = "Home",
                ChildItems = new ObservableCollection<INavigationControlItem>()
            };
            favoriteSection = new LocationItem()
            {
                Text = "SidebarFavorites".GetLocalized(),
                Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                Glyph = "\ueb83",
                ChildItems = new ObservableCollection<INavigationControlItem>()
            };
            quickAccessSection = new LocationItem()
            {
                Text = "SidebarQuickAccess".GetLocalized(),
                ItemVisibility = (AppSettings.ShowQuickAccessSwitch.Equals(true) ? Visibility.Visible : Visibility.Collapsed),
                Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,                
                Glyph = "\uEA52",
                ChildItems = new ObservableCollection<INavigationControlItem>()
            };
        }

        /// <summary>
        /// Adds the default items to the navigation page
        /// </summary>
        public void AddDefaultItems()
        {
            Items.Add(AppSettings.DesktopPath);
            Items.Add(AppSettings.DownloadsPath);
            Items.Add(AppSettings.DocumentsPath);
            Items.Add(AppSettings.PicturesPath);
            Items.Add(AppSettings.MusicPath);
            Items.Add(AppSettings.VideosPath);
        }

        /// <summary>
        /// Adds the item to the navigation page
        /// </summary>
        /// <param name="item">Item to remove</param>
        public async void AddItem(string item)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);
                await AddItemToSidebarAsync(item);
                Save();
            }
        }

        public async void AddQuickAccessItem(string item)
        {
            if (!QuickAccessItems.Contains(item))
            {
                QuickAccessItems.Add(item);
                await AddItemToQuickAccessSidebarAsync(item);
                Save();
            }
        }

        public async Task ShowHideRecycleBinItemAsync(bool show)
        {
            await MainPage.SideBarItemsSemaphore.WaitAsync();
            try
            {
                if (show)
                {
                    var recycleBinItem = new LocationItem
                    {
                        Text = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                        Font = Application.Current.Resources["RecycleBinIcons"] as FontFamily,
                        Glyph = "\uEF87",
                        IsDefaultLocation = true,
                        Path = App.AppSettings.RecycleBinPath
                    };
                    // Add recycle bin to sidebar, title is read from LocalSettings (provided by the fulltrust process)
                    // TODO: the very first time the app is launched localized name not available
                    if (!favoriteSection.ChildItems.Any(x => x.Path == App.AppSettings.RecycleBinPath))
                    {
                        favoriteSection.ChildItems.Add(recycleBinItem);
                    }
                }
                else
                {
                    foreach (INavigationControlItem item in favoriteSection.ChildItems.ToList())
                    {
                        if (item is LocationItem && item.Path == App.AppSettings.RecycleBinPath)
                        {
                            favoriteSection.ChildItems.Remove(item);
                        }
                    }
                }
            }
            finally
            {
                MainPage.SideBarItemsSemaphore.Release();
            }
        }

        /// <summary>
        /// Removes the item from the navigation page
        /// </summary>
        /// <param name="item">Item to remove</param>
        public void RemoveItem(string item)
        {
            if (Items.Contains(item))
            {
                Items.Remove(item);
                RemoveStaleSidebarItems();
                Save();
            }
        }

        /// <summary>
        /// Removes the quick access items.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveQuickAccessItems(string item)
        {
            if (QuickAccessItems.Contains(item))
            {
                QuickAccessItems.Remove(item);
                RemoveStaleSidebarQuickAccessItems();
                Save();
            }
        }

        /// <summary>
        /// Moves the location item in the navigation sidebar from the old position to the new position
        /// </summary>
        /// <param name="locationItem">Location item to move</param>
        /// <param name="oldIndex">The old position index of the location item</param>
        /// <param name="newIndex">The new position index of the location item</param>
        /// <returns>True if the move was successful</returns>
        public bool MoveItem(INavigationControlItem locationItem, int oldIndex, int newIndex)
        {
            if (locationItem == null)
            {
                return false;
            }

            if (oldIndex >= 0 && newIndex >= 0)
            {
                favoriteSection.ChildItems.RemoveAt(oldIndex);
                favoriteSection.ChildItems.Insert(newIndex, locationItem);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Swaps two location items in the navigation sidebar
        /// </summary>
        /// <param name="firstLocationItem">The first location item</param>
        /// <param name="secondLocationItem">The second location item</param>
        public void SwapItems(INavigationControlItem firstLocationItem, INavigationControlItem secondLocationItem)
        {
            if (firstLocationItem == null || secondLocationItem == null)
            {
                return;
            }

            // A backup of the items, because the swapping of items requires removing and inserting them in the correct position
            var sidebarItemsBackup = new List<string>(this.Items);

            try
            {
                var indexOfFirstItemInMainPage = IndexOfItem(firstLocationItem);
                var indexOfSecondItemInMainPage = IndexOfItem(secondLocationItem);

                // Moves the items in the MainPage
                var result = MoveItem(firstLocationItem, indexOfFirstItemInMainPage, indexOfSecondItemInMainPage);

                // Moves the items in this model and saves the model
                if (result == true)
                {
                    var indexOfFirstItemInModel = this.Items.IndexOf(firstLocationItem.Path);
                    var indexOfSecondItemInModel = this.Items.IndexOf(secondLocationItem.Path);
                    if (indexOfFirstItemInModel >= 0 && indexOfSecondItemInModel >= 0)
                    {
                        this.Items.RemoveAt(indexOfFirstItemInModel);
                        this.Items.Insert(indexOfSecondItemInModel, firstLocationItem.Path);
                    }

                    Save();
                }
            }
            catch (Exception ex) when (
                ex is ArgumentException // Pinned item was invalid
                || ex is FileNotFoundException // Pinned item was deleted
                || ex is System.Runtime.InteropServices.COMException // Pinned item's drive was ejected
                || (uint)ex.HResult == 0x8007000F // The system cannot find the drive specified
                || (uint)ex.HResult == 0x800700A1) // The specified path is invalid (usually an mtp device was disconnected)
            {
                Debug.WriteLine($"An error occurred while swapping pinned items in the navigation sidebar. {ex.Message}");
                this.Items = sidebarItemsBackup;
                this.RemoveStaleSidebarItems();
                _ = this.AddAllItemsToSidebar();
            }
        }

        /// <summary>
        /// Returns the index of the location item in the navigation sidebar
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem)
        {
            return favoriteSection.ChildItems.IndexOf(locationItem);
        }

        /// <summary>
        /// Returns the index of the location item in the collection containing Navigation control items
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <param name="collection">The collection in which to find the location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem, List<INavigationControlItem> collection)
        {
            return collection.IndexOf(locationItem);
        }

        /// <summary>
        /// Saves the model
        /// </summary>
        public void Save() => controller?.SaveModel();

        /// <summary>
        /// Adds the item do the navigation sidebar
        /// </summary>
        /// <param name="path">The path which to save</param>
        /// <returns>Task</returns>
        public async Task AddItemToSidebarAsync(string path)
        {
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
            if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                var lastItem = favoriteSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(App.AppSettings.RecycleBinPath));
                int insertIndex = lastItem != null ? favoriteSection.ChildItems.IndexOf(lastItem) + 1 : 0;
                var locationItem = new LocationItem
                {
                    Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    Path = path,
                    Glyph = GetItemIcon(path),
                    IsDefaultLocation = false,
                    Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
                };

                if (!favoriteSection.ChildItems.Contains(locationItem))
                {
                    favoriteSection.ChildItems.Insert(insertIndex, locationItem);
                }
            }
            else
            {
                Debug.WriteLine($"Pinned item was invalid and will be removed from the file lines list soon: {res.ErrorCode}");
                RemoveItem(path);
            }
        }

        public async Task AddItemToQuickAccessSidebarAsync(string path)
        {
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
            if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                var lastItem = quickAccessSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(App.AppSettings.RecycleBinPath));
                int insertIndex = lastItem != null ? quickAccessSection.ChildItems.IndexOf(lastItem) + 1 : 0;
                var locationItem = new LocationItem
                {
                    Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    Path = path,
                    Glyph = GetItemIcon(path),
                    IsDefaultLocation = false,
                    Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
                };

                if (!quickAccessSection.ChildItems.Contains(locationItem))
                {
                    quickAccessSection.ChildItems.Insert(insertIndex, locationItem);
                }
            }
            else
            {
                Debug.WriteLine($"Pinned item was invalid and will be removed from the file lines list soon: {res.ErrorCode}");
                RemoveItem(path);
            }
        }

        /// <summary>
        /// Adds the item to sidebar asynchronous.
        /// </summary>
        /// <param name="section">The section.</param>
        private void AddItemToSidebarAsync(LocationItem section)
        {
            var lastItem = favoriteSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(App.AppSettings.RecycleBinPath));
            int insertIndex = lastItem != null ? favoriteSection.ChildItems.IndexOf(lastItem) + 1 : 0;

            if (!favoriteSection.ChildItems.Contains(section))
            {
                favoriteSection.ChildItems.Insert(insertIndex, section);
            }
        }

        /// <summary>
        /// Adds all items to the navigation sidebar
        /// </summary>
        public async Task AddAllItemsToSidebar()
        {
            await MainPage.SideBarItemsSemaphore.WaitAsync();
            try
            {
                MainPage.SideBarItems.BeginBulkOperation();

                if (homeSection != null)
                {
                    AddItemToSidebarAsync(homeSection);
                }

                if (App.AppSettings.ShowQuickAccessSwitch)
                {
                    MainPage.SideBarItems.Add(quickAccessSection);

                    for (int i = 0; i < QuickAccessItems.Count(); i++)
                    {
                        string path = QuickAccessItems[i];
                        await AddItemToQuickAccessSidebarAsync(path);
                    }
                }

                if (!MainPage.SideBarItems.Contains(favoriteSection))
                {
                    MainPage.SideBarItems.Add(favoriteSection);
                }

                MainPage.SideBarItems.EndBulkOperation();
            }
            finally
            {
                MainPage.SideBarItemsSemaphore.Release();
            }

            await ShowHideRecycleBinItemAsync(App.AppSettings.PinRecycleBinToSideBar);
        }

        /// <summary>
        /// Removes stale items in the navigation sidebar
        /// </summary>
        public void RemoveStaleSidebarItems()
        {
            // Remove unpinned items from sidebar
            for (int i = 0; i < favoriteSection.ChildItems.Count(); i++)
            {
                if (favoriteSection.ChildItems[i] is LocationItem)
                {
                    var item = favoriteSection.ChildItems[i] as LocationItem;
                    if (!item.IsDefaultLocation && !Items.Contains(item.Path))
                    {
                        favoriteSection.ChildItems.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the stale sidebar quick access items.
        /// </summary>
        public void RemoveStaleSidebarQuickAccessItems()
        {
            // Remove unpinned items from quick access
            for (int i = 0; i < quickAccessSection.ChildItems.Count(); i++)
            {
                if (quickAccessSection.ChildItems[i] is LocationItem)
                {
                    var item = quickAccessSection.ChildItems[i] as LocationItem;
                    if (!item.IsDefaultLocation && !QuickAccessItems.Contains(item.Path))
                    {
                        quickAccessSection.ChildItems.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the icon for the items in the navigation sidebar
        /// </summary>
        /// <param name="path">The path in the sidebar</param>
        /// <returns>The icon code</returns>
        public string GetItemIcon(string path)
        {
            string iconCode;
            
            if (path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\ue9f1";
            }
            else if (path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE91c";
            }
            else if (path.Equals(AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uea11";
            }
            else if (path.Equals(AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uea83";
            }
            else if (path.Equals(AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uead4";
            }
            else if (path.Equals(AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uec0d";
            }
            else if (Path.GetPathRoot(path).Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\ueb8b";
            }
            else
            {
                iconCode = "\uea55";
            }

            return iconCode;
        }
    }
}