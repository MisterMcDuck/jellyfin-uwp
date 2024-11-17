﻿using System;
using Jellyfin.Sdk;
using Jellyfin.Services;
using Jellyfin.Views;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
sealed partial class App : Application
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly NavigationManager _navigationManager;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        // TODO: Is there a better way to do DI in UWP?
        _appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();
        _sdkClientSettings = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinSdkSettings>();
        _navigationManager = AppServices.Instance.ServiceProvider.GetRequiredService<NavigationManager>();

        InitializeComponent();

        Current.RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;

        Suspending += OnSuspending;
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        Frame rootFrame = Window.Current.Content as Frame;

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (rootFrame == null)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
                ApplicationViewScaling.TrySetDisableLayoutScaling(true);
            }
            else
            {
                // Xbox always renders at 1920 x 1080, so emulate that for a consistency when testing on Windows.
                ApplicationView.PreferredLaunchViewSize = new Size(1920, 1080);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            }

            if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                //TODO: Load state from previously suspended application
            }

            // Place the frame in the current Window
            Window.Current.Content = rootFrame;
        }

        if (!e.PrelaunchActivated)
        {
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (_appSettings.ServerUrl is null)
                {
                    rootFrame.Navigate(typeof(ServerSelection), e.Arguments);
                }
                else
                {
                    // TODO: Validate the server is still reachable
                    _sdkClientSettings.SetServerUrl(_appSettings.ServerUrl);

                    if (_appSettings.AccessToken is null)
                    {
                        rootFrame.Navigate(typeof(Login), e.Arguments);
                    }
                    else
                    {
                        // TODO: Validate the access token is still valid
                        _sdkClientSettings.SetAccessToken(_appSettings.AccessToken);

                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                }
            }

            _navigationManager.Initialize(rootFrame);

            // TODO: REMOVE! Do properly
            _ = new DeviceProfileManager().InitializeAsync();

            // Ensure the current window is active
            Window.Current.Activate();
        }
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }

    /// <summary>
    /// Invoked when application execution is being suspended.  Application state is saved
    /// without knowing whether the application will be terminated or resumed with the contents
    /// of memory still intact.
    /// </summary>
    /// <param name="sender">The source of the suspend request.</param>
    /// <param name="e">Details about the suspend request.</param>
    private void OnSuspending(object sender, SuspendingEventArgs e)
    {
        var deferral = e.SuspendingOperation.GetDeferral();
        //TODO: Save application state and stop any background activity
        deferral.Complete();
    }
}
