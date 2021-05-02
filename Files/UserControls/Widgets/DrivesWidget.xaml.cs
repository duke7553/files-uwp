﻿using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels;
using Files.ViewModels.Widgets;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;

namespace Files.UserControls.Widgets
{
    public sealed partial class DrivesWidget : UserControl, IWidgetItemModel, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

        public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;

        public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

        public event DrivesWidgetNewPaneInvokedEventHandler DrivesWidgetNewPaneInvoked;

        public event PropertyChangedEventHandler PropertyChanged;

        public static ObservableCollection<INavigationControlItem> ItemsAdded = new ObservableCollection<INavigationControlItem>();

        private IShellPage associatedInstance;

        public IShellPage AppInstance
        {
            get => associatedInstance;
            set
            {
                if (value != associatedInstance)
                {
                    associatedInstance = value;
                    NotifyPropertyChanged(nameof(AppInstance));
                }
            }
        }

        public string WidgetName => nameof(DrivesWidget);

        public bool IsWidgetSettingEnabled => App.AppSettings.ShowDrivesWidget;

        public DrivesWidget()
        {
            InitializeComponent();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void EjectDevice_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            await DriveHelpers.EjectDeviceAsync(item.Path);
        }

        private async void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            await NavigationHelpers.OpenPathInNewTab(item.Path);
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
        }

        private async void OpenDriveProperties_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            await FilePropertiesHelpers.OpenPropertiesWindowAsync(item, associatedInstance);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string NavigationPath = ""; // path to navigate
            string ClickedCard = (sender as Button).Tag.ToString();

            NavigationPath = ClickedCard;

            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            if (ctrlPressed)
            {
                await NavigationHelpers.OpenPathInNewTab(NavigationPath);
                return;
            }

            DrivesWidgetInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
            {
                Path = NavigationPath
            });
        }

        private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed) // check middle click
            {
                string navigationPath = (sender as Button).Tag.ToString();
                await NavigationHelpers.OpenPathInNewTab(navigationPath);
            }
        }

        public class DrivesWidgetInvokedEventArgs : EventArgs
        {
            public string Path { get; set; }
        }

        private void GridScaleUp(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Source for the scaling: https://github.com/windows-toolkit/WindowsCommunityToolkit/blob/master/Microsoft.Toolkit.Uwp.SampleApp/SamplePages/Implicit%20Animations/ImplicitAnimationsPage.xaml.cs
            // Search for "Scale Element".
            var element = sender as UIElement;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Scale = new Vector3(1.02f, 1.02f, 1);
        }

        private void GridScaleNormal(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var element = sender as UIElement;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Scale = new Vector3(1);
        }

        public bool ShowMultiPaneControls
        {
            get => AppInstance.IsMultiPaneEnabled && AppInstance.IsPageMainPane;
        }

        private void OpenInNewPane_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            DrivesWidgetNewPaneInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
            {
                Path = item.Path
            });
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            var newPaneMenuItem = (sender as MenuFlyout).Items.Single(x => x.Name == "OpenInNewPane");
            newPaneMenuItem.Visibility = ShowMultiPaneControls ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void MapNetworkDrive_Click(object sender, RoutedEventArgs e)
        {
            if (AppInstance.ServiceConnection != null)
            {
                await AppInstance.ServiceConnection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "NetworkDriveOperation" },
                        { "netdriveop", "OpenMapNetworkDriveDialog" }
                    });
            }
        }

        private async void DisconnectNetworkDrive_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            if (AppInstance.ServiceConnection != null)
            {
                await AppInstance.ServiceConnection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "NetworkDriveOperation" },
                        { "netdriveop", "DisconnectNetworkDrive" },
                        { "drive", item.Path }
                    });
            }
        }

        private async void GoToStorageSense_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:storagesense"));
        }

        public void Dispose()
        {
        }
    }
}