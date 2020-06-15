﻿using Files.View_Models;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class StatusBarControl : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel => App.SelectedItemsPropertiesViewModel;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel => App.DirectoryPropertiesViewModel;

        public StatusBarControl()
        {
            this.InitializeComponent();
        }
    }
}