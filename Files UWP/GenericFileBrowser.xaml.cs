﻿using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Files.Filesystem;
using Files.Navigation;
using Files.Interacts;
using Windows.System;
using Windows.UI.Xaml.Input;
using Windows.UI.Popups;
using System.IO;

namespace Files
{

    public sealed partial class GenericFileBrowser : Page
    {
        public TextBlock textBlock;
        public static DataGrid data;
        public static MenuFlyout context;
        public static MenuFlyout HeaderContextMenu;
        public static Page GFBPageName;
        public static ContentDialog collisionBox;
        public static ContentDialog reviewBox;
        public static ContentDialog AddItemBox;
        public static ContentDialog NameBox;
        public static TextBox inputFromRename;
        public static string inputForRename;



        public GenericFileBrowser()
        {
            this.InitializeComponent();
            GFBPageName = GenericItemView;
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            ItemViewModel.PVIS.isVisible = Visibility.Collapsed;
            data = AllView;
            context = RightClickContextMenu;
            HeaderContextMenu = HeaderRightClickMenu;
            Interacts.Interaction.page = this;
            OpenItem.Click += Interaction.OpenItem_Click;
            ShareItem.Click += Interaction.ShareItem_Click;
            DeleteItem.Click += Interaction.DeleteItem_Click;
            RenameItem.Click += Interaction.RenameItem_Click;
            CutItem.Click += Interaction.CutItem_Click;
            CopyItem.Click += Interaction.CopyItem_ClickAsync;
            AllView.RightTapped += Interaction.AllView_RightTapped;
            Back.Click += NavigationActions.Back_Click;
            Forward.Click += NavigationActions.Forward_Click;
            Refresh.Click += NavigationActions.Refresh_Click;
            AddItem.Click += AddItem_ClickAsync;
            AllView.DoubleTapped += Interaction.List_ItemClick;
            Paste.Click += Interaction.PasteItem_ClickAsync;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            AddItemBox = AddDialog;
            NameBox = NameDialog;
            inputFromRename = RenameInput;
        }

        

        private async void AddItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            await AddDialog.ShowAsync();
        }

