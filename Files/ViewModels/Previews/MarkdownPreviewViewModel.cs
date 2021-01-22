﻿using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.ViewModels.Previews
{
    public class MarkdownPreviewViewModel : BasePreviewModel
    {
        public MarkdownPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public static List<string> Extensions => new List<string>() {
            ".md", ".markdown",
        };

        public string textValue;
        public string TextValue
        {
            get => textValue;
            set => SetProperty(ref textValue, value);
        }

        public override async void LoadPreviewAndDetails()
        {
            try
            {
                var text = await FileIO.ReadTextAsync(ItemFile);
                var displayText = text.Length < Constants.PreviewPane.TextCharacterLimit ? text : text.Remove(Constants.PreviewPane.TextCharacterLimit);
                TextValue = displayText;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            base.LoadSystemFileProperties();
        }
    }
}
