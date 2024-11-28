using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;

namespace Jellyfin.Views;

public sealed partial class LoginViewModel : ObservableObject
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
    private string _userName;

    [ObservableProperty]
    private string _password;

    [ObservableProperty]
    private bool _rememberMe;

    public LoginViewModel(
        AppSettings appSettings,
        JellyfinSdkSettings sdkClientSettings,
        JellyfinApiClient jellyfinApiClient,
        NavigationManager navigationManager)
    {
        _appSettings = appSettings;
        _sdkClientSettings = sdkClientSettings;
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        IsInteractable = true;
    }

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
                // TODO: Save creds separately for each server
                _appSettings.AccessToken = accessToken;
            }

            _sdkClientSettings.SetAccessToken(accessToken);

            Console.WriteLine("Authentication success.");

            _navigationManager.NavigateToHome();

            // After signing in, disallow accidentally coming back here.
            _navigationManager.ClearHistory();
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
        _navigationManager.NavigateToServerSelection();
        _navigationManager.ClearHistory();
    }
}
