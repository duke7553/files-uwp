﻿using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using Windows.Storage;

namespace ProcessLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];

            ToggleQuickLock(executable);
        }

        static void Main2(string[] args)
        {
            var arguments = (string)ApplicationData.Current.LocalSettings.Values["Arguments"];
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                if (arguments.Equals("DetectUserPaths"))
                {
                    ApplicationData.Current.LocalSettings.Values["DetectedDesktopLocation"] = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Desktop", null);
                    ApplicationData.Current.LocalSettings.Values["DetectedDownloadsLocation"] = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", null);
                    ApplicationData.Current.LocalSettings.Values["DetectedDocumentsLocation"] = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Personal", null);
                    ApplicationData.Current.LocalSettings.Values["DetectedPicturesLocation"] = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "My Pictures", null);
                    ApplicationData.Current.LocalSettings.Values["DetectedMusicLocation"] = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "My Music", null);
                    ApplicationData.Current.LocalSettings.Values["DetectedVideosLocation"] = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "My Video", null);
                }
                else
                {
                    var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = executable;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.Arguments = arguments;
                    process.Start();
                }
            }
            else
            {
                var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = executable;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

            }

        }

        static void ToggleQuickLock(string path)
        {
            string PipeName = "QuickLook.App.Pipe." + WindowsIdentity.GetCurrent().User?.Value;
            string Toggle = "QuickLook.App.PipeMessages.Toggle";

            using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                client.Connect();

                using (var writer = new StreamWriter(client))
                {
                    writer.WriteLine($"{Toggle}|{path}");
                    writer.Flush();
                }
            }
        }
    }
}
