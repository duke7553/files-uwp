﻿using Files.Dialogs;
using Files.View_Models.Properties;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Views
{
    public sealed partial class PropertiesDetails : PropertiesTab
    {
        public PropertiesDetails()
        {
            InitializeComponent();
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties != null)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                (BaseProperties as FileProperties).GetSystemFileProperties();
                stopwatch.Stop();
                Debug.WriteLine(string.Format("System file properties were obtained in {0} milliseconds", stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Tries to save changed properties to file.
        /// </summary>
        /// <returns>Returns true if properties have been saved successfully.</returns>
        public async Task<bool> SaveChangesAsync()
        {
            while (true)
            {
                var dialog = new PropertySaveError();
                try
                {
                    await (BaseProperties as FileProperties).SyncPropertyChangesAsync();
                    return true;
                }
                catch
                {
                    switch (await dialog.ShowAsync())
                    {
                        case ContentDialogResult.Primary:
                            break;

                        case ContentDialogResult.Secondary:
                            return true;

                        default:
                            return false;
                    }
                }

                // Wait for the current dialog to be closed before continuing the loop
                // and opening another dialog (attempting to open more than one ContentDialog
                // at a time will throw an error)
                while (dialog.IsLoaded)
                { }
            }
        }

        private async void ClearPropertiesConfirmation_Click(object sender, RoutedEventArgs e)
        {
            ClearPropertiesFlyout.Hide();
            await (BaseProperties as FileProperties).ClearPropertiesAsync();
        }
    }
}