        private void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                DataPackageView packageView = Clipboard.GetContent();
                if (packageView.Contains(StandardDataFormats.StorageItems))
                {
                    Interacts.Interaction.PS.isEnabled = true;
                }
                else
                {
                    Interacts.Interaction.PS.isEnabled = false;
                }
            }
            catch (Exception)
            {
                Interacts.Interaction.PS.isEnabled = false;
            }

        }

        public static UniversalPath p = new UniversalPath();
        public static UniversalPath P { get { return p; } }
        
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = (string)eventArgs.Parameter;
            ItemViewModel.FilesAndFolders.Clear();
            ItemViewModel.ViewModel = new ItemViewModel(parameters, GFBPageName);
            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
                P.path = "Desktop";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                P.path = "Documents";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                P.path = "Downloads";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                P.path = "Pictures";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
                P.path = "Music";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
                P.path = "OneDrive";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
                P.path = "Videos";
            }
            else
            {
                P.path = parameters;
            }

        }




        private void AllView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            
        }

        private async void AllView_DropAsync(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if(items.Count() == 1)
                {
                    DataPackage data = new DataPackage();
                    foreach(IStorageItem storageItem in items)
                    {
                        var itemPath = storageItem.Path;

                    } 
                }
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ItemViewModel.PVIS.isVisible = Visibility.Collapsed;
        }

        private async void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            var newCellText = (GenericFileBrowser.data.SelectedItem as ListedItem)?.FileName;
            var selectedItem = ItemViewModel.FilesAndFolders[e.Row.GetIndex()];
            if(selectedItem.FileExtension == "Folder")
            {
                StorageFolder FolderToRename = await StorageFolder.GetFolderFromPathAsync(selectedItem.FilePath);
                if(FolderToRename.Name != newCellText)
                {
                    await FolderToRename.RenameAsync(newCellText);
                    AllView.CommitEdit();
                }
                else
                {
                    AllView.CancelEdit();
                }
            }
            else
            {
                StorageFile fileToRename = await StorageFile.GetFileFromPathAsync(selectedItem.FilePath);
                if (fileToRename.Name != newCellText)
                {
                    await fileToRename.RenameAsync(newCellText);
                    AllView.CommitEdit();
                }
                else
                {
                    AllView.CancelEdit();
                }
            }
            //Navigation.NavigationActions.Refresh_Click(null, null);
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            AddDialogFrame.Navigate(typeof(AddItem), new SuppressNavigationTransitionInfo());
        }

        private void GenericItemView_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            data.CommitEdit();
            data.SelectedItems.Clear();
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
        }

        private void NameDialog_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = inputFromRename.Text;
        }

        private void NameDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private async void VisiblePath_TextChanged(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var PathBox = (sender as TextBox);
                var CurrentInput = PathBox.Text;
                if (CurrentInput != ItemViewModel.PUIP.Path)
                {
                    if (ItemViewModel.tokenSource != null)
                    {
                        ItemViewModel.tokenSource.Cancel();
                        ItemViewModel.FilesAndFolders.Clear();
                    }

                    if (CurrentInput == "Home" || CurrentInput == "home")
                    {
                        MainPage.accessibleContentFrame.Navigate(typeof(YourHome));
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Recents";
                    }
                    else if (CurrentInput == "Desktop" || CurrentInput == "desktop")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.DesktopPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Desktop";
                        PathBox.Text = "Desktop";
                    }
                    else if (CurrentInput == "Documents" || CurrentInput == "documents")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.DocumentsPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Documents";
                        PathBox.Text = "Documents";

                    }
                    else if (CurrentInput == "Downloads" || CurrentInput == "downloads")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.DownloadsPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Downloads";
                        PathBox.Text = "Downloads";

                    }
                    else if (CurrentInput == "Pictures" || CurrentInput == "pictures")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), MainPage.PicturesPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Pictures";

                    }
                    else if (CurrentInput == "Music" || CurrentInput == "music")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.MusicPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Music";
                        PathBox.Text = "Music";

                    }
                    else if (CurrentInput == "Videos" || CurrentInput == "videos")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.VideosPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Videos";
                        PathBox.Text = "Videos";

                    }
                    else if (CurrentInput == "OneDrive" || CurrentInput == "Onedrive" || CurrentInput == "onedrive")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.OneDrivePath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search OneDrive";
                        PathBox.Text = "OneDrive";

                    }
                    else
                    {
                        if (CurrentInput.Contains("."))
                        {
                            if (CurrentInput.Contains(".exe") || CurrentInput.Contains(".EXE"))
                            {
                                if (StorageFile.GetFileFromPathAsync(CurrentInput) != null)
                                {
                                    await Interaction.LaunchExe(CurrentInput);
                                    PathBox.Text = ItemViewModel.PUIP.Path;
                                }
                                else
                                {
                                    MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                    await dialog.ShowAsync();
                                }    
                            }
                            else
                            {
                                try
                                {
                                    await StorageFile.GetFileFromPathAsync(CurrentInput);
                                    StorageFile file = await StorageFile.GetFileFromPathAsync(CurrentInput);
                                    var options = new LauncherOptions
                                    {
                                        DisplayApplicationPicker = false

                                    };
                                    await Launcher.LaunchFileAsync(file, options);
                                    PathBox.Text = ItemViewModel.PUIP.Path;
                                }
                                catch (ArgumentException)
                                {
                                    MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                    await dialog.ShowAsync();
                                }
                                catch (FileNotFoundException)
                                {
                                    MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                    await dialog.ShowAsync();
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                await StorageFolder.GetFolderFromPathAsync(CurrentInput);
                                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                                MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
                                MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search";
                            }
                            catch (ArgumentException)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                            catch (FileNotFoundException)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                            
                        }

                    }
                }
            }
            
        }
    }

    public class EmptyFolderTextState : INotifyPropertyChanged
    {


        public Visibility _isVisible;
        public Visibility isVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    NotifyPropertyChanged("isVisible");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }
}