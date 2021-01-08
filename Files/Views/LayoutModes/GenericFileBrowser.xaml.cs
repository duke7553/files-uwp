using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls.Selection;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files.Views.LayoutModes
{
    public sealed partial class GenericFileBrowser : BaseLayout
    {
        private string oldItemName;
        private DataGridColumn sortedColumn;

        private static readonly MethodInfo SelectAllMethod = typeof(DataGrid)
            .GetMethod("SelectAll", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

        public DataGridColumn SortedColumn
        {
            get
            {
                return sortedColumn;
            }
            set
            {
                if (value == nameColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.Name;
                }
                else if (value == dateColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.DateModified;
                }
                else if (value == typeColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.FileType;
                }
                else if (value == sizeColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.Size;
                }
                else if (value == originalPathColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.OriginalPath;
                }
                else if (value == dateDeletedColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.DateDeleted;
                }
                else
                {
                    FolderSettings.DirectorySortOption = SortOption.Name;
                }

                if (value != sortedColumn)
                {
                    // Remove arrow on previous sorted column
                    if (sortedColumn != null)
                    {
                        sortedColumn.SortDirection = null;
                    }
                }
                value.SortDirection = FolderSettings.DirectorySortDirection == SortDirection.Ascending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
                sortedColumn = value;
            }
        }

        public GenericFileBrowser()
        {
            InitializeComponent();
            base.BaseLayoutContextFlyout = BaseLayoutContextFlyout;
            base.BaseLayoutItemContextFlyout = BaseLayoutItemContextFlyout;

            tapDebounceTimer = new DispatcherTimer();

            var selectionRectangle = RectangleSelection.Create(AllView, SelectionRectangle, AllView_SelectionChanged);
            selectionRectangle.SelectionStarted += SelectionRectangle_SelectionStarted;
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
            AllView.PointerCaptureLost += AllView_ItemPress;
        }

        private void SelectionRectangle_SelectionStarted(object sender, EventArgs e)
        {
            // If drag selection is active do not trigger file open on pointer release
            AllView.PointerCaptureLost -= AllView_ItemPress;
        }

        private void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            // Restore file open on pointer release
            AllView.PointerCaptureLost += AllView_ItemPress;
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            ParentShellPageInstance.FilesystemViewModel.PropertyChanged += ViewModel_PropertyChanged;
            AllView.LoadingRow += AllView_LoadingRow;
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            ViewModel_PropertyChanged(null, new PropertyChangedEventArgs("DirectorySortOption"));
            var parameters = (NavigationArguments)eventArgs.Parameter;
            if (parameters.IsLayoutSwitch)
            {
                ReloadItemIcons();
            }
        }

        private void ReloadItemIcons()
        {
            var rows = new List<DataGridRow>();
            Interaction.FindChildren<DataGridRow>(rows, AllView);
            foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders)
            {
                listedItem.ItemPropertiesInitialized = false;
                if (rows.Any(x => x.DataContext == listedItem))
                {
                    ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem);
                    listedItem.ItemPropertiesInitialized = true;
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            ParentShellPageInstance.FilesystemViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            AllView.LoadingRow -= AllView_LoadingRow;
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
        }

        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
        }

        protected override void AddSelectedItem(ListedItem item)
        {
            AllView.SelectedItems.Add(item);
        }

        protected override IEnumerable GetAllItems()
        {
            return AllView.ItemsSource;
        }

        public override void SelectAllItems()
        {
            SelectAllMethod.Invoke(AllView, null);
        }

        public override void ClearSelection()
        {
            AllView.SelectedItems.Clear();
        }

        public override void SetDragModeForItems()
        {
            if (IsItemSelected && !InstanceViewModel.IsPageTypeSearchResults)
            {
                var rows = new List<DataGridRow>();
                Interaction.FindChildren<DataGridRow>(rows, AllView);
                foreach (DataGridRow row in rows)
                {
                    row.CanDrag = SelectedItems.Contains(row.DataContext);
                }
            }
        }

        public override void ScrollIntoView(ListedItem item)
        {
            AllView.ScrollIntoView(item, null);
        }

        public override void FocusFileList()
        {
            AllView.Focus(FocusState.Programmatic);
        }

        public override void FocusSelectedItems()
        {
            AllView.ScrollIntoView(AllView.ItemsSource.Cast<ListedItem>().Last(), null);
        }

        public override void StartRenameItem()
        {
            if (AllView.SelectedIndex != -1)
            {
                AllView.CurrentColumn = AllView.Columns[1];
                AllView.BeginEdit();
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DirectorySortOption")
            {
                switch (FolderSettings.DirectorySortOption)
                {
                    case SortOption.Name:
                        SortedColumn = nameColumn;
                        break;

                    case SortOption.DateModified:
                        SortedColumn = dateColumn;
                        break;

                    case SortOption.FileType:
                        SortedColumn = typeColumn;
                        break;

                    case SortOption.Size:
                        SortedColumn = sizeColumn;
                        break;

                    case SortOption.OriginalPath:
                        SortedColumn = originalPathColumn;
                        break;

                    case SortOption.DateDeleted:
                        SortedColumn = dateDeletedColumn;
                        break;
                }
            }
            else if (e.PropertyName == "DirectorySortDirection")
            {
                // Swap arrows
                SortedColumn = sortedColumn;
            }
        }

        private TextBox renamingTextBox;

        private DispatcherTimer tapDebounceTimer;
        private void AllView_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (ParentShellPageInstance.FilesystemViewModel.WorkingDirectory.StartsWith(AppSettings.RecycleBinPath))
            {
                // Do not rename files and folders inside the recycle bin
                e.Cancel = true;
                return;
            }

            if (e.EditingEventArgs is TappedRoutedEventArgs && AppSettings.OpenItemsWithOneclick)
            {
                // If for some reason we started renaming by a click in a one-click mode, cancel it
                e.Cancel = true;
                return;
            }
        }

        private void AllView_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            int extensionLength = SelectedItem.FileExtension?.Length ?? 0;
            oldItemName = SelectedItem.ItemName;

            renamingTextBox = e.EditingElement as TextBox;
            renamingTextBox.Focus(FocusState.Programmatic); // Without this,the user cannot edit the text box when renaming via right-click

            int selectedTextLength = SelectedItem.ItemName.Length;
            if (!SelectedItem.IsShortcutItem && AppSettings.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            renamingTextBox.Select(0, selectedTextLength);
            renamingTextBox.TextChanged += TextBox_TextChanged;
            e.EditingElement.LosingFocus += EditingElement_LosingFocus;
            IsRenamingItem = true;
        }

        private void EditingElement_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.NewFocusedElement is Popup || args.NewFocusedElement is AppBarButton)
            {
                args.Cancel = true;
                args.TryCancel();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text))
            {
                FileNameTeachingTip.IsOpen = true;
            }
            else
            {
                FileNameTeachingTip.IsOpen = false;
            }
        }

        private async void AllView_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            e.EditingElement.LosingFocus -= EditingElement_LosingFocus;
            if (e.EditAction == DataGridEditAction.Cancel || renamingTextBox == null)
            {
                return;
            }

            renamingTextBox.Text = renamingTextBox.Text.Trim().TrimEnd('.');

            var selectedItem = e.Row.DataContext as ListedItem;
            string newItemName = renamingTextBox.Text;

            bool successful = await ParentShellPageInstance.InteractionOperations.RenameFileItemAsync(selectedItem, oldItemName, newItemName);
            if (!successful)
            {
                selectedItem.ItemName = oldItemName;
                renamingTextBox.Text = oldItemName;
            }
        }

        private void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            if (renamingTextBox != null)
            {
                renamingTextBox.TextChanged -= TextBox_TextChanged;
            }
            FileNameTeachingTip.IsOpen = false;
            IsRenamingItem = false;
        }

        private async void AllView_ItemPress(object sender, PointerRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            var cp = e.GetCurrentPoint((UIElement)sender);
            if (cp.Position.Y <= AllView.ColumnHeaderHeight // Return if click is on the header
                || cp.Properties.IsLeftButtonPressed // Return if dragging an item
                || cp.Properties.IsRightButtonPressed // Return if the user right clicks an item
                || ctrlPressed || shiftPressed) // Allow for Ctrl+Shift selection
            {
                return;
            }

            // Check if the setting to open items with a single click is turned on
            if (AppSettings.OpenItemsWithOneclick)
            {
                await Task.Delay(200); // The delay gives time for the item to be selected
                ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
            }
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
            SelectedItems = AllView.SelectedItems.Cast<ListedItem>().ToList();
        }

        private void AllView_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column == SortedColumn)
            {
                ParentShellPageInstance.FilesystemViewModel.IsSortedAscending = !ParentShellPageInstance.FilesystemViewModel.IsSortedAscending;
                e.Column.SortDirection = ParentShellPageInstance.FilesystemViewModel.IsSortedAscending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
            }
            else if (e.Column != iconColumn)
            {
                SortedColumn = e.Column;
                e.Column.SortDirection = DataGridSortDirection.Ascending;
                ParentShellPageInstance.FilesystemViewModel.IsSortedAscending = true;
            }
        }

        private void AllView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (IsRenamingItem)
                {
                    AllView.CommitEdit();
                }
                else
                {
                    ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
                }
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                ParentShellPageInstance.InteractionOperations.ShowPropertiesButton_Click(null, null);
            }
            else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
            {
                // Unfocus the ListView so keyboard shortcut can be handled
                Focus(FocusState.Programmatic);
            }
            else if (ctrlPressed && shiftPressed && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.W))
            {
                // Unfocus the ListView so keyboard shortcut can be handled (ctrl + shift + W/"->"/"<-")
                Focus(FocusState.Programmatic);
            }
            else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
            {
                // Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
                Focus(FocusState.Programmatic);
            }
        }

        public void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (!IsRenamingItem)
            {
                HandleRightClick(sender, e);
            }
        }

        public void AllView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            HandleRightClick(sender, e);
        }

        private void HandleRightClick(object sender, RoutedEventArgs e)
        {
            var rowPressed = Interaction.FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (rowPressed != null)
            {
                var objectPressed = ((ReadOnlyObservableCollection<ListedItem>)AllView.ItemsSource)[rowPressed.GetIndex()];

                // Check if RightTapped row is currently selected
                if (IsItemSelected)
                {
                    if (SelectedItems.Contains(objectPressed))
                    {
                        return;
                    }
                }

                // The following code is only reachable when a user RightTapped an unselected row
                SetSelectedItemOnUi(objectPressed);
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance != null)
            {
                if (ParentShellPageInstance.CurrentPageType == typeof(GenericFileBrowser))
                {
                    // Don't block the various uses of enter key (key 13)
                    var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
                    if (args.KeyCode == 13 || focusedElement is Button || focusedElement is TextBox || focusedElement is PasswordBox ||
                        Interaction.FindParent<ContentDialog>(focusedElement) != null)
                    {
                        return;
                    }

                    base.Page_CharacterReceived(sender, args);
                    AllView.Focus(FocusState.Keyboard);
                }
            }
        }

        private void AllView_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            InitializeDrag(e.Row);

            if (e.Row.DataContext is ListedItem item && !item.ItemPropertiesInitialized)
            {
                ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(item);
                item.ItemPropertiesInitialized = true;
            }
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            DataGridRow row = element as DataGridRow;
            return row.DataContext as ListedItem;
        }

        private void RadioMenuSortColumn_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumnEventArgs args = null;

            switch ((sender as RadioMenuFlyoutItem).Tag)
            {
                case "nameColumn":
                    args = new DataGridColumnEventArgs(nameColumn);
                    break;

                case "dateColumn":
                    args = new DataGridColumnEventArgs(dateColumn);
                    break;

                case "typeColumn":
                    args = new DataGridColumnEventArgs(typeColumn);
                    break;

                case "sizeColumn":
                    args = new DataGridColumnEventArgs(sizeColumn);
                    break;

                case "originalPathColumn":
                    args = new DataGridColumnEventArgs(originalPathColumn);
                    break;

                case "dateDeletedColumn":
                    args = new DataGridColumnEventArgs(dateDeletedColumn);
                    break;
            }

            if (args != null)
            {
                AllView_Sorting(sender, args);
            }
        }

        private void RadioMenuSortDirection_Click(object sender, RoutedEventArgs e)
        {
            if (((sender as RadioMenuFlyoutItem).Tag as string) == "sortAscending")
            {
                SortedColumn.SortDirection = DataGridSortDirection.Ascending;
            }
            else if (((sender as RadioMenuFlyoutItem).Tag as string) == "sortDescending")
            {
                SortedColumn.SortDirection = DataGridSortDirection.Descending;
            }
        }
    }
}