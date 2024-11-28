using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Services;

namespace Jellyfin.Views;

public sealed partial class ServerSelectionViewModel : ObservableValidator
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    private bool _isInteractable;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private bool _showErrorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [Required(AllowEmptyStrings = false)]
    [NotifyDataErrorInfo]
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

    private bool CanConnect() => !string.IsNullOrWhiteSpace(ServerUrl);

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        IsInteractable = false;
        ShowErrorMessage = false;
        try
        {
            ValidateAllProperties();
            if (HasErrors)
            {
                UpdateErrorMessage("A Server URL is required");
                return;
            }

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

            // Once we directly navigate home (see above), disallow accidentally coming back here.
            ////_navigationManager.ClearBackStack();
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
