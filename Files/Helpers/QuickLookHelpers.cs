﻿using System;
using Microsoft.Toolkit.Uwp.Extensions;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.UI.Core;

namespace Files.Helpers
{
    public static class QuickLookHelpers
    {
        public static async void ToggleQuickLook(IShellPage associatedInstance)
        {
            try
            {
                if (associatedInstance.ContentPage.IsItemSelected && !associatedInstance.ContentPage.IsRenamingItem)
                {
                    Debug.WriteLine("Toggle QuickLook");
                    if (associatedInstance.ServiceConnection != null)
                    {
                        await associatedInstance.ServiceConnection.SendMessageSafeAsync(new ValueSet()
                        {
                            { "path", associatedInstance.ContentPage.SelectedItem.ItemPath },
                            { "Arguments", "ToggleQuickLook" }
                        });
                    }
                }
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundPreviewDialog/Text".GetLocalized());
                associatedInstance.NavigationToolbar.CanRefresh = false;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var ContentOwnedViewModelInstance = associatedInstance.FilesystemViewModel;
                    ContentOwnedViewModelInstance?.RefreshItems(null);
                });
            }
        }
    }
}
