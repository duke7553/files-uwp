﻿using Files.Filesystem;
using Files.UserControls.Preview;
using Files.View_Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Files.App;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class DetailsPane : UserControl
    {
        private DependencyProperty selectedItemsProperty = DependencyProperty.Register("SelectedItems", typeof(List<ListedItem>), typeof(DetailsPane), null);
        public List<ListedItem> SelectedItems
        {
            get => (List<ListedItem>)GetValue(selectedItemsProperty);
            set
            {
                PreviewGrid.Children.Clear();
                PreviewNotAvaliableText.Visibility = Visibility.Collapsed;
                SetValue(selectedItemsProperty, value);
                if (value.Count == 1)
                {
                    foreach (var extension in AppData.FilePreviewExtensionManager.Extensions)
                    {
                        if(extension.FileExtensions.Contains(value[0].FileExtension))
                        {
                            UpdatePreviewControl(value[0], extension);
                            return;
                        }
                    }
                    var control = PreviewBase.GetControlFromExtension(value[0].FileExtension);
                    if (control != null)
                    {
                        control.SetFile(value[0].ItemPath);
                        control.HorizontalAlignment = HorizontalAlignment.Stretch;
                        control.VerticalAlignment = VerticalAlignment.Stretch;
                        PreviewGrid.Children.Add(control);
                        return;
                    }
                }

                PreviewNotAvaliableText.Visibility = Visibility.Visible;
            }
        }

        List<Package> OptionalPackages = new List<Package>();

        private bool isMarkDown = false;

        public DetailsPane()
        {
            this.InitializeComponent();
        }

        public async void UpdatePreviewControl(ListedItem item, Helpers.Extension extension)
        {
            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);

            var buffer = await FileIO.ReadBufferAsync(file);
            var byteArray = new Byte[buffer.Length];
            buffer.CopyTo(byteArray);

            try
            {
                var result = await extension.Invoke(new ValueSet() { { "byteArray", byteArray }, { "filePath", item.ItemPath } });
                var preview = result["preview"];
                CustomPreviewGrid.Children.Add(XamlReader.Load(preview as string) as UIElement);
            } catch
            {
                Debug.WriteLine("Failed to parse xaml");
            }
        }
    }
}
