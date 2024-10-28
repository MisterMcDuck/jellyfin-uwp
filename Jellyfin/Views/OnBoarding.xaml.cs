﻿using System;
using Jellyfin.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Jellyfin.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnBoarding : Page
    {
        public OnBoarding()
        {
            this.InitializeComponent();
            this.Loaded += OnBoarding_Loaded;
            btnConnect.Click += BtnConnect_Click;
            txtUrl.KeyUp += TxtUrl_KeyUp;
        }

        private void TxtUrl_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                BtnConnect_Click(btnConnect, null);
            }
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            btnConnect.IsEnabled = false;
            txtError.Visibility = Visibility.Collapsed;

            string serverUrl = txtUrl.Text;
            if (!Uri.IsWellFormedUriString(serverUrl, UriKind.Absolute))
            {
                UpdateErrorMessage($"Invalid url: {txtUrl.Text}");
                return;
            }

            JellyfinService.Instance.SetServerUrl(serverUrl);
            try
            {
                // Get public system info to verify that the url points to a Jellyfin server.
                var systemInfo = await JellyfinService.Instance.Client.System.Info.Public.GetAsync();
                Console.WriteLine($"Connected to {serverUrl}");
                Console.WriteLine($"Server Name: {systemInfo.ServerName}");
                Console.WriteLine($"Server Version: {systemInfo.Version}");
            }
            catch (InvalidOperationException ex)
            {
                UpdateErrorMessage($"Invalid url: {ex.Message}");
                return;
            }
            catch (SystemException ex)
            {
                UpdateErrorMessage($"Error connecting to {serverUrl}: {ex.Message}");
                return;
            }

            // Save the Url in settings
            Central.Settings.JellyfinServer = serverUrl;

            Frame.Navigate(typeof(MainPage));

            btnConnect.IsEnabled = true;
        }

        private void OnBoarding_Loaded(object sender, RoutedEventArgs e)
        {
            txtUrl.Focus(FocusState.Programmatic);
        }

        private void UpdateErrorMessage(string message)
        {
            txtError.Visibility = Visibility.Visible;
            txtError.Text = message;
        }
    }
}