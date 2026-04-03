using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions;
using PhotoViewer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoViewer.Services
{
    public class OneDriveProvider : IPhotoProvider
    {
        private readonly GraphServiceClient _graphClient;

        public OneDriveProvider(AuthenticationResult authResult)
        {
            var authProvider = new TokenAuthenticationProvider(authResult.AccessToken);
            _graphClient = new GraphServiceClient(authProvider);
        }

        public string SourceName => "OneDrive";

        /// <summary>
        /// Fetches a list of image items from the user's special 'Pictures' folder in OneDrive.
        /// </summary>
        public async Task<IEnumerable<PhotoItem>> GetPhotoPathsAsync()
        {
            var photos = new List<PhotoItem>();

            // The fluent API for .Special is not working as expected.
            // We build the request URL manually as a workaround.
            var requestInfo = _graphClient.Me.Drive.ToGetRequestInformation();
            requestInfo.UrlTemplate = requestInfo.UrlTemplate?.Replace("{?%24select}", "/special/pictures/children{?%24select}");
            
            requestInfo.QueryParameters.Add("%24select", new[] { "id", "name", "webUrl", "@microsoft.graph.downloadUrl", "image", "createdDateTime", "size" });

            var itemsPage = await _graphClient.RequestAdapter.SendAsync(requestInfo, DriveItemCollectionResponse.CreateFromDiscriminatorValue);

            if (itemsPage?.Value == null)
            {
                return photos;
            }

            // Filter for items that are images and have a name
            var imageItems = itemsPage.Value.Where(item => item.Image != null && item.AdditionalData.ContainsKey("@microsoft.graph.downloadUrl") && item.Name != null);
            photos.AddRange(imageItems.Select(item => new PhotoItem(item.AdditionalData["@microsoft.graph.downloadUrl"].ToString()!, item.Name!, item.CreatedDateTime?.DateTime ?? System.DateTime.MinValue, item.Size ?? 0)));
            return photos;
        }
    }
}