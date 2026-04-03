using System;

namespace PhotoViewer.Models
{
    public class PhotoItem
    {
        public string FilePath { get; }
        public string FileName { get; }
        public DateTime CreationDate { get; }
        public long FileSizeInBytes { get; }

        public PhotoItem(string filePath, string fileName, DateTime creationDate, long fileSizeInBytes)
        {
            FilePath = filePath;
            FileName = fileName;
            CreationDate = creationDate;
            FileSizeInBytes = fileSizeInBytes;
        }
    }
}