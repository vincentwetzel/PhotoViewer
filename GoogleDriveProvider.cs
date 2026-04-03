using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Collections.Generic;
using System.Linq;
using PhotoViewer.Models;
using System.Threading.Tasks;
using GFile = Google.Apis.Drive.v3.Data.File;
using System;

namespace PhotoViewer.Services
{
    public class GoogleDriveProvider : IPhotoProvider
    {
        private readonly DriveService _driveService;

        public string SourceName { get; }

        public GoogleDriveProvider(UserCredential credential)
        {
            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "PhotoViewer",
            });
            SourceName = $"Google Drive ({credential.UserId})";
        }

        /// <summary>
        /// Fetches a list of all image items from the user's Google Drive.
        /// </summary>
        public async Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync()
        {
            var photoItems = new List<PhotoItem>();
            string? pageToken = null;

            do
            {
                var request = _driveService.Files.List();
                request.Q = "mimeType contains 'image/'"; // Query to find all image types
                request.Spaces = "drive";
                request.Fields = "nextPageToken, files(id, name, createdTime, size, webContentLink)";
                request.PageToken = pageToken;

                var result = await request.ExecuteAsync();
                if (result.Files != null)
                {
                    photoItems.AddRange(result.Files.Select(f => new PhotoItem(f.WebContentLink, f.Name, f.CreatedTimeDateTimeOffset?.DateTime ?? DateTime.MinValue, f.Size ?? 0)));
                }
                pageToken = result.NextPageToken;
            } while (pageToken != null);

            return photoItems;
        }
    }
}