﻿using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.ViewModels.Dialogs
{
    public enum DynamicResult
    {
        Primary = 1,
        Secondary = 2,
        Cancel = 4,
    }

    public enum DynamicButtons
    {
        Primary = 1,
        Secondary = 2,
        Cancel = 4,
        None = 8
    }

    public class DynamicDialogViewModel : ObservableObject, IDisposable
    {
        #region Public Properties

        /// <summary>
        /// Stores any additional data that could be written to, read from.
        /// </summary>
        public object AdditionalData { get; set; }

        public object displayControl;
        /// <summary>
        /// The control that is dynamically displayed.
        /// </summary>
        public object DisplayControl
        {
            get => displayControl;
            set
            {
                if (SetProperty(ref displayControl, value))
                {
                    if (value == null)
                    {
                        DisplayControlLoad = false;
                    }
                    else
                    {
                        DisplayControlLoad = true;
                    }
                }
            }
        }

        private bool displayControlLoad;
        /// <summary>
        /// Determines whether the <see cref="DisplayControl"/> is loaded, value of this property is automatically handled.
        /// </summary>
        public bool DisplayControlLoad
        {
            get => displayControlLoad;
            set => SetProperty(ref displayControlLoad, value);
        }

        private DynamicButtons dynamicButtons;

        /// <summary>
        /// Decides which buttons to show.
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// Setting value to <see cref="DynamicButtons"/> may override
        /// <see cref="PrimaryButtonText"/> and/or <see cref="SecondaryButtonText"/> and/or <see cref="CloseButtonText"/>.
        /// </summary>
        public DynamicButtons DynamicButtons
        {
            get => dynamicButtons;
            set
            {
                if (SetProperty(ref dynamicButtons, value))
                {
                    if (value.HasFlag(DynamicButtons.None))
                    {
                        PrimaryButtonText = null; // Hides this option
                        SecondaryButtonText = null; // Hides this option
                        CloseButtonText = null; // Hides this option

                        return;
                    }

                    if (!value.HasFlag(DynamicButtons.Primary))
                    {
                        PrimaryButtonText = null; // Hides this option
                    }
                    if (!value.HasFlag(DynamicButtons.Secondary))
                    {
                        SecondaryButtonText = null; // Hides this option
                    }
                    if (!value.HasFlag(DynamicButtons.Cancel))
                    {
                        CloseButtonText = null; // Hides this option
                    }
                }
            }
        }

        private string titleText;
        /// <summary>
        /// The Title text of the dialog.
        /// </summary>
        public string TitleText
        {
            get => titleText;
            set => SetProperty(ref titleText, value);
        }

        private string subtitleText;
        /// <summary>
        /// The subtitle of the dialog.
        /// <br/>
        /// (Can be null or empty)
        /// </summary>
        public string SubtitleText
        {
            get => subtitleText;
            set
            {
                if (SetProperty(ref subtitleText, value))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        SubtitleLoad = true;
                    }
                    else
                    {
                        SubtitleLoad = false;
                    }
                }
            }
        }

        private bool subtitleLoad;
        /// <summary>
        /// Determines whether the <see cref="SubtitleText"/> is loaded, value of this property is automatically handled.
        /// </summary>
        public bool SubtitleLoad
        {
            get => subtitleLoad;
            private set => SetProperty(ref subtitleLoad, value);
        }

        #region Primary Button

        private string primaryButtonText;
        /// <summary>
        /// The text content of the primary button.
        /// </summary>
        public string PrimaryButtonText
        {
            get => primaryButtonText;
            set => SetProperty(ref primaryButtonText, value);
        }

        private bool isPrimaryButtonEnabled;
        /// <summary>
        /// Determines whether Primary Button is enabled.
        /// </summary>
        public bool IsPrimaryButtonEnabled
        {
            get => isPrimaryButtonEnabled;
            private set => SetProperty(ref isPrimaryButtonEnabled, value);
        }

        #endregion

        #region Secondary Button

        private string secondaryButtonText;
        /// <summary>
        /// The text of the secondary button.
        /// </summary>
        public string SecondaryButtonText
        {
            get => secondaryButtonText;
            set => SetProperty(ref secondaryButtonText, value);
        }

        private bool isSecondaryButtonEnabled;
        /// <summary>
        /// Determines whether Secondary Button is enabled.
        /// </summary>
        public bool IsSecondaryButtonEnabled
        {
            get => isSecondaryButtonEnabled;
            private set => SetProperty(ref isSecondaryButtonEnabled, value);
        }

        #endregion

        #region Close Button

        private string closeButtonText;
        /// <summary>
        /// The text of the close button.
        /// </summary>
        public string CloseButtonText
        {
            get => closeButtonText;
            set => SetProperty(ref closeButtonText, value);
        }

        #endregion

        private DynamicButtons dynamicButtonsEnabled;
        /// <summary>
        /// Determines which buttons should be enabled
        /// </summary>
        public DynamicButtons DynamicButtonsEnabled
        {
            get => dynamicButtonsEnabled;
            set
            {
                if (SetProperty(ref dynamicButtonsEnabled, value))
                {
                    if (!value.HasFlag(DynamicButtons.Cancel))
                    {
                        Debugger.Break(); // Cannot disable the Close button!
                    }

                    if (value.HasFlag(DynamicButtons.None))
                    {
                        IsPrimaryButtonEnabled = false; // Hides this option
                        IsSecondaryButtonEnabled = false; // Hides this option

                        return;
                    }

                    if (!value.HasFlag(DynamicButtons.Primary))
                    {
                        IsPrimaryButtonEnabled = false; // Hides this option
                    }
                    else
                    {
                        IsPrimaryButtonEnabled = true;
                    }

                    if (!value.HasFlag(DynamicButtons.Secondary))
                    {
                        IsSecondaryButtonEnabled = false; // Hides this option
                    }
                    else
                    {
                        IsSecondaryButtonEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// The result of the dialog, value of this property is automatically handled.
        /// </summary>
        public DynamicResult DynamicResult { get; set; }

        #endregion Public Properties

        #region Actions

        /// <summary>
        /// Hides the dialog.
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// This action is assigned by default.
        /// </summary>
        public Action HideDialog { get; set; }

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> primaryButtonAction;
        /// <summary>
        /// OnPrimary action.
        /// </summary>
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> PrimaryButtonAction
        {
            get => primaryButtonAction;
            set
            {
                if (SetProperty(ref primaryButtonAction, value))
                {
                    PrimaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicResult.Primary;
                        PrimaryButtonAction(this, e);
                    });
                }
            }
        }

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> secondaryButtonAction;
        /// <summary>
        /// OnSecondary action.
        /// </summary>
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> SecondaryButtonAction
        {
            get => secondaryButtonAction;
            set
            {
                if (SetProperty(ref secondaryButtonAction, value))
                {
                    SecondaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicResult.Secondary;
                        SecondaryButtonAction(this, e);
                    });
                }
            }
        }

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> closeButtonAction;
        /// <summary>
        /// OnClose action.
        /// </summary>
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> CloseButtonAction
        {
            get => closeButtonAction;
            set
            {
                if (SetProperty(ref closeButtonAction, value))
                {
                    CloseButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicResult.Cancel;
                        CloseButtonAction(this, e);
                    });
                }
            }
        }

        private Action<DynamicDialogViewModel, KeyRoutedEventArgs> keyDownAction;
        /// <summary>
        /// The keydown action on the dialog.
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// This action is assigned by default.
        /// </summary>
        public Action<DynamicDialogViewModel, KeyRoutedEventArgs> KeyDownAction
        {
            get => keyDownAction;
            set
            {
                if (SetProperty(ref keyDownAction, value))
                {
                    DynamicKeyDownCommand = new RelayCommand<KeyRoutedEventArgs>((e) =>
                    {
                        DynamicResult = DynamicResult.Cancel;
                        KeyDownAction(this, e);
                    });
                }
            }
        }

        #endregion Actions

        #region Commands

        public ICommand PrimaryButtonCommand { get; private set; }

        public ICommand SecondaryButtonCommand { get; private set; }

        public ICommand CloseButtonCommand { get; private set; }

        public ICommand DynamicKeyDownCommand { get; private set; }

        #endregion Commands

        #region Constructor

        public DynamicDialogViewModel()
        {
            // Create default implementation
            TitleText = "DynamicDialog";
            PrimaryButtonText = "Ok";
            PrimaryButtonAction = (vm, e) => HideDialog();
            SecondaryButtonAction = (vm, e) => HideDialog();
            CloseButtonAction = (vm, e) => HideDialog();
            KeyDownAction = (vm, e) =>
            {
                if (e.Key == VirtualKey.Escape)
                {
                    HideDialog();
                }
            };
            DynamicButtons = DynamicButtons.Primary;
            DynamicButtonsEnabled = DynamicButtons.Primary | DynamicButtons.Secondary | DynamicButtons.Cancel;
        }

        #endregion Constructor

        #region IDisposable

        public void Dispose()
        {
            (displayControl as IDisposable)?.Dispose();

            displayControl = null;
            titleText = null;
            subtitleText = null;
            primaryButtonText = null;
            secondaryButtonText = null;
            closeButtonText = null;

            primaryButtonAction = null;
            secondaryButtonAction = null;
            closeButtonAction = null;
            keyDownAction = null;

            PrimaryButtonCommand = null;
            SecondaryButtonCommand = null;
            CloseButtonCommand = null;
        }

        #endregion IDisposable
    }
}