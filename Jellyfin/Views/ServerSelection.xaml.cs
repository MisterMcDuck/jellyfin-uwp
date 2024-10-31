using System;
using Jellyfin.Core;
using Jellyfin.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Jellyfin.Views;

public sealed partial class ServerSelection : Page
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;

    public ServerSelection()
    {
        // TODO: Is there a better way to do DI in UWP?
        _appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();
        _sdkClientSettings = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinSdkSettings>();
        _jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();

        InitializeComponent();
        Loaded += ServerSelection_Loaded;

        if (_appSettings.ServerUrl is not null)
        {
            ServerUrlText.Text = _appSettings.ServerUrl;
        }
    }

    private void ServerUrlText_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ConnectButton_Click(ConnectButton, null);
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectButton.IsEnabled = false;
        try
        {
            ErrorText.Visibility = Visibility.Collapsed;

            string serverUrl = ServerUrlText.Text;

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
                UpdateErrorMessage($"Error connecting to {serverUrl}: {ex.Message}");
                return;
            }

            // Save the Url in settings
            _appSettings.ServerUrl = serverUrl;

            Frame.Navigate(typeof(Login));
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private void ServerSelection_Loaded(object sender, RoutedEventArgs e)
    {
        ServerUrlText.Focus(FocusState.Programmatic);
    }

    private void UpdateErrorMessage(string message)
    {
        ErrorText.Visibility = Visibility.Visible;
        ErrorText.Text = message;
    }
}