using Microsoft.Identity.Client;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoViewer.Services
{
    public class OneDriveAuthenticationService
    {
        // ==========================================================================================
        // IMPORTANT: ACTION REQUIRED
        // ==========================================================================================
        // To enable OneDrive integration, you must register your application in the Azure Portal
        // and replace the placeholder value below with your application's Client ID.
        //
        // 1. Go to the Azure Portal: https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade
        // 2. Click 'New registration'.
        // 3. Give your app a name (e.g., "PhotoViewer").
        // 4. Under 'Supported account types', select 'Accounts in any organizational directory... and personal Microsoft accounts...'.
        // 5. Under 'Redirect URI', select 'Public client/native (mobile & desktop)' and enter 'http://localhost'.
        // 6. Click 'Register'.
        // 7. Copy the 'Application (client) ID' from the overview page and paste it below.
        // ==========================================================================================
        private const string ClientId = "YOUR_CLIENT_ID_HERE"; // <-- PASTE YOUR CLIENT ID HERE

        // Use "consumers" for personal Microsoft accounts, or your tenant ID for organizational accounts
        private const string TenantId = "consumers";

        // The scopes define the permissions the application is requesting.
        private readonly string[] _scopes = { "Files.Read.All", "User.Read" };

        private readonly IPublicClientApplication _pca;

        public OneDriveAuthenticationService()
        {
            _pca = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
                .WithDefaultRedirectUri() // Uses http://localhost for desktop apps
                .Build();
        }

        public async Task<AuthenticationResult?> SignInInteractivelyAsync()
        {
            var accounts = await _pca.GetAccountsAsync();
            AuthenticationResult? authResult;
            try
            {
                // Try to get a token silently
                authResult = await _pca.AcquireTokenSilent(_scopes, accounts.FirstOrDefault()).ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                // A MsalUiRequiredException means the user needs to sign in interactively.
                try
                {
                    authResult = await _pca.AcquireTokenInteractive(_scopes).ExecuteAsync();
                }
                catch (System.Exception)
                {
                    // Handle exceptions (e.g., user cancels the login)
                    return null;
                }
            }
            return authResult;
        }

        public async Task<AuthenticationResult?> SignInSilentlyAsync(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                return null;
            }

            try
            {
                var account = await _pca.GetAccountAsync(accountId);
                if (account != null)
                {
                    return await _pca.AcquireTokenSilent(_scopes, account).ExecuteAsync();
                }
            }
            catch (MsalUiRequiredException)
            {
                // This can happen if the user's credentials have expired or been revoked.
                return null;
            }
            catch (System.Exception)
            {
                // Handle other exceptions
                return null;
            }
            return null;
        }
    }
}