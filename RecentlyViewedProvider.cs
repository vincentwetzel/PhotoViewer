using PhotoViewer.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoViewer.Services
{
    public class RecentlyViewedProvider : IPhotoProvider
    {
        private readonly HistoryService _historyService;

        public string SourceName => "Recently Viewed";

        public RecentlyViewedProvider(HistoryService historyService)
        {
            _historyService = historyService;
        }

        public Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync()
        {
            return Task.Run(() =>
            {
                return _historyService.GetHistory()
                    .Select(path => new FileInfo(path))
                    .Where(fileInfo => fileInfo.Exists) // Only include files that still exist
                    .Select(fileInfo => new PhotoItem(
                        fileInfo.FullName,
                        fileInfo.Name,
                        fileInfo.CreationTime,
                        fileInfo.Length));
            });
        }
    }
}