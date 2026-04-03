using PhotoViewer.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoViewer.Services
{
    public class FavoritesProvider : IPhotoProvider
    {
        private readonly FavoritesService _favoritesService;

        public string SourceName => "Favorites";

        public FavoritesProvider(FavoritesService favoritesService)
        {
            _favoritesService = favoritesService;
        }

        public Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync()
        {
            return Task.Run(() =>
            {
                return _favoritesService.GetFavorites()
                    .Where(File.Exists)
                    .Select(filePath =>
                    {
                        var fileInfo = new FileInfo(filePath);
                        return new PhotoItem(
                            fileInfo.FullName,
                            fileInfo.Name,
                            fileInfo.CreationTime,
                            fileInfo.Length);
                    });
            });
        }
    }
}