using System;
using Jellyfin.Core;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class Login : Page
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;

    public Login()
    {
        // TODO: Is there a better way to do DI in UWP?
        _appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();
        _sdkClientSettings = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinSdkSettings>();
        _jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();

        InitializeComponent();
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        UserText.IsEnabled = false;
        PasswordText.IsEnabled = false;
        RememberMeCheckBox.IsEnabled = false;
        SignInButton.IsEnabled = false;
        ErrorText.Visibility = Visibility.Collapsed;

        try
        {
            string username = UserText.Text;
            string password = PasswordText.Password;

            Console.WriteLine($"Logging into {_sdkClientSettings.ServerUrl}");

            AuthenticateUserByName request = new()
            {
                Username = username,
                Pw = password,
            };
            AuthenticationResult authenticationResult = await _jellyfinApiClient.Users.AuthenticateByName.PostAsync(request);

            string accessToken = authenticationResult.AccessToken;

            if (RememberMeCheckBox.IsChecked.GetValueOrDefault())
            {
                _appSettings.AccessToken = accessToken;
            }

            _sdkClientSettings.SetAccessToken(accessToken);

            Console.WriteLine("Authentication success.");

            Frame.Navigate(typeof(Home));
        }
        catch (Exception ex)
        {
            ErrorText.Visibility = Visibility.Visible;

            // TODO: Need a friendlier message.
            ErrorText.Text = ex.Message;
        }
        finally
        {
            UserText.IsEnabled = true;
            PasswordText.IsEnabled = true;
            RememberMeCheckBox.IsEnabled = true;
            SignInButton.IsEnabled = true;
        }
    }

    private void ChangeServerButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ServerSelection));
    }
}