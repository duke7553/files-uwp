﻿using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class CodePreview : UserControl
    {
        public CodePreview(ListedItem item)
        {
            this.InitializeComponent();
            SetFile(item);
        }

        public static List<string> Extensions => (new List<List<string>>(languageExtensions.Values).SelectMany(i => i).Distinct()).ToList();

        async void SetFile(ListedItem item)
        {
            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
            var text = await FileIO.ReadTextAsync(file);
            // Use the MarkDownTextBlock's built in code highlighting
            TextPreviewControl.Text = $"```{GetCodeLanguage(item.FileExtension)}\n{text}\n```";
        }

        static Dictionary<string, List<string>> languageExtensions = new Dictionary<string, List<string>>()
        {
            {"xml",  new List<string> {".xml", ".axml", ".xaml" } },
            {"json",  new List<string> {".json" } },
            {"yaml", new List<string> {".yml"} },
            {"python",  new List<string> {".py", ".py3", ".py", ".cgi", ".fcgi", ".gyp", ".gypi", ".lmi", ".py3", ".pyde", ".pyi", ".pyp", ".pyt", ".pyw", ".rpy", ".smk", ".spec", ".tac", ".wsgi", ".xpy" } },
            {"cs",  new List<string> {".cs", ".cake", ".csx", ".linq" } },
            {"fs",  new List<string> {".fs", "fsi", "fsx" } },
            {"java",  new List<string> {".java" } },
            {"vbnet",  new List<string> {".vb", "vbhtml" } },
            {"c",  new List<string> {".c" } },
            {"cpp",  new List<string> {".cpp", "c++", ".cc", ".cp", ".cxx", ".h", ".h++", ".hh", ".hpp", ".hxx", ".inc", ".inl", ".ino", ".ipp", ".re", ".tcc", ".tpp" } },
            {"powershell",  new List<string> {".pwsh", ".ps1", "psd1", ".psm1" } },
        };

        static string GetCodeLanguage(string ext)
        {
            foreach (var lang in languageExtensions)
            {
                if(lang.Value.Contains(ext))
                {
                    return lang.Key;
                }
            }
            return ext;
        }
    }
}
