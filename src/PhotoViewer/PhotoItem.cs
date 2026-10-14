using System;
using System.IO;
using PhotoViewer.Services;

namespace PhotoViewer.Models
{
    public class PhotoItem
    {
        public string FilePath { get; }
        public string FileName { get; }
        public DateTime CreationDate { get; }
        public DateTime DateTaken { get; }
        private readonly DateTime? _dateModifiedOverride;
        public DateTime DateModified
        {
            get
            {
                if (_dateModifiedOverride.HasValue) return _dateModifiedOverride.Value;
                try
                {
                    return File.Exists(FilePath) ? File.GetLastWriteTime(FilePath) : CreationDate;
                }
                catch
                {
                    return CreationDate;
                }
            }
        }
        public long FileSizeInBytes { get; }
        public int PixelWidth { get; }
        public int PixelHeight { get; }

        public double AspectRatio => PixelHeight > 0 ? (double)PixelWidth / PixelHeight : 1.0;

        public bool IsVideo => MediaFileTypes.IsVideo(FileName);

        public PhotoItem(string filePath, string fileName, DateTime creationDate, long fileSizeInBytes, int pixelWidth = 0, int pixelHeight = 0, DateTime? dateTaken = null, DateTime? dateModified = null)
        {
            FilePath = filePath;
            FileName = fileName;
            CreationDate = creationDate;
            DateTaken = dateTaken ?? creationDate;
            _dateModifiedOverride = dateModified;
            FileSizeInBytes = fileSizeInBytes;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
        }
    }
}
