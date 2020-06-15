﻿using Files.Filesystem;
using GalaSoft.MvvmLight;
using System;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace Files.View_Models
{
    public class SelectedItemsPropertiesViewModel : ViewModelBase
    {
        public string ItemName
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemName;
                }
                else
                {
                    return null;
                }
            }
        }

        public string ItemType
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemType;
                }
                else
                {
                    return null;
                }
            }
        }

        public string ItemPath
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemPath;
                }
                else
                {
                    return null;
                }
            }
        }

        private String _ItemsSize;

        public String ItemsSize
        {
            get => _ItemsSize;
            set => Set(ref _ItemsSize, value);
        }

        private String _SelectedItemCount;

        public String SelectedItemCount
        {
            get => _SelectedItemCount;
            set => Set(ref _SelectedItemCount, value);
        }

        private bool _IsItemSelected;

        public bool IsItemSelected
        {
            get => _IsItemSelected;
            set => Set(ref _IsItemSelected, value);
        }

        public string ItemCreatedTimestamp
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    DateTimeOffset dateCreated;
                    if (App.CurrentInstance.ContentPage.SelectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        dateCreated = StorageFolder.GetFolderFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.ItemPath).GetResults().DateCreated;
                    }
                    else if (App.CurrentInstance.ContentPage.SelectedItem.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        dateCreated = StorageFile.GetFileFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.ItemPath).GetResults().DateCreated;
                    }
                    return ListedItem.GetFriendlyDate(dateCreated);
                }
                else
                {
                    return null;
                }
            }
        }

        public string ItemModifiedTimestamp
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemDateModified;
                }
                else
                {
                    return null;
                }
            }
        }

        public ImageSource FileIconSource
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.FileImage;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool LoadFolderGlyph
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadFolderGlyph;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool LoadUnknownTypeGlyph
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadUnknownTypeGlyph;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool LoadFileIcon
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadFileIcon;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}