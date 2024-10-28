using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace Jellyfin.Core
{
    public sealed class JellyfinService
    {
        private readonly JellyfinSdkSettings _settings;

        public JellyfinService()
        {
            _settings = new JellyfinSdkSettings();

            // Initialize the sdk client settings. This only needs to happen once on startup.
            PackageId packageId = Package.Current.Id;
            PackageVersion packageVersion = packageId.Version;
            string packageVersionStr = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
            var deviceInformation = new EasClientDeviceInformation();
            _settings.Initialize(
                packageId.Name,
                packageVersionStr,
                deviceInformation.FriendlyName,
                deviceInformation.Id.ToString());

            // TODO: Set this from caller?
            ////_settings.SetServerUrl(Central.Settings.JellyfinServer);

            var authenticationProvider = new JellyfinAuthenticationProvider(_settings);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(packageId.Name, packageVersionStr));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 1.0));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));

            var requestAdapter = new JellyfinRequestAdapter(authenticationProvider, _settings, httpClient);

            Client = new JellyfinApiClient(requestAdapter);
        }

        // TODO: Use DI
        public static JellyfinService Instance { get; } = new JellyfinService();

        public JellyfinApiClient Client { get; }

        public void SetServerUrl(string serverUrl) => _settings.SetServerUrl(serverUrl);

        public async Task AuthenticateUserByNameAsync(string username, string password, CancellationToken cancellationToken)
        {
            try
            {
                ////Console.WriteLine($"Logging into {_sdkClientSettings.ServerUrl}");

                // Authenticate user.
                var request = new AuthenticateUserByName
                {
                    Username = username,
                    Pw = password,
                };
                AuthenticationResult authenticationResult = await Client.Users.AuthenticateByName.PostAsync(request)
                    .ConfigureAwait(false);

                _settings.SetAccessToken(authenticationResult.AccessToken);
                ////userDto = authenticationResult.User;
                ////Console.WriteLine("Authentication success.");
                ////Console.WriteLine($"Welcome to Jellyfin - {userDto.Name}");
            }
            catch (Exception)
            {
                ////await Console.Error.WriteLineAsync("Error authenticating.").ConfigureAwait(false);
                ////await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            }
        }
    }
}
