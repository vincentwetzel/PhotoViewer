using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.Services
{
    public class GoogleAuthenticationService
    {
        // The scopes define the permissions the application is requesting.
        // Drive.Readonly is sufficient to list and download files.
        private readonly string[] _scopes = { DriveService.Scope.DriveReadonly };

        public async Task<UserCredential> SignInInteractivelyAsync()
        {
            UserCredential credential;

            // IMPORTANT: You must create a 'client_secrets.json' file in your project
            // and set its "Copy to Output Directory" property to "Copy if newer".
            // This file is obtained from the Google Cloud Console for your OAuth 2.0 Client ID.
            // See: https://developers.google.com/drive/api/v3/quickstart/dotnet
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    _scopes,
                    "user", // A unique identifier for the user.
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
            return credential;
        }
    }
}