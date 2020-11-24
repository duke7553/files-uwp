﻿using ByteSizeLib;
using Files.Common;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files.Filesystem
{
    public class DriveItem : ObservableObject, INavigationControlItem
    {
        public string Glyph { get; set; }
        public string Path { get; set; }
        public string DeviceID { get; set; }
        public StorageFolder Root { get; set; }
        public NavigationControlItemType ItemType { get; set; } = NavigationControlItemType.Drive;
        public ByteSize MaxSpace { get; set; }
        public ByteSize FreeSpace { get; set; }
        public ByteSize SpaceUsed { get; set; }
        public Visibility ItemVisibility { get; set; } = Visibility.Visible;
        public bool IsRemovable { get; set; }

        private DriveType type;

        public DriveType Type
        {
            get => type;
            set
            {
                type = value;
                SetGlyph(type);
            }
        }

        private string text;

        public string Text
        {
            get => text;
            set => SetProperty(ref text, value);
        }

        private string spaceText;

        public string SpaceText
        {
            get => spaceText;
            set => SetProperty(ref spaceText, value);
        }

        public DriveItem()
        {
            ItemType = NavigationControlItemType.OneDrive;
        }

        public DriveItem(StorageFolder root, string deviceId, DriveType type)
        {
            Text = root.DisplayName;
            Type = type;
            Path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
            DeviceID = deviceId;
            Root = root;
            IsRemovable = (Type == DriveType.Removable || Type == DriveType.CDRom);

            CoreApplication.MainView.ExecuteOnUIThreadAsync(() => UpdatePropertiesAsync());
        }

        public async Task UpdateLabelAsync()
        {
            try
            {
                var properties = await Root.Properties.RetrievePropertiesAsync(new[] { "System.ItemNameDisplay" })
                    .AsTask().WithTimeoutAsync(TimeSpan.FromSeconds(5));
                Text = (string)properties["System.ItemNameDisplay"];
            }
            catch (NullReferenceException)
            {
            }
        }

        public async Task UpdatePropertiesAsync()
        {
            try
            {
                var properties = await Root.Properties.RetrievePropertiesAsync(new[] { "System.FreeSpace", "System.Capacity" })
                    .AsTask().WithTimeoutAsync(TimeSpan.FromSeconds(5));

                MaxSpace = ByteSize.FromBytes((ulong)properties["System.Capacity"]);
                FreeSpace = ByteSize.FromBytes((ulong)properties["System.FreeSpace"]);
                SpaceUsed = MaxSpace - FreeSpace;

                SpaceText = string.Format(
                    "DriveFreeSpaceAndCapacity".GetLocalized(),
                    FreeSpace.ToBinaryString().ConvertSizeAbbreviation(),
                    MaxSpace.ToBinaryString().ConvertSizeAbbreviation());
            }
            catch (NullReferenceException)
            {
                SpaceText = "DriveCapacityUnknown".GetLocalized();
                SpaceUsed = ByteSize.FromBytes(0);
            }
        }

        private void SetGlyph(DriveType type)
        {
            switch (type)
            {
                case DriveType.Fixed:
                    Glyph = "\ueb8b";
                    break;

                case DriveType.Removable:
                    Glyph = "\uec0a";
                    break;

                case DriveType.Network:
                    Glyph = "\ueac2";
                    break;

                case DriveType.Ram:
                    break;

                case DriveType.CDRom:
                    Glyph = "\uec39";
                    break;

                case DriveType.Unknown:
                    break;

                case DriveType.NoRootDirectory:
                    break;

                case DriveType.VirtualDrive:
                    Glyph = "\ue9b7";
                    break;

                case DriveType.FloppyDisk:
                    Glyph = "\ueb4a";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    public enum DriveType
    {
        Fixed,
        Removable,
        Network,
        Ram,
        CDRom,
        FloppyDisk,
        Unknown,
        NoRootDirectory,
        VirtualDrive
    }
}