using System;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.Views;

public sealed class LoginViewModel : BindableBase
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;

    public LoginViewModel(AppSettings appSettings, JellyfinSdkSettings sdkClientSettings, JellyfinApiClient jellyfinApiClient)
    {
        _appSettings = appSettings;
        _sdkClientSettings = sdkClientSettings;
        _jellyfinApiClient = jellyfinApiClient;

        IsInteractable = true;
    }

    public bool IsInteractable { get; set => SetProperty(ref field, value); }

    public string ErrorMessage { get; set => SetProperty(ref field, value); }

    public bool ShowErrorMessage { get; set => SetProperty(ref field, value); }

    public string UserName { get; set => SetProperty(ref field, value); }

    public string Password { get; set => SetProperty(ref field, value); }

    public bool RememberMe { get; set => SetProperty(ref field, value); }

    public async void SignIn()
    {
        IsInteractable = false;
        ShowErrorMessage = false;

        try
        {
            Console.WriteLine($"Logging into {_sdkClientSettings.ServerUrl}");

            AuthenticateUserByName request = new()
            {
                Username = UserName,
                Pw = Password,
            };
            AuthenticationResult authenticationResult = await _jellyfinApiClient.Users.AuthenticateByName.PostAsync(request);

            string accessToken = authenticationResult.AccessToken;

            if (RememberMe)
            {
                _appSettings.AccessToken = accessToken;
            }

            _sdkClientSettings.SetAccessToken(accessToken);

            Console.WriteLine("Authentication success.");

            App.AppFrame.Navigate(typeof(Home));
        }
        catch (Exception ex)
        {
            // TODO: Need a friendlier message.
            ErrorMessage = ex.Message;
            ShowErrorMessage = true;
        }
        finally
        {
            IsInteractable = true;
        }
    }

    public void ChangeServer()
    {
        App.AppFrame.Navigate(typeof(ServerSelection));
    }
}