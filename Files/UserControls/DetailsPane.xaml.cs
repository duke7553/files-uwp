﻿using Files.Filesystem;
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
                if (value.Count == 1 && value[0].FileText != null)
                {
                    foreach (var extension in AppData.FilePreviewExtensionManager.Extensions)
                    {
                        if(extension.FileExtensions.Contains(value[0].FileExtension))
                        {
                            UpdatePreviewControl(value[0], extension);
                            break;
                        }
                    }
                    //if (value[0].FileExtension.Equals(".md"))
                    //{
                    //    UpdatePreviewControl(value[0]);
                    //    //MarkdownTextPreview.Text = value[0].FileText;
                    //    //MarkdownTextPreview.Visibility = Visibility.Visible;
                    //    //TextPreview.Visibility = Visibility.Collapsed;
                    //}
                    //else
                    //{
                    //    //TextPreview.Text = value[0].FileText;
                    //    //MarkdownTextPreview.Visibility = Visibility.Collapsed;
                    //    //TextPreview.Visibility = Visibility.Visible;
                    //}
                }
                else
                {
                    TextPreview.Text = "No preview avaliable";
                }
                SetValue(selectedItemsProperty, value);
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
                CustomPreviewGrid.Children.Clear();
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
