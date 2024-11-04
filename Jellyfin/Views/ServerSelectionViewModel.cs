using System;
using Jellyfin.Common;
using Jellyfin.Sdk;

namespace Jellyfin.Views;

public sealed class ServerSelectionViewModel : BindableBase
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;

    public ServerSelectionViewModel(AppSettings appSettings, JellyfinSdkSettings sdkClientSettings, JellyfinApiClient jellyfinApiClient)
    {
        _appSettings = appSettings;
        _sdkClientSettings = sdkClientSettings;
        _jellyfinApiClient = jellyfinApiClient;

        if (_appSettings.ServerUrl is not null)
        {
            ServerUrl = _appSettings.ServerUrl;
        }

        IsInteractable = true;
    }

    public bool IsInteractable { get; set => SetProperty(ref field, value); }

    public string ErrorMessage { get; set => SetProperty(ref field, value); }

    public bool ShowErrorMessage { get; set => SetProperty(ref field, value); }

    public string ServerUrl { get; set => SetProperty(ref field, value); }

    public async void Connect()
    {
        IsInteractable = false;
        try
        {
            ShowErrorMessage = false;

            string serverUrl = ServerUrl;

            // Add protocol if needed
            if (!serverUrl.Contains("://", StringComparison.Ordinal))
            {
                serverUrl = "https://" + serverUrl;
            }

            if (!Uri.IsWellFormedUriString(serverUrl, UriKind.Absolute))
            {
                UpdateErrorMessage($"Invalid url: {serverUrl}");
                return;
            }

            _sdkClientSettings.SetServerUrl(serverUrl);
            try
            {
                // Get public system info to verify that the url points to a Jellyfin server.
                var systemInfo = await _jellyfinApiClient.System.Info.Public.GetAsync();
                Console.WriteLine($"Connected to {serverUrl}");
                Console.WriteLine($"Server Name: {systemInfo.ServerName}");
                Console.WriteLine($"Server Version: {systemInfo.Version}");
            }
            catch (Exception ex)
            {
                UpdateErrorMessage("We're unable to connect to the selected server right now. Please ensure it is running and try again.");
                Console.Error.WriteLine($"Error connecting to {serverUrl}: {ex.Message}");
                return;
            }

            // Save the Url in settings
            _appSettings.ServerUrl = serverUrl;

            App.AppFrame.Navigate(typeof(Login));
        }
        finally
        {
            IsInteractable = true;
        }
    }

    private void UpdateErrorMessage(string message)
    {
        ShowErrorMessage = true;
        ErrorMessage = message;
    }
}