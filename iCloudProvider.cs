﻿using PhotoViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoViewer.Services
{
    public class iCloudProvider : IPhotoProvider
    {
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

        public string SourceName => "iCloud Photos";

        public Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync()
        {
            return Task.Run(() => {
                var iCloudPhotosPath = GetiCloudPhotosPath();

                if (string.IsNullOrEmpty(iCloudPhotosPath) || !Directory.Exists(iCloudPhotosPath))
                {
                    // Silently fail if the directory doesn't exist.
                    return Enumerable.Empty<PhotoItem>();
                }

                var directoryInfo = new DirectoryInfo(iCloudPhotosPath);

                var enumerationOptions = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    MatchType = MatchType.Simple,
                    AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
                };

                return directoryInfo.EnumerateFiles("*", enumerationOptions)
                    .Where(file => SupportedExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
                    .Select(fileInfo => new PhotoItem(
                        fileInfo.FullName,
                        fileInfo.Name,
                        fileInfo.CreationTime,
                        fileInfo.Length));
            });
        }

        private string GetiCloudPhotosPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "iCloud Photos");
        }
    }
}