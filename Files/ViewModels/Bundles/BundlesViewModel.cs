﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Files.Helpers;
using Files.SettingsInterfaces;
using Windows.UI.Xaml.Input;
using Windows.System;
using Files.Dialogs;
using Files.ViewModels.Dialogs;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Files.ViewModels.Bundles
{
    /// <summary>
    /// Bundles list View Model
    /// </summary>
    public class BundlesViewModel : ObservableObject, IDisposable
    {
        #region Singleton

        private IBundlesSettings BundlesSettings => App.BundlesSettings;

        #endregion

        #region Private Members

        private IShellPage associatedInstance;

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of all bundles
        /// </summary>
        public ObservableCollection<BundleContainerViewModel> Items { get; set; } = new ObservableCollection<BundleContainerViewModel>();

        private string bundleNameTextInput = string.Empty;
        public string BundleNameTextInput
        {
            get => bundleNameTextInput;
            set => SetProperty(ref bundleNameTextInput, value);
        }

        private string addBundleErrorText = string.Empty;
        public string AddBundleErrorText
        {
            get => addBundleErrorText;
            set => SetProperty(ref addBundleErrorText, value);
        }

        public bool noBundlesAddItemLoad = false;
        public bool NoBundlesAddItemLoad
        {
            get => noBundlesAddItemLoad;
            set => SetProperty(ref noBundlesAddItemLoad, value);
        }

        #endregion

        #region Commands

        public ICommand InputTextKeyDownCommand { get; set; }

        public ICommand AddBundleCommand { get; set; }

        public ICommand ImportBundlesCommand { get; set; }

        public ICommand ExportBundlesCommand { get; set; }

        #endregion

        #region Constructor

        public BundlesViewModel()
        {
            // Create commands
            InputTextKeyDownCommand = new RelayCommand<KeyRoutedEventArgs>(InputTextKeyDown);
            AddBundleCommand = new RelayCommand(AddBundle);
            ImportBundlesCommand = new RelayCommand(ImportBundles);
            ExportBundlesCommand = new RelayCommand(ExportBundles);
        }

        #endregion

        #region Command Implementation

        private void InputTextKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                AddBundle();
                e.Handled = true;
            }
        }

        private void AddBundle()
        {
            if (!CanAddBundle(BundleNameTextInput))
            {
                return;
            }

            string savedBundleNameTextInput = BundleNameTextInput;
            BundleNameTextInput = string.Empty;

            if (BundlesSettings.SavedBundles == null || (BundlesSettings.SavedBundles?.ContainsKey(savedBundleNameTextInput) ?? false)) // Init
            {
                BundlesSettings.SavedBundles = new Dictionary<string, List<string>>()
                {
                    { savedBundleNameTextInput, new List<string>() { null } }
                };
            }

            Items.Add(new BundleContainerViewModel(associatedInstance)
            {
                BundleName = savedBundleNameTextInput,
                BundleRenameText = savedBundleNameTextInput,
                NotifyItemRemoved = NotifyItemRemovedHandle,
            });
            NoBundlesAddItemLoad = false;

            // Save bundles
            Save();
        }

        private async void ImportBundles()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                DisplayControl = new TextBox()
                {
                    PlaceholderText = "Import file location..."
                },
                TitleText = "Select location to import from",
                SubtitleText = "Select location to import from",
                PrimaryButtonText = "Ok",
                SecondaryButtonText = "Cancel",
                PrimaryButtonAction = async (vm, e) => 
                {
                    try
                    {
                        string filePath = (vm.DisplayControl as TextBox).Text;

                        if (!await StorageItemHelpers.Exists(filePath))
                        {
                            e.Cancel = true;
                        }
                        else
                        {
                            string data = NativeFileOperationsHelper.ReadStringFromFile(filePath);
                            BundlesSettings.ImportSettings(JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data));
                            await Load(); // Update the collection
                        }
                    }
                    catch { }
                },
                SecondaryButtonAction = (vm, e) => { /* Do Nothing */ }
            });
            await dialog.ShowAsync();
        }

        private async void ExportBundles()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                DisplayControl = new TextBox()
                {
                    PlaceholderText = "Export file location..."
                },
                TitleText = "Select location to export to",
                SubtitleText = "Select location to export to",
                PrimaryButtonText = "Ok",
                SecondaryButtonText = "Cancel",
                PrimaryButtonAction = async (vm, e) =>
                {
                    try
                    {
                        string filePath = (vm.DisplayControl as TextBox).Text;

                        if (!await StorageItemHelpers.Exists(filePath))
                        {
                            e.Cancel = true;
                        }
                        else
                        {
                            NativeFileOperationsHelper.WriteStringToFile(filePath, (string)BundlesSettings.ExportSettings());
                        }
                    }
                    catch { }
                },
                SecondaryButtonAction = (vm, e) => { /* Do Nothing */ }
            });
            await dialog.ShowAsync();
        }

        #endregion

        #region Handlers

        /// <summary>
        /// This function gets called when an item is removed to update the collection
        /// </summary>
        /// <param name="item"></param>
        private void NotifyItemRemovedHandle(BundleContainerViewModel item)
        {
            Items.Remove(item);
            item?.Dispose();

            if (Items.Count == 0)
            {
                NoBundlesAddItemLoad = true;
            }
        }

        /// <summary>
        /// This function gets called when an item is renamed to update the collection
        /// </summary>
        /// <param name="item"></param>
        private void NotifyBundleItemRemovedHandle(BundleItemViewModel item)
        {
            foreach (var bundle in Items)
            {
                if (bundle.BundleName == item.OriginBundleName)
                {
                    bundle.Contents.Remove(item);
                    item?.Dispose();

                    if (bundle.Contents.Count == 0)
                    {
                        bundle.NoBundleContentsTextVisibility = Visibility.Visible;
                    }
                }
            }
        }

        #endregion

        #region Public Helpers

        public void Save()
        {
            if (BundlesSettings.SavedBundles != null)
            {
                Dictionary<string, List<string>> bundles = new Dictionary<string, List<string>>();

                // For every bundle in items bundle collection:
                foreach (var bundle in Items)
                {
                    List<string> bundleItems = new List<string>();

                    // For every bundleItem in current bundle
                    foreach (var bundleItem in bundle.Contents)
                    {
                        if (bundleItem != null)
                        {
                            bundleItems.Add(bundleItem.Path);
                        }
                    }

                    bundles.Add(bundle.BundleName, bundleItems);
                }

                BundlesSettings.SavedBundles = bundles; // Calls Set()
            }
        }

        public async Task Load()
        {
            if (BundlesSettings.SavedBundles != null)
            {
                Items.Clear();

                // For every bundle in saved bundle collection:
                foreach (var bundle in BundlesSettings.SavedBundles)
                {
                    List<BundleItemViewModel> bundleItems = new List<BundleItemViewModel>();

                    // For every bundleItem in current bundle
                    foreach (var bundleItem in bundle.Value)
                    {
                        if (bundleItems.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle)
                        {
                            if (bundleItem != null)
                            {
                                bundleItems.Add(new BundleItemViewModel(associatedInstance, bundleItem, await StorageItemHelpers.GetTypeFromPath(bundleItem, associatedInstance))
                                {
                                    OriginBundleName = bundle.Key,
                                    NotifyItemRemoved = NotifyBundleItemRemovedHandle
                                });
                            }
                        }
                    }

                    // Fill current bundle with collected bundle items
                    Items.Add(new BundleContainerViewModel(associatedInstance)
                    {
                        BundleName = bundle.Key,
                        BundleRenameText = bundle.Key,
                        NotifyItemRemoved = NotifyItemRemovedHandle,
                    }.SetBundleItems(bundleItems));
                }

                if (Items.Count == 0)
                {
                    NoBundlesAddItemLoad = true;
                }
                else
                {
                    NoBundlesAddItemLoad = false;
                }
            }
            else // Null, therefore no items :)
            {
                NoBundlesAddItemLoad = true;
            }
        }

        public void Initialize(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
        }

        public bool CanAddBundle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                AddBundleErrorText = "Input field cannot be empty!";
                return false;
            }

            if (!Items.Any((item) => item.BundleName == name))
            {
                AddBundleErrorText = string.Empty;
                return true;
            }
            else
            {
                AddBundleErrorText = "Bundle with the same name already exists!";
                return false;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            foreach (var item in Items)
            {
                item.NotifyItemRemoved -= NotifyItemRemovedHandle;
                item?.Dispose();
            }

            associatedInstance = null;
            Items = null;
        }

        #endregion
    }
}
