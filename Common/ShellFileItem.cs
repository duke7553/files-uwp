﻿using System;

namespace Files.Common
{
    public class ShellFileItem
    {
        public bool IsFolder;
        public string RecyclePath;
        public string FileName;
        public string FilePath;
        public DateTime RecycleDate;
        public string FileSize;
        public long FileSizeBytes;
        public string FileType;

        public ShellFileItem()
        {
        }

        public ShellFileItem(bool isFolder, string recyclePath, string fileName, string filePath, DateTime recycleDate, string fileSize, long fileSizeBytes, string fileType)
        {
            this.IsFolder = isFolder;
            this.RecyclePath = recyclePath;
            this.FileName = fileName;
            this.FilePath = filePath;
            this.RecycleDate = recycleDate;
            this.FileSize = fileSize;
            this.FileSizeBytes = fileSizeBytes;
            this.FileType = fileType;
        }
    }
}