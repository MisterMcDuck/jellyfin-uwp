using System;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;

namespace Jellyfin.Views;

public sealed class LoginViewModel : BindableBase
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    // Backing fields for the properties
    private bool _isInteractable;
    private string _errorMessage;
    private bool _showErrorMessage;
    private string _userName;
    private string _password;
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

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
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
            _appSettings.SessionId = authenticationResult.SessionInfo.Id;

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
