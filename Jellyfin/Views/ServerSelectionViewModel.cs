using System;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Services;

namespace Jellyfin.Views;

public sealed class ServerSelectionViewModel : BindableBase
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    // Backing fields for the properties
    private bool _isInteractable;
    private string _errorMessage;
    private bool _showErrorMessage;
    private string _serverUrl;

    public ServerSelectionViewModel(
        AppSettings appSettings,
        JellyfinSdkSettings sdkClientSettings,
        JellyfinApiClient jellyfinApiClient,
        NavigationManager navigationManager)
    {
        _appSettings = appSettings;
        _sdkClientSettings = sdkClientSettings;
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        if (_appSettings.ServerUrl is not null)
        {
            ServerUrl = _appSettings.ServerUrl;
        }

        IsInteractable = true;
    }

    public bool IsInteractable
    {
        get => _isInteractable;
        set => SetProperty(ref _isInteractable, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool ShowErrorMessage
    {
        get => _showErrorMessage;
        set => SetProperty(ref _showErrorMessage, value);
    }

    public string ServerUrl
    {
        get => _serverUrl;
        set => SetProperty(ref _serverUrl, value);
    }

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
                UpdateErrorMessage($"Invalid URL: {serverUrl}");
                return;
            }

            _sdkClientSettings.SetServerUrl(serverUrl);
            try
            {
                // Get public system info to verify that the URL points to a Jellyfin server.
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

            // Save the URL in settings
            _appSettings.ServerUrl = serverUrl;

            // TODO: Go directly home if there are saved creds
            _navigationManager.NavigateToLogin();
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
