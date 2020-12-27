﻿using Files.Filesystem;
using Files.Interacts;
using Files.UserControls;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.AppService;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class PaneHolderPage : Page, IShellPage, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public PaneHolderPage()
        {
            this.InitializeComponent();
            ActivePane = PaneLeft;
        }

        private string navParamsLeft;

        public string NavParamsLeft
        {
            get => navParamsLeft;
            set
            {
                if (navParamsLeft != value)
                {
                    navParamsLeft = value;
                    NotifyPropertyChanged("NavParamsLeft");
                }
            }
        }

        private string navParamsRight;

        public string NavParamsRight
        {
            get => navParamsRight;
            set
            {
                if (navParamsRight != value)
                {
                    navParamsRight = value;
                    NotifyPropertyChanged("NavParamsRight");
                }
            }
        }

        private IShellPage activePane;

        public IShellPage ActivePane
        {
            get => activePane;
            set
            {
                if (activePane != value)
                {
                    activePane = value;
                    PaneLeft.IsCurrentInstance = false;
                    if (PaneRight != null)
                    {
                        PaneRight.IsCurrentInstance = false;
                    }
                    if (ActivePane != null)
                    {
                        ActivePane.IsCurrentInstance = isCurrentInstance;
                    }
                    NotifyPropertyChanged("ActivePane");
                    NotifyPropertyChanged("IsLeftPaneActive");
                    NotifyPropertyChanged("IsRightPaneActive");
                }
            }
        }

        public bool IsLeftPaneActive => ActivePane == PaneLeft;

        public bool IsRightPaneActive => ActivePane == PaneRight;

        private bool isRightPaneVisible;

        public bool IsRightPaneVisible
        {
            get => isRightPaneVisible;
            set
            {
                if (value != isRightPaneVisible)
                {
                    isRightPaneVisible = value;
                    NotifyPropertyChanged("IsRightPaneVisible");
                }
            }
        }

        public StatusBarControl BottomStatusStripControl => ActivePane?.BottomStatusStripControl;

        public Frame ContentFrame => ActivePane?.ContentFrame;

        public Interaction InteractionOperations => ActivePane?.InteractionOperations;

        public ItemViewModel FilesystemViewModel => ActivePane?.FilesystemViewModel;

        public CurrentInstanceViewModel InstanceViewModel => ActivePane?.InstanceViewModel;

        public AppServiceConnection ServiceConnection => ActivePane?.ServiceConnection;

        BaseLayout IShellPage.ContentPage => ActivePane?.ContentPage;

        public Control OperationsControl => ActivePane?.OperationsControl;

        public Type CurrentPageType => ActivePane?.CurrentPageType;

        public INavigationControlItem SidebarSelectedItem
        {
            get => ActivePane?.SidebarSelectedItem;
            set
            {
                if (ActivePane != null)
                {
                    ActivePane.SidebarSelectedItem = value;
                }
            }
        }

        public INavigationToolbar NavigationToolbar => ActivePane?.NavigationToolbar;

        private bool isCurrentInstance;

        public bool IsCurrentInstance
        {
            get => isCurrentInstance;
            set
            {
                isCurrentInstance = value;
                PaneLeft.IsCurrentInstance = false;
                if (PaneRight != null)
                {
                    PaneRight.IsCurrentInstance = false;
                }
                if (ActivePane != null)
                {
                    ActivePane.IsCurrentInstance = value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            NavParamsLeft = eventArgs.Parameter.ToString();
            NavParamsRight = "NewTab".GetLocalized();
        }

        public void Clipboard_ContentChanged(object sender, object e)
        {

        }

        public void Refresh_Click()
        {
            ActivePane?.Refresh_Click();
        }

        public void Dispose()
        {
            PaneLeft?.Dispose();
            PaneRight?.Dispose();
        }

        private void PaneLeft_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ActivePane = PaneLeft;
            e.Handled = false;
        }

        private void PaneRight_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ActivePane = PaneRight;
            e.Handled = false;
        }

        private void PaneResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (PaneRight != null && PaneRight.ActualWidth <= 300)
            {
                IsRightPaneVisible = false;
            }
        }

        private void ShowPaneButton_Click(object sender, RoutedEventArgs e)
        {
            IsRightPaneVisible = true;
        }

        private void Page_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!IsRightPaneVisible)
            {
                var position = e.GetCurrentPoint(null).Position;
                if (position.X >= this.ActualWidth - 70 && Math.Abs(position.Y - Window.Current.Bounds.Height / 2) <= 70)
                {
                    AddPaneButton.Visibility = Visibility.Visible;
                    AddPaneTeachingTip.IsOpen = false;
                }
                else
                {
                    AddPaneButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Page_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsRightPaneVisible)
            {
                var position = e.GetCurrentPoint(null).Position;
                if (position.X >= this.ActualWidth - 70 && Math.Abs(position.Y - Window.Current.Bounds.Height / 2) <= 70)
                {
                    AddPaneButton.Visibility = Visibility.Visible;
                    AddPaneTeachingTip.IsOpen = false;
                }
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSettings.ShowDualPaneTeachingTip)
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(2));
                AddPaneTeachingTip.IsOpen = true;
                //AppSettings.ShowDualPaneTeachingTip = false;
            }
        }
    }
}
