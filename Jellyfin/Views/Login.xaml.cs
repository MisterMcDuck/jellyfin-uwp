using System;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class Login : Page
{
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;

    public Login()
    {
        // TODO: Is there a better way to do DI in UWP?
        _sdkClientSettings = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinSdkSettings>();
        _jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();

        InitializeComponent();
    }

    private async void SignIn_Click(object sender, RoutedEventArgs e)
    {
        SignIn.IsEnabled = false;
        Error.Visibility = Visibility.Collapsed;

        try
        {
            string username = User.Text;
            string password = Password.Password;

            Console.WriteLine($"Logging into {_sdkClientSettings.ServerUrl}");

            AuthenticateUserByName request = new()
            {
                Username = username,
                Pw = password,
            };
            AuthenticationResult authenticationResult = await _jellyfinApiClient.Users.AuthenticateByName.PostAsync(request);

            _sdkClientSettings.SetAccessToken(authenticationResult.AccessToken);
            UserDto userDto = authenticationResult.User;
            Console.WriteLine("Authentication success.");

            Frame.Navigate(typeof(Home));
        }
        catch (Exception ex)
        {
            Error.Visibility = Visibility.Visible;

            // TODO: Need a friendlier message.
            Error.Text = ex.Message;
        }
        finally
        {
            SignIn.IsEnabled = true;
        }
    }

    private void ChangeServer_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ServerSelection));
    }
}