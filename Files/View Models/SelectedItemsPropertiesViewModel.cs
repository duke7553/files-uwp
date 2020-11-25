using ByteSizeLib;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.Services.Maps;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.View_Models
{
    public class SelectedItemsPropertiesViewModel : ObservableObject
    {
        private bool loadFolderGlyph;

        public bool LoadFolderGlyph
        {
            get => loadFolderGlyph;
            set => SetProperty(ref loadFolderGlyph, value);
        }

        private bool loadUnknownTypeGlyph;

        public bool LoadUnknownTypeGlyph
        {
            get => loadUnknownTypeGlyph;
            set => SetProperty(ref loadUnknownTypeGlyph, value);
        }

        private bool loadCombinedItemsGlyph;

        public bool LoadCombinedItemsGlyph
        {
            get => loadCombinedItemsGlyph;
            set => SetProperty(ref loadCombinedItemsGlyph, value);
        }

        private string driveItemGlyphSource;

        public string DriveItemGlyphSource
        {
            get => driveItemGlyphSource;
            set => SetProperty(ref driveItemGlyphSource, value);
        }

        private bool loadDriveItemGlyph;

        public bool LoadDriveItemGlyph
        {
            get => loadDriveItemGlyph;
            set => SetProperty(ref loadDriveItemGlyph, value);
        }

        private bool loadFileIcon;

        public bool LoadFileIcon
        {
            get => loadFileIcon;
            set => SetProperty(ref loadFileIcon, value);
        }

        private ImageSource fileIconSource;

        public ImageSource FileIconSource
        {
            get => fileIconSource;
            set => SetProperty(ref fileIconSource, value);
        }

        private string itemName;

        public string ItemName
        {
            get => itemName;
            set
            {
                ItemNameVisibility = Visibility.Visible;
                SetProperty(ref itemName, value);
            }
        }

        private string originalItemName;

        public string OriginalItemName
        {
            get => originalItemName;
            set
            {
                ItemNameVisibility = Visibility.Visible;
                SetProperty(ref originalItemName, value);
            }
        }

        private Visibility itemNameVisibility = Visibility.Collapsed;

        public Visibility ItemNameVisibility
        {
            get => itemNameVisibility;
            set => SetProperty(ref itemNameVisibility, value);
        }

        private string itemType;

        public string ItemType
        {
            get => itemType;
            set
            {
                ItemTypeVisibility = Visibility.Visible;
                SetProperty(ref itemType, value);
            }
        }

        private Visibility itemTypeVisibility = Visibility.Collapsed;

        public Visibility ItemTypeVisibility
        {
            get => itemTypeVisibility;
            set => SetProperty(ref itemTypeVisibility, value);
        }

        private string driveFileSystem;

        public string DriveFileSystem
        {
            get => driveFileSystem;
            set
            {
                DriveFileSystemVisibility = Visibility.Visible;
                SetProperty(ref driveFileSystem, value);
            }
        }

        private Visibility driveFileSystemVisibility = Visibility.Collapsed;

        public Visibility DriveFileSystemVisibility
        {
            get => driveFileSystemVisibility;
            set => SetProperty(ref driveFileSystemVisibility, value);
        }

        private string itemPath;

        public string ItemPath
        {
            get => itemPath;
            set
            {
                ItemPathVisibility = Visibility.Visible;
                SetProperty(ref itemPath, value);
            }
        }

        private Visibility itemPathVisibility = Visibility.Collapsed;

        public Visibility ItemPathVisibility
        {
            get => itemPathVisibility;
            set => SetProperty(ref itemPathVisibility, value);
        }

        private string itemSize;

        public string ItemSize
        {
            get => itemSize;
            set => SetProperty(ref itemSize, value);
        }

        private Visibility itemSizeVisibility = Visibility.Collapsed;

        public Visibility ItemSizeVisibility
        {
            get => itemSizeVisibility;
            set => SetProperty(ref itemSizeVisibility, value);
        }

        private long itemSizeBytes;

        public long ItemSizeBytes
        {
            get => itemSizeBytes;
            set => SetProperty(ref itemSizeBytes, value);
        }

        private Visibility itemSizeProgressVisibility = Visibility.Collapsed;

        public Visibility ItemSizeProgressVisibility
        {
            get => itemSizeProgressVisibility;
            set => SetProperty(ref itemSizeProgressVisibility, value);
        }

        public string itemMD5Hash;

        public string ItemMD5Hash
        {
            get => itemMD5Hash;
            set
            {
                if (!string.IsNullOrEmpty(value) && value != itemMD5Hash)
                {
                    SetProperty(ref itemMD5Hash, value);
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
        }

        private bool itemMD5HashCalcError;

        public bool ItemMD5HashCalcError
        {
            get => itemMD5HashCalcError;
            set => SetProperty(ref itemMD5HashCalcError, value);
        }

        public Visibility itemMD5HashVisibility = Visibility.Collapsed;

        public Visibility ItemMD5HashVisibility
        {
            get => itemMD5HashVisibility;
            set => SetProperty(ref itemMD5HashVisibility, value);
        }

        public Visibility itemMD5HashProgressVisibiity = Visibility.Collapsed;

        public Visibility ItemMD5HashProgressVisibility
        {
            get => itemMD5HashProgressVisibiity;
            set => SetProperty(ref itemMD5HashProgressVisibiity, value);
        }

        public int foldersCount;

        public int FoldersCount
        {
            get => foldersCount;
            set => SetProperty(ref foldersCount, value);
        }

        public int filesCount;

        public int FilesCount
        {
            get => filesCount;
            set => SetProperty(ref filesCount, value);
        }

        public string filesAndFoldersCountString;

        public string FilesAndFoldersCountString
        {
            get => filesAndFoldersCountString;
            set
            {
                if (FilesAndFoldersCountVisibility == Visibility.Collapsed)
                {
                    FilesAndFoldersCountVisibility = Visibility.Visible;
                }
                SetProperty(ref filesAndFoldersCountString, value);
            }
        }

        public Visibility filesAndFoldersCountVisibility = Visibility.Collapsed;

        public Visibility FilesAndFoldersCountVisibility
        {
            get => filesAndFoldersCountVisibility;
            set => SetProperty(ref filesAndFoldersCountVisibility, value);
        }

        private ulong driveUsedSpaceValue;

        public ulong DriveUsedSpaceValue
        {
            get => driveUsedSpaceValue;
            set
            {
                SetProperty(ref driveUsedSpaceValue, value);
                DriveUsedSpace = ByteSize.FromBytes(DriveUsedSpaceValue).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSize.FromBytes(DriveUsedSpaceValue).Bytes.ToString("#,##0") + " " + "ItemSizeBytes".GetLocalized() + ")";
                DriveUsedSpaceDoubleValue = Convert.ToDouble(DriveUsedSpaceValue);
            }
        }

        private string driveUsedSpace;

        public string DriveUsedSpace
        {
            get => driveUsedSpace;
            set
            {
                DriveUsedSpaceVisibiity = Visibility.Visible;
                SetProperty(ref driveUsedSpace, value);
            }
        }

        public Visibility driveUsedSpaceVisibiity = Visibility.Collapsed;

        public Visibility DriveUsedSpaceVisibiity
        {
            get => driveUsedSpaceVisibiity;
            set => SetProperty(ref driveUsedSpaceVisibiity, value);
        }

        private ulong driveFreeSpaceValue;

        public ulong DriveFreeSpaceValue
        {
            get => driveFreeSpaceValue;
            set
            {
                SetProperty(ref driveFreeSpaceValue, value);
                DriveFreeSpace = ByteSize.FromBytes(DriveFreeSpaceValue).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSize.FromBytes(DriveFreeSpaceValue).Bytes.ToString("#,##0") + " " + "ItemSizeBytes".GetLocalized() + ")";
            }
        }

        private string driveFreeSpace;

        public string DriveFreeSpace
        {
            get => driveFreeSpace;
            set
            {
                DriveFreeSpaceVisibiity = Visibility.Visible;
                SetProperty(ref driveFreeSpace, value);
            }
        }

        public Visibility driveFreeSpaceVisibiity = Visibility.Collapsed;

        public Visibility DriveFreeSpaceVisibiity
        {
            get => driveFreeSpaceVisibiity;
            set => SetProperty(ref driveFreeSpaceVisibiity, value);
        }

        private string itemCreatedTimestamp;

        public string ItemCreatedTimestamp
        {
            get => itemCreatedTimestamp;
            set
            {
                ItemCreatedTimestampVisibiity = Visibility.Visible;
                SetProperty(ref itemCreatedTimestamp, value);
            }
        }

        public Visibility itemCreatedTimestampVisibiity = Visibility.Collapsed;

        public Visibility ItemCreatedTimestampVisibiity
        {
            get => itemCreatedTimestampVisibiity;
            set => SetProperty(ref itemCreatedTimestampVisibiity, value);
        }

        private string itemModifiedTimestamp;

        public string ItemModifiedTimestamp
        {
            get => itemModifiedTimestamp;
            set
            {
                ItemModifiedTimestampVisibility = Visibility.Visible;
                SetProperty(ref itemModifiedTimestamp, value);
            }
        }

        private Visibility itemModifiedTimestampVisibility = Visibility.Collapsed;

        public Visibility ItemModifiedTimestampVisibility
        {
            get => itemModifiedTimestampVisibility;
            set => SetProperty(ref itemModifiedTimestampVisibility, value);
        }

        public string itemAccessedTimestamp;

        public string ItemAccessedTimestamp
        {
            get => itemAccessedTimestamp;
            set
            {
                ItemAccessedTimestampVisibility = Visibility.Visible;
                SetProperty(ref itemAccessedTimestamp, value);
            }
        }

        private Visibility itemAccessedTimestampVisibility = Visibility.Collapsed;

        public Visibility ItemAccessedTimestampVisibility
        {
            get => itemAccessedTimestampVisibility;
            set => SetProperty(ref itemAccessedTimestampVisibility, value);
        }

        public string itemFileOwner;

        public string ItemFileOwner
        {
            get => itemFileOwner;
            set
            {
                ItemFileOwnerVisibility = Visibility.Visible;
                SetProperty(ref itemFileOwner, value);
            }
        }

        private Visibility itemFileOwnerVisibility = Visibility.Collapsed;

        public Visibility ItemFileOwnerVisibility
        {
            get => itemFileOwnerVisibility;
            set => SetProperty(ref itemFileOwnerVisibility, value);
        }

        private Visibility lastSeparatorVisibility = Visibility.Visible;

        public Visibility LastSeparatorVisibility
        {
            get => lastSeparatorVisibility;
            set => SetProperty(ref lastSeparatorVisibility, value);
        }

        private ulong driveCapacityValue;

        public ulong DriveCapacityValue
        {
            get => driveCapacityValue;
            set
            {
                SetProperty(ref driveCapacityValue, value);
                DriveCapacity = ByteSize.FromBytes(DriveCapacityValue).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSize.FromBytes(DriveCapacityValue).Bytes.ToString("#,##0") + " " + "ItemSizeBytes".GetLocalized() + ")";
                DriveCapacityDoubleValue = Convert.ToDouble(DriveCapacityValue);
            }
        }

        private string driveCapacity;

        public string DriveCapacity
        {
            get => driveCapacity;
            set
            {
                DriveCapacityVisibiity = Visibility.Visible;
                SetProperty(ref driveCapacity, value);
            }
        }

        public Visibility driveCapacityVisibiity = Visibility.Collapsed;

        public Visibility DriveCapacityVisibiity
        {
            get => driveCapacityVisibiity;
            set => SetProperty(ref driveCapacityVisibiity, value);
        }

        private double driveCapacityDoubleValue;

        public double DriveCapacityDoubleValue
        {
            get => driveCapacityDoubleValue;
            set => SetProperty(ref driveCapacityDoubleValue, value);
        }

        private double driveUsedSpaceDoubleValue;

        public double DriveUsedSpaceDoubleValue
        {
            get => driveUsedSpaceDoubleValue;
            set => SetProperty(ref driveUsedSpaceDoubleValue, value);
        }

        private Visibility itemAttributesVisibility = Visibility.Visible;

        public Visibility ItemAttributesVisibility
        {
            get => itemAttributesVisibility;
            set => SetProperty(ref itemAttributesVisibility, value);
        }

        private string _SelectedItemsCountString;

        public string SelectedItemsCountString
        {
            get => _SelectedItemsCountString;
            set => SetProperty(ref _SelectedItemsCountString, value);
        }

        private int _SelectedItemsCount;

        public int SelectedItemsCount
        {
            get => _SelectedItemsCount;
            set => SetProperty(ref _SelectedItemsCount, value);
        }

        private bool isItemSelected;

        public bool IsItemSelected
        {
            get => isItemSelected;
            set => SetProperty(ref isItemSelected, value);
        }

        private BaseLayout contentPage = null;

        public SelectedItemsPropertiesViewModel(BaseLayout contentPageParam)
        {
            contentPage = contentPageParam;
        }

        private bool isSelectedItemImage = false;

        public bool IsSelectedItemImage
        {
            get => isSelectedItemImage;
            set => SetProperty(ref isSelectedItemImage, value);
        }

        private bool isSelectedItemShortcut = false;

        public bool IsSelectedItemShortcut
        {
            get => isSelectedItemShortcut;
            set => SetProperty(ref isSelectedItemShortcut, value);
        }

        public async void CheckFileExtension()
        {
            // Set properties to false
            IsSelectedItemImage = false;
            IsSelectedItemShortcut = false;

            //check if the selected item is an image file
            string ItemExtension = await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => contentPage.SelectedItem.FileExtension);
            if (!string.IsNullOrEmpty(ItemExtension) && SelectedItemsCount == 1)
            {
                if (ItemExtension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    // Since item is an image, set the IsSelectedItemImage property to true
                    IsSelectedItemImage = true;
                }
                else if (ItemExtension.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    // The selected item is a shortcut, so set the IsSelectedItemShortcut property to true
                    IsSelectedItemShortcut = true;
                }
            }
        }

        private string shortcutItemType;

        public string ShortcutItemType
        {
            get => shortcutItemType;
            set => SetProperty(ref shortcutItemType, value);
        }

        private string shortcutItemPath;

        public string ShortcutItemPath
        {
            get => shortcutItemPath;
            set => SetProperty(ref shortcutItemPath, value);
        }

        private string shortcutItemWorkingDir;

        public string ShortcutItemWorkingDir
        {
            get => shortcutItemWorkingDir;
            set => SetProperty(ref shortcutItemWorkingDir, value);
        }

        private Visibility shortcutItemWorkingDirVisibility = Visibility.Collapsed;

        public Visibility ShortcutItemWorkingDirVisibility
        {
            get => shortcutItemWorkingDirVisibility;
            set => SetProperty(ref shortcutItemWorkingDirVisibility, value);
        }

        private string shortcutItemArguments;

        public string ShortcutItemArguments
        {
            get => shortcutItemArguments;
            set
            {
                SetProperty(ref shortcutItemArguments, value);
            }
        }

        private Visibility shortcutItemArgumentsVisibility = Visibility.Collapsed;

        public Visibility ShortcutItemArgumentsVisibility
        {
            get => shortcutItemArgumentsVisibility;
            set => SetProperty(ref shortcutItemArgumentsVisibility, value);
        }

        private bool loadLinkIcon;

        public bool LoadLinkIcon
        {
            get => loadLinkIcon;
            set => SetProperty(ref loadLinkIcon, value);
        }

        private RelayCommand shortcutItemOpenLinkCommand;

        public RelayCommand ShortcutItemOpenLinkCommand
        {
            get => shortcutItemOpenLinkCommand;
            set
            {
                SetProperty(ref shortcutItemOpenLinkCommand, value);
            }
        }

        public bool ContainsFilesOrFolders { get; set; }

        public Uri FolderIconSource
        {
            get
            {
                return ContainsFilesOrFolders ? new Uri("ms-appx:///Assets/FolderIcon2.svg") : new Uri("ms-appx:///Assets/FolderIcon.svg");
            }
        }

        private DateTimeOffset dateTaken;

        public DateTimeOffset DateTaken
        {
            get => dateTaken;
            set => SetProperty(ref dateTaken, value);
        }

        private double? longitude;

        public double? Longitude
        {
            get => longitude;
            set => SetProperty(ref longitude, value);
        }

        private double? latitude;

        public double? Latitude
        {
            get => latitude;
            set => SetProperty(ref latitude, value);
        }

        private int rating;

        public int Rating
        {
            get => rating;
            set => SetProperty(ref rating, value);
        }

        private MapLocation geopoint;

        public MapLocation Geopoint
        {
            get => geopoint;
            set => SetProperty(ref geopoint, value);
        }

        private string geopointString;

        public string GeopointString
        {
            get => geopointString;
            set => SetProperty(ref geopointString, value);
        }

        private string cameraNameString;

        public string CameraNameString
        {
            get => cameraNameString;
            set => SetProperty(ref cameraNameString, value);
        }

        private string shotString;

        public string ShotString
        {
            get => shotString;
            set => SetProperty(ref shotString, value);
        }

        private IDictionary<string, object> systemFileProperties_RO;

        public IDictionary<string, object> SystemFileProperties_RO
        {
            get => systemFileProperties_RO;
            set => SetProperty(ref systemFileProperties_RO, value);
        }

        private IDictionary<string, object> systemFileProperties_RW;

        public IDictionary<string, object> SystemFileProperties_RW
        {
            get => systemFileProperties_RW;
            set => SetProperty(ref systemFileProperties_RW, value);
        }

        private Visibility detailsSectionVisibility_Image;

        public Visibility DetailsSectionVisibility_Image
        {
            get => detailsSectionVisibility_Image;
            set => SetProperty(ref detailsSectionVisibility_Image, value);
        }

        private Visibility detailsSectionVisibility_GPS;

        public Visibility DetailsSectionVisibility_GPS
        {
            get => detailsSectionVisibility_GPS;
            set => SetProperty(ref detailsSectionVisibility_GPS, value);
        }

        private Visibility detailsSectionVisibility_Photo;

        public Visibility DetailsSectionVisibility_Photo
        {
            get => detailsSectionVisibility_Photo;
            set => SetProperty(ref detailsSectionVisibility_Photo, value);
        }

        private Visibility detailsSectionVisibility_Audio;

        public Visibility DetailsSectionVisibility_Audio
        {
            get => detailsSectionVisibility_Audio;
            set => SetProperty(ref detailsSectionVisibility_Audio, value);
        }

        private Visibility detailsSectionVisibility_Music;

        public Visibility DetailsSectionVisibility_Music
        {
            get => detailsSectionVisibility_Music;
            set => SetProperty(ref detailsSectionVisibility_Music, value);
        }

        private Visibility detailsSectionVisibility_Media;

        public Visibility DetailsSectionVisibility_Media
        {
            get => detailsSectionVisibility_Media;
            set => SetProperty(ref detailsSectionVisibility_Media, value);
        }

        private bool isReadOnly;

        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                IsReadOnlyEnabled = true;
                SetProperty(ref isReadOnly, value);
            }
        }

        private bool isReadOnlyEnabled;

        public bool IsReadOnlyEnabled
        {
            get => isReadOnlyEnabled;
            set => SetProperty(ref isReadOnlyEnabled, value);
        }

        private bool isHidden;

        public bool IsHidden
        {
            get => isHidden;
            set => SetProperty(ref isHidden, value);
        }
    }
}