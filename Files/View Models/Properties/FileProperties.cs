﻿using ByteSizeLib;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.View_Models.Properties
{
    internal class FileProperties : BaseProperties
    {
        private ProgressBar ProgressBar;

        public ListedItem Item { get; }

        public FileProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, ProgressBar progressBar, ListedItem item)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            ProgressBar = progressBar;
            Item = item;

            GetBaseProperties();
        }

        public override void GetBaseProperties()
        {
            if (Item != null)
            {
                ViewModel.ItemName = Item.ItemName;
                ViewModel.ItemType = Item.ItemType;
                ViewModel.ItemPath = Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath;
                ViewModel.ItemModifiedTimestamp = Item.ItemDateModified;
                ViewModel.FileIconSource = Item.FileImage;
                ViewModel.LoadFolderGlyph = Item.LoadFolderGlyph;
                ViewModel.LoadUnknownTypeGlyph = Item.LoadUnknownTypeGlyph;
                ViewModel.LoadFileIcon = Item.LoadFileIcon;
            }
        }

        public override async void GetSpecialProperties()
        {
            if (Item.IsShortcutItem)
            {
                var shortcutItem = (ShortcutItem)Item;
                ViewModel.SelectedTabIndex = 1;
                ViewModel.ShortcutTabVisibility = Visibility.Visible;
                ViewModel.ShortcutItemType = Item.IsLinkItem ? "Web link" : "File";
                ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
                ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
                ViewModel.ShortcutItemWorkingDirVisibility = Item.IsLinkItem ? Visibility.Collapsed : Visibility.Visible;
                ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
                ViewModel.ShortcutItemArgumentsVisibility = Item.IsLinkItem ? Visibility.Collapsed : Visibility.Visible;
                ViewModel.ShortcutItemOpenLinkCommand = new GalaSoft.MvvmLight.Command.RelayCommand(async () =>
                {
                    if (Item.IsLinkItem)
                    {
                        var tmpItem = (ShortcutItem)Item;
                        await Interacts.Interaction.InvokeWin32Component(tmpItem.TargetPath, tmpItem.Arguments, tmpItem.RunAsAdmin, tmpItem.WorkingDirectory);
                    }
                    else
                    {
                        var folderUri = new Uri("files-uwp:" + "?folder=" + Path.GetDirectoryName(((ShortcutItem)Item).TargetPath));
                        await Windows.System.Launcher.LaunchUriAsync(folderUri);
                    }
                }, () =>
                {
                    return !string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath);
                }, false);
                ViewModel.ShortcutItemUpdateShortcutCommand = new GalaSoft.MvvmLight.Command.RelayCommand(async () =>
                {
                    var tmpItem = (ShortcutItem)Item;
                    if (App.Connection != null)
                    {
                        var value = new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "UpdateLink" },
                            { "filepath", Item.ItemPath },
                            { "targetpath", ViewModel.ShortcutItemPath },
                            { "arguments", ViewModel.ShortcutItemArguments },
                            { "workingdir", ViewModel.ShortcutItemWorkingDir },
                            { "runasadmin", tmpItem.RunAsAdmin },
                        };
                        await App.Connection.SendMessageAsync(value);
                    }
                }, () =>
                {
                    return !string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath);
                }, false);
                if (Item.IsLinkItem)
                {
                    ViewModel.LoadLinkIcon = true;
                    // Can't show any other property
                    return;
                }
                if (string.IsNullOrWhiteSpace(shortcutItem.TargetPath))
                {
                    // Can't show any other property
                    return;
                }
            }

            var file = await ItemViewModel.GetFileFromPathAsync((Item as ShortcutItem)?.TargetPath ?? Item.ItemPath);
            ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(file.DateCreated);

            GetOtherProperties(file.Properties);

            ViewModel.ItemSizeVisibility = Visibility.Visible;
            ViewModel.ItemSize = ByteSize.FromBytes(Item.FileSizeBytes).ToBinaryString().ConvertSizeAbbreviation()
                + " (" + ByteSize.FromBytes(Item.FileSizeBytes).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";

            using (var Thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 80, ThumbnailOptions.UseCurrentScale))
            {
                BitmapImage icon = new BitmapImage();
                if (Thumbnail != null)
                {
                    ViewModel.FileIconSource = icon;
                    await icon.SetSourceAsync(Thumbnail);
                    ViewModel.LoadUnknownTypeGlyph = false;
                    ViewModel.LoadFileIcon = true;
                }
            }

            // Get file MD5 hash
            var hashAlgTypeName = HashAlgorithmNames.Md5;
            ViewModel.ItemMD5HashProgressVisibility = Visibility.Visible;
            ViewModel.ItemMD5HashVisibility = Visibility.Visible;
            try
            {
                ViewModel.ItemMD5Hash = await App.CurrentInstance.InteractionOperations
                    .GetHashForFile(Item, hashAlgTypeName, TokenSource.Token, ProgressBar);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                ViewModel.ItemMD5HashCalcError = true;
            }
        }
    }
}