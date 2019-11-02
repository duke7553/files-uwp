﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using System.ComponentModel;
using Files.Filesystem;
using Files.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using System.Collections;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using System.IO;
using System.Reflection;
using Files.Dialogs;

namespace Files.Interacts
{
    public class Interaction
    {
        private ProHome tabInstance;
        InstanceTabsView instanceTabsView;
        public Interaction()
        {
            tabInstance = App.selectedTabInstance;
            Frame rootFrame = Window.Current.Content as Frame;
            instanceTabsView = rootFrame.Content as InstanceTabsView;
        }

        public void List_ItemClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            OpenSelectedItems(false);
        }

        public async void OpenInNewWindowItem_Click(object sender, RoutedEventArgs e)
        {
            var CurrentSourceType = App.selectedTabInstance.accessibleContentFrame.CurrentSourcePageType;
            int index = -1;
            if (CurrentSourceType == typeof(GenericFileBrowser))
            {
                index = (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedIndex;
            }
            else if (CurrentSourceType == typeof(PhotoAlbum))
            {
                index = (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedIndex;
            }
            var selectedItemPath = tabInstance.instanceViewModel.FilesAndFolders[index].FilePath;
            var folderUri = new Uri("files-uwp:" + "?folder=" + @selectedItemPath);

            await Launcher.LaunchUriAsync(folderUri);
        }

        public void OpenDirectoryInNewTab_Click(object sender, RoutedEventArgs e)
        {
            var CurrentSourceType = App.selectedTabInstance.accessibleContentFrame.CurrentSourcePageType;
            int index = -1;
            if(CurrentSourceType == typeof(GenericFileBrowser))
            {
                index = (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedIndex;
            }
            else if(CurrentSourceType == typeof(PhotoAlbum))
            {
                index = (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedIndex;
            }
            var selectedItemPath = tabInstance.instanceViewModel.FilesAndFolders[index].FilePath;

            instanceTabsView.AddNewTab(typeof(ProHome), selectedItemPath);
        }

        public async void OpenDirectoryInTerminal(object sender, RoutedEventArgs e)
        {

            ApplicationData.Current.LocalSettings.Values["Application"] = "cmd.exe";
            if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                ApplicationData.Current.LocalSettings.Values["Arguments"] = "/k \"cd /d "+ tabInstance.instanceViewModel.Universal.path + "&& title Command Prompt" + "\""; 
            }
            else if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                ApplicationData.Current.LocalSettings.Values["Arguments"] = "/k \"cd /d " + tabInstance.instanceViewModel.Universal.path + "&& title Command Prompt" + "\"";
            }

            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public async void PinItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var selectedDataItem = tabInstance.instanceViewModel.FilesAndFolders[(tabInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView.SelectedIndex];
                StorageFolder cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;

                try
                {
                    List<string> items = new List<string>();
                    items.Add(selectedDataItem.FilePath);
                    var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
                    await FileIO.AppendLinesAsync(ListFile, items);
                }
                catch (FileNotFoundException)
                {
                    List<string> items = new List<string>();
                    items.Add(selectedDataItem.FilePath);
                    var createdListFile = await cacheFolder.CreateFileAsync("PinnedItems.txt");
                    await FileIO.WriteLinesAsync(createdListFile, items);
                } 
            }
            else if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                var selectedDataItem = tabInstance.instanceViewModel.FilesAndFolders[(tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedIndex];
                StorageFolder cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;

                try
                {
                    List<string> items = new List<string>();
                    items.Add(selectedDataItem.FilePath);
                    var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
                    await FileIO.AppendLinesAsync(ListFile, items);
                }
                catch (FileNotFoundException)
                {
                    List<string> items = new List<string>();
                    items.Add(selectedDataItem.FilePath);
                    var createdListFile = await cacheFolder.CreateFileAsync("PinnedItems.txt");
                    await FileIO.WriteLinesAsync(createdListFile, items);
                }
            }
            var CurrentInstance = App.selectedTabInstance;
            CurrentInstance.PopulatePinnedSidebarItems();
        }

        public void GetPath_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.Clear();
            DataPackage data = new DataPackage();
            if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                data.SetText(tabInstance.instanceViewModel.Universal.path);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
            else if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                data.SetText(tabInstance.instanceViewModel.Universal.path);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
            // Eventually notify user via flyout            

        }

        public static async Task LaunchExe(string ApplicationPath)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");
            ApplicationData.Current.LocalSettings.Values["Application"] = ApplicationPath;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public async void CommandInvokedHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://home"));
        }

        public async void GrantAccessPermissionHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }

        public DataGrid dataGrid;

        public void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            dataGrid = (DataGrid)sender;
            var RowPressed = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if(RowPressed != null)
            {
                var ObjectPressed = ((ReadOnlyObservableCollection<ListedItem>)dataGrid.ItemsSource)[RowPressed.GetIndex()];
                // Check if RightTapped row is currently selected
                var CurrentInstance = App.selectedTabInstance;
                if ((CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Contains(ObjectPressed))
                    return;
                // The following code is only reachable when a user RightTapped an unselected row
                dataGrid.SelectedItems.Clear();
                dataGrid.SelectedItems.Add(ObjectPressed);
            }
            
        }

        public static void FindChildren<T>(List<T> results, DependencyObject startNode) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(startNode);
            for (int i = 0; i < count; i++)
            {
                DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
                if ((current.GetType()).Equals(typeof(T)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(T))))
                {
                    T asType = (T)current;
                    results.Add(asType);
                }
                FindChildren<T>(results, current);
            }
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            T parent = null;
            DependencyObject CurrentParent = VisualTreeHelper.GetParent(child);
            while (CurrentParent != null)
            {
                if (CurrentParent is T)
                {
                    parent = (T)CurrentParent;
                    break;
                }
                CurrentParent = VisualTreeHelper.GetParent(CurrentParent);

            }
            return parent;
        }
        
        public void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedItems(true);
        }

        private async void OpenSelectedItems(bool displayApplicationPicker)
        {
            try
            {
                List<ListedItem> selectedItems;
                int selectedItemCount;
                Type sourcePageType = App.selectedTabInstance.accessibleContentFrame.SourcePageType;
                if (sourcePageType == typeof(GenericFileBrowser))
                {
                    DataGrid data =(tabInstance.accessibleContentFrame.Content as GenericFileBrowser).data;
                    selectedItems = data.SelectedItems.Cast<ListedItem>().ToList();
                    selectedItemCount = data.SelectedItems.Count;
                }
                else
                {
                    GridView gv = (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv;
                    selectedItems = gv.SelectedItems.Cast<ListedItem>().ToList();
                    selectedItemCount = gv.SelectedItems.Count;
                }
                foreach (ListedItem clickedOnItem in selectedItems)
                {
                    // Access MRU List
                    var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                    if (clickedOnItem.FileType == "Folder")
                    {
                        if (selectedItemCount == 1)
                        {
                            // Add location to MRU List
                            mostRecentlyUsed.Add(await StorageFolder.GetFolderFromPathAsync(clickedOnItem.FilePath));

                            tabInstance.instanceViewModel.Universal.path = clickedOnItem.FilePath;
                            tabInstance.PathText.Text = clickedOnItem.FilePath;
                            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                            {
                                (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).TextState.isVisible = Visibility.Collapsed;
                            }
                            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                            {
                                (tabInstance.accessibleContentFrame.Content as PhotoAlbum).TextState.isVisible = Visibility.Collapsed;
                            }
                            tabInstance.FS.isEnabled = false;
                            if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                            {
                                tabInstance.PathText.Text = "Desktop";
                                tabInstance.locationsList.SelectedIndex = 1;
                                tabInstance.accessibleContentFrame.Navigate(sourcePageType, YourHome.DesktopPath, new SuppressNavigationTransitionInfo());

                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                            {
                                tabInstance.PathText.Text = "Documents";
                                tabInstance.locationsList.SelectedIndex = 3;
                                tabInstance.accessibleContentFrame.Navigate(sourcePageType, YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                            {
                                tabInstance.PathText.Text = "Downloads";
                                tabInstance.locationsList.SelectedIndex = 2;
                                tabInstance.accessibleContentFrame.Navigate(sourcePageType, YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                            {
                                tabInstance.PathText.Text = "Pictures";
                                tabInstance.locationsList.SelectedIndex = 4;
                                tabInstance.accessibleContentFrame.Navigate(sourcePageType, YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                            {
                                tabInstance.PathText.Text = "Music";
                                tabInstance.locationsList.SelectedIndex = 5;
                                tabInstance.accessibleContentFrame.Navigate(sourcePageType, YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                            {
                                tabInstance.PathText.Text = "OneDrive";
                                tabInstance.drivesList.SelectedIndex = 1;
                                tabInstance.accessibleContentFrame.Navigate(sourcePageType, YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                            {
                                tabInstance.PathText.Text = "Videos";
                                if (sourcePageType == typeof(GenericFileBrowser))
                                    tabInstance.locationsList.SelectedIndex = 6;
                                else
                                    tabInstance.drivesList.SelectedIndex = 6;
                                tabInstance.accessibleContentFrame.Navigate(sourcePageType, YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                            }
                            else
                            {
                                if (sourcePageType == typeof(GenericFileBrowser))
                                {
                                    if (clickedOnItem.FilePath.Split(@"\")[0].Contains("C:"))
                                    {
                                        tabInstance.drivesList.SelectedIndex = 0;
                                    }
                                    tabInstance.instanceViewModel.Universal.path = clickedOnItem.FilePath;
                                    tabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), tabInstance.instanceViewModel.Universal.path, new SuppressNavigationTransitionInfo());
                                }
                                else
                                {
                                    tabInstance.drivesList.SelectedIndex = 0; if (clickedOnItem.FilePath.Split(@"\")[0].Contains("C:"))
                                        tabInstance.PathText.Text = clickedOnItem.FilePath;
                                    tabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), clickedOnItem.FilePath, new SuppressNavigationTransitionInfo());
                                }
                            }
                        }
                        else
                        {
                            var selectedItemPath = clickedOnItem.FilePath;
                            instanceTabsView.AddNewTab(typeof(ProHome), selectedItemPath);
                        }
                    }
                    else if (clickedOnItem.FileType == "Application")
                    {
                        // Add location to MRU List
                        mostRecentlyUsed.Add(await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath));
                        await LaunchExe(clickedOnItem.FilePath);
                    }
                    else
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                        // Add location to MRU List
                        mostRecentlyUsed.Add(file);
                        var options = new LauncherOptions
                        {
                            DisplayApplicationPicker = displayApplicationPicker
                        };
                        await Launcher.LaunchFileAsync(file, options);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                MessageDialog dialog = new MessageDialog("The file you are attempting to access may have been moved or deleted.", "File Not Found");
                await dialog.ShowAsync();
                NavigationActions.Refresh_Click(null, null);
            }
        }

        public void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager manager = DataTransferManager.GetForCurrentView();
            manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);
            DataTransferManager.ShowShareUI();
        }

        private async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();
            if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.selectedTabInstance;

                foreach (ListedItem li in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
                {
                    if (li.FileType == "Folder")
                    {
                        var folderAsItem = await StorageFolder.GetFolderFromPathAsync(li.FilePath);
                        items.Add(folderAsItem);
                    }
                    else
                    {
                        var fileAsItem = await StorageFile.GetFileFromPathAsync(li.FilePath);
                        items.Add(fileAsItem);
                    }
                }
            }
            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                foreach (ListedItem li in (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems)
                {
                    if (li.FileType == "Folder")
                    {
                        var folderAsItem = await StorageFolder.GetFolderFromPathAsync(li.FilePath);
                        items.Add(folderAsItem);
                    }
                    else
                    {
                        var fileAsItem = await StorageFile.GetFileFromPathAsync(li.FilePath);
                        items.Add(fileAsItem);
                    }
                }
            }
            
            DataRequest dataRequest = args.Request;
            dataRequest.Data.SetStorageItems(items);
            dataRequest.Data.Properties.Title = "Data Shared From Files";
            dataRequest.Data.Properties.Description = "The items you selected will be shared";
            dataRequestDeferral.Complete();
        }

        public async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                {
                    var CurrentInstance = App.selectedTabInstance;
                    List<ListedItem> selectedItems = new List<ListedItem>();
                    foreach (ListedItem selectedItem in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
                    {
                        selectedItems.Add(selectedItem);
                    }

                    CurrentInstance.deleteProgressBoxIndicator.Maximum = selectedItems.Count;
                    CurrentInstance.deleteProgressBoxIndicator.Value = 0;
                    CurrentInstance.deleteProgressBoxTitle.Text = "Moving " + selectedItems.Count + " items to the Recycle Bin";
                    if(selectedItems.Count > 5)
                    {
                        CurrentInstance.deleteProgressBox.Visibility = Visibility.Visible;
                    }
                    CurrentInstance.deleteProgressBoxTextInfo.Text = "Removing item (0/" + selectedItems.Count + ")";
                    foreach (ListedItem storItem in selectedItems)
                    {
                        CurrentInstance.deleteProgressBoxTextInfo.Text = "Removing item (" + (CurrentInstance.deleteProgressBoxIndicator.Value + 1) + "/" + selectedItems.Count + ")";
                        try
                        {
                            if (storItem.FileType != "Folder")
                            {
                                var item = await StorageFile.GetFileFromPathAsync(storItem.FilePath);
                                await item.DeleteAsync(StorageDeleteOption.Default);

                            }
                            else
                            {
                                var item = await StorageFolder.GetFolderFromPathAsync(storItem.FilePath);
                                await item.DeleteAsync(StorageDeleteOption.Default);

                            }
                        }
                        catch (FileLoadException)
                        {
                            // try again
                            if (storItem.FileType != "Folder")
                            {
                                var item = await StorageFile.GetFileFromPathAsync(storItem.FilePath);
                                await item.DeleteAsync(StorageDeleteOption.Default);

                            }
                            else
                            {
                                var item = await StorageFolder.GetFolderFromPathAsync(storItem.FilePath);
                                await item.DeleteAsync(StorageDeleteOption.Default);

                            }
                        }

                        tabInstance.instanceViewModel.RemoveFileOrFolder(storItem);
                        CurrentInstance.deleteProgressBoxIndicator.Value++;
                    }
                    CurrentInstance.deleteProgressBox.Visibility = Visibility.Collapsed;
                    CurrentInstance.FS.isEnabled = false;
                }
                else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                {
                    var CurrentInstance = App.selectedTabInstance;
                    List<ListedItem> selectedItems = new List<ListedItem>();
                    foreach (ListedItem selectedItem in (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems)
                    {
                        selectedItems.Add(selectedItem);
                    }

                    CurrentInstance.deleteProgressBoxIndicator.Maximum = selectedItems.Count;
                    CurrentInstance.deleteProgressBoxIndicator.Value = 0;
                    CurrentInstance.deleteProgressBoxTitle.Text = "Moving " + selectedItems.Count + " items to the Recycle Bin";

                    if (selectedItems.Count > 5)
                    {
                        CurrentInstance.deleteProgressBox.Visibility = Visibility.Visible;
                    }
                    CurrentInstance.deleteProgressBoxTextInfo.Text = "Removing item (0/" + selectedItems.Count + ")";

                    foreach (ListedItem storItem in selectedItems)
                    {
                        CurrentInstance.deleteProgressBoxTextInfo.Text = "Removing item (" + (CurrentInstance.deleteProgressBoxIndicator.Value + 1) + "/" + selectedItems.Count + ")";
                        try
                        {
                            if (storItem.FileType != "Folder")
                            {
                                var item = await StorageFile.GetFileFromPathAsync(storItem.FilePath);
                                await item.DeleteAsync(StorageDeleteOption.Default);

                            }
                            else
                            {
                                var item = await StorageFolder.GetFolderFromPathAsync(storItem.FilePath);
                                await item.DeleteAsync(StorageDeleteOption.Default);

                            }
                        }
                        catch (FileLoadException)
                        {
                            // try again
                            if (storItem.FileType != "Folder")
                            {
                                var item = await StorageFile.GetFileFromPathAsync(storItem.FilePath);
                                await item.DeleteAsync(StorageDeleteOption.Default);

                            }
                            else
                            {
                                var item = await StorageFolder.GetFolderFromPathAsync(storItem.FilePath);
                                await item.DeleteAsync(StorageDeleteOption.Default);

                            }
                        }

                        tabInstance.instanceViewModel.RemoveFileOrFolder(storItem);
                        CurrentInstance.deleteProgressBoxIndicator.Value++;
                    }
                    CurrentInstance.deleteProgressBox.Visibility = Visibility.Collapsed;
                    CurrentInstance.FS.isEnabled = false;
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageDialog AccessDeniedDialog = new MessageDialog("Access Denied", "Unable to delete this item");
                await AccessDeniedDialog.ShowAsync();
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("Attention: Tried to delete an item that could be found");
            }
        }

        public void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var fileBrowser = App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser;
                if (fileBrowser.AllView.SelectedItem != null)
                    fileBrowser.AllView.CurrentColumn = fileBrowser.AllView.Columns[1];
                fileBrowser.AllView.BeginEdit();
            }
            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                var photoAlbum = App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum;
                photoAlbum.StartRename();
            }
        }

        public async Task<bool> RenameFileItem(ListedItem item, string oldName, string newName)
        {
            if (oldName == newName)
                return true;
            bool isRenamedSameNameDiffCase = oldName.ToLower() == newName.ToLower();
            try
            {
                if (newName != "")
                {
                    if (item.FileType == "Folder")
                    {
                        var folder = await StorageFolder.GetFolderFromPathAsync(item.FilePath);
                        if (isRenamedSameNameDiffCase)
                            throw new InvalidOperationException();
                        //await folder.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                        else
                            await folder.RenameAsync(newName, NameCollisionOption.FailIfExists);
                    }
                    else
                    {
                        var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                        if (isRenamedSameNameDiffCase)
                            throw new InvalidOperationException();
                        //await file.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                        else
                            await file.RenameAsync(newName, NameCollisionOption.FailIfExists);
                    }
                }
            }
            catch (Exception)
            {
                MessageDialog itemAlreadyExistsDialog = new MessageDialog("An item with this name already exists in this folder", "Try again");
                await itemAlreadyExistsDialog.ShowAsync();
                return false;
            }
            tabInstance.FS.isEnabled = false;
            return true;
        }

        public List<DataGridRow> dataGridRows = new List<DataGridRow>();
        public List<GridViewItem> gridViewItems = new List<GridViewItem>();
        public async void CutItem_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Move;
            App.pathsToDeleteAfterPaste.Clear();
            List<IStorageItem> items = new List<IStorageItem>();
            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.selectedTabInstance;
                if ((CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Count != 0)
                {
                    dataGridRows.Clear();
                    FindChildren<DataGridRow>(dataGridRows, (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).GFBPageName.Content);
                    
                    // First, reset DataGrid Rows that may be in "cut" command mode
                    foreach (DataGridRow row in dataGridRows)
                    {
                        if ((CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.Columns[0].GetCellContent(row).Opacity < 1)
                        {
                            (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.Columns[0].GetCellContent(row).Opacity = 1;
                        }
                    }

                    foreach (ListedItem StorItem in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
                    {
                        IEnumerator allItems = (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.ItemsSource.GetEnumerator();
                        int index = -1;
                        while (allItems.MoveNext())
                        {
                            index++;
                            var item = allItems.Current;
                            if(item == StorItem)
                            {
                                DataGridRow dataGridRow = dataGridRows[index];
                                (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.Columns[0].GetCellContent(dataGridRow).Opacity = 0.4;
                            }
                        }

                        App.pathsToDeleteAfterPaste.Add(StorItem.FilePath);
                        if (StorItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                    }
                }
            }
            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                var CurrentInstance = App.selectedTabInstance;
                if ((CurrentInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems.Count != 0)
                {

                    gridViewItems.Clear();
                    FindChildren<GridViewItem>(gridViewItems, (CurrentInstance.accessibleContentFrame.Content as PhotoAlbum).PAPageName.Content);

                    // First, reset GridView items that may be in "cut" command mode
                    foreach (GridViewItem gridViewItem in gridViewItems)
                    {
                        List<Grid> itemContentGrids = new List<Grid>();
                        FindChildren<Grid>(itemContentGrids, (CurrentInstance.accessibleContentFrame.Content as PhotoAlbum).gv.ContainerFromItem(gridViewItem.Content));
                        var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                        if (imageOfItem.Opacity < 1)
                        {
                            imageOfItem.Opacity = 1;
                        }
                    }

                    foreach (ListedItem StorItem in (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems)
                    {
                        GridViewItem itemToDimForCut = (GridViewItem) (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.ContainerFromItem(StorItem);
                        List<Grid> itemContentGrids = new List<Grid>();
                        FindChildren<Grid>(itemContentGrids, (CurrentInstance.accessibleContentFrame.Content as PhotoAlbum).gv.ContainerFromItem(itemToDimForCut.Content));
                        var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                        imageOfItem.Opacity = 0.4;
                    
                        App.pathsToDeleteAfterPaste.Add(StorItem.FilePath);
                        if (StorItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                    }
                }
            }
            IEnumerable<IStorageItem> EnumerableOfItems = items;
            dataPackage.SetStorageItems(EnumerableOfItems);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
        }
        public string CopySourcePath;
        public async void CopyItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            List<IStorageItem> items = new List<IStorageItem>();
            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.selectedTabInstance;
                CopySourcePath = tabInstance.instanceViewModel.Universal.path;

                if ((CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
                    {
                        if (StorItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                    }
                }
            }
            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                CopySourcePath = tabInstance.instanceViewModel.Universal.path;

                if ((tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems)
                    {
                        if (StorItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                    }
                }
            }
            if (items?.Count > 0)
            {
                IEnumerable<IStorageItem> EnumerableOfItems = items;
                dataPackage.SetStorageItems(EnumerableOfItems);
                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }

        }

        public async void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            string DestinationPath = null;
            int oldCount;
            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                DestinationPath = tabInstance.instanceViewModel.Universal.path;
                oldCount = tabInstance.instanceViewModel.FilesAndFolders.Count;
            }
            else if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                DestinationPath = tabInstance.instanceViewModel.Universal.path;
                oldCount = tabInstance.instanceViewModel.FilesAndFolders.Count;
            }
            DataPackageView packageView = Clipboard.GetContent();
            var ItemsToPaste = await packageView.GetStorageItemsAsync();
            foreach (IStorageItem item in ItemsToPaste)
            {
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    CloneDirectoryAsync(item.Path, DestinationPath, item.Name);
                }
                else if (item.IsOfType(StorageItemTypes.File))
                {
                    StorageFile ClipboardFile = await StorageFile.GetFileFromPathAsync(item.Path);
                    await ClipboardFile.CopyAsync(await StorageFolder.GetFolderFromPathAsync(DestinationPath), item.Name, NameCollisionOption.GenerateUniqueName);
                }
            }

            if (packageView.RequestedOperation == DataPackageOperation.Move)
            {
                foreach (string path in App.pathsToDeleteAfterPaste)
                {
                    if (path.Contains("."))
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                        await file.DeleteAsync();
                    }
                    if (!path.Contains("."))
                    {
                        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                        await folder.DeleteAsync();
                    }
                }
            }

        }

        public async void CloneDirectoryAsync(string SourcePath, string DestinationPath, string sourceRootName)
        {
            StorageFolder SourceFolder = await StorageFolder.GetFolderFromPathAsync(SourcePath);
            StorageFolder DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);
            try
            {
                await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.FailIfExists);      
                
                DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath + @"\" + sourceRootName);
                foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
                {
                    await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
                }
                foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
                {
                    CloneDirectoryAsync(folderinSourceDir.Path, DestinationFolder.Path, folderinSourceDir.Name);
                }
            }
            catch (Exception)
            {
                tryagain:
                MessageDialog AlreadyExistsDialog = new MessageDialog("An item with this name already exists. To continue, please enter a different name.", "Name in use");
                AlreadyExistsDialog.Commands.Add(new UICommand("Enter a name"));
                await AlreadyExistsDialog.ShowAsync();
                string newName = null;
                if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                {
                    var CurrentInstance = App.selectedTabInstance;
                    await (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).NameBox.ShowAsync();
                    newName = (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).inputForRename;
                }
                else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                {
                    var CurrentInstance = App.selectedTabInstance;
                    await (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).NameBox.ShowAsync();
                    newName = (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).inputForRename;
                }
                
                if(newName != sourceRootName)
                {
                    await DestinationFolder.CreateFolderAsync(newName);
                    
                    DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath + @"\" + newName);
                    foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
                    {
                        await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
                    }
                    foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
                    {
                        CloneDirectoryAsync(folderinSourceDir.Path, DestinationFolder.Path, folderinSourceDir.Name);
                    }
                }
                else
                {
                    goto tryagain;
                }
            } 
        }

        public void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            AddItem.CreateFile(tabInstance, "Folder");
        }

        public void NewTextDocument_Click(object sender, RoutedEventArgs e)
        {
            AddItem.CreateFile(tabInstance, "Text Document");
        }

        public void NewBitmapImage_Click(object sender, RoutedEventArgs e)
        {
            AddItem.CreateFile(tabInstance, "Bitmap Image");
        }

        public void SelectAllItems()
        {
            if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.selectedTabInstance;
                foreach (ListedItem li in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.ItemsSource)
                {
                    if (!(CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Contains(li))
                    {
                        (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Add(li);
                    }
                }
            }
            else if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectAll();
            }
        }

        public void ClearAllItems()
        {
            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.selectedTabInstance;
                (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Clear();
            }
            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (tabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems.Clear();
            }
        }
    }
}