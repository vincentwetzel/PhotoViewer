using PhotoViewer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoViewer.Services;

public interface IPhotoProvider
{
    string SourceName { get; }
    Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync();
}