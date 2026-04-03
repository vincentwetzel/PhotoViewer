using System;

namespace PhotoViewer.Models
{
    public class PhotoItem
    {
        public string FilePath { get; }
        public string FileName { get; }
        public DateTime CreationDate { get; }
        public long FileSizeInBytes { get; }
        public int PixelWidth { get; }
        public int PixelHeight { get; }

        public double AspectRatio => PixelHeight > 0 ? (double)PixelWidth / PixelHeight : 1.0;

        public PhotoItem(string filePath, string fileName, DateTime creationDate, long fileSizeInBytes, int pixelWidth = 0, int pixelHeight = 0)
        {
            FilePath = filePath;
            FileName = fileName;
            CreationDate = creationDate;
            FileSizeInBytes = fileSizeInBytes;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
        }
    }
}