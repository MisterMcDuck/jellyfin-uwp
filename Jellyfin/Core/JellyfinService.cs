using System.Net.Http;
using System.Net.Http.Headers;
using Jellyfin.Sdk;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace Jellyfin.Core;

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
}
