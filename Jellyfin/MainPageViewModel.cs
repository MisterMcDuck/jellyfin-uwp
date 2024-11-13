using System;
using System.Collections.ObjectModel;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Windows.UI.Xaml.Controls;

namespace Jellyfin;

public sealed class MainPageViewModel : BindableBase
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;
    private readonly Frame _contentFrame;

    public MainPageViewModel(
        AppSettings appSettings,
        JellyfinApiClient jellyfinApiClient,
        NavigationManager navigationManager,
        Frame contentFrame)
    {
        _appSettings = appSettings;
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;
        _contentFrame = contentFrame;

        navigationManager.RegisterContentFrame(contentFrame);

        InitializeNavigationItems();
    }

    public void HandleParameters(MainPage.Parameters parameters)
    {
        if (parameters is not null)
        {
            parameters.DeferredNavigationAction();
        }
        else
        {
            // Default to home
            _navigationManager.NavigateToHome();
        }
    }

    public ObservableCollection<NavigationViewItemBase> NavigationItems { get; } = new();

    public void NavigationItemSelected(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is Action navigationAction)
        {
            navigationAction();
        }
    }

    private async void InitializeNavigationItems()
    {
        NavigationItems.Add(new NavigationViewItem
        {
            Content = "Home",
            Icon = new SymbolIcon(Symbol.Home),
            Tag = () => _navigationManager.NavigateToHome(),
            XYFocusRight = _contentFrame,
        });

        BaseItemDtoQueryResult result = await _jellyfinApiClient.UserViews.GetAsync();
        if (result.Items.Count > 0)
        {
            NavigationItems.Add(new NavigationViewItemHeader { Content = "Media" });
        }

        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            Guid itemId = item.Id.Value;

            // TODO: Create a better abstraction for this!
            if (item.CollectionType.HasValue && item.CollectionType.Value == BaseItemDto_CollectionType.Movies)
            {
                NavigationItems.Add(new NavigationViewItem
                {
                    Content = item.Name,
                    Icon = new SymbolIcon(Symbol.Library),
                    Tag = () => _navigationManager.NavigateToMovies(itemId),
                    XYFocusRight = _contentFrame,
                });
            }
        }

        NavigationItems.Add(new NavigationViewItemHeader { Content = "User" });

        NavigationItems.Add(new NavigationViewItem
        {
            Content = "Select Server",
            Icon = new SymbolIcon(Symbol.Switch),
            Tag = () => _navigationManager.NavigateToServerSelection(),
            XYFocusRight = _contentFrame,
        });

        NavigationItems.Add(new NavigationViewItem
        {
            Content = "Sign Out",
            Icon = new SymbolIcon(Symbol.BlockContact),
            Tag = () =>
            {
                _appSettings.AccessToken = null;
                _navigationManager.NavigateToLogin();

                // After signing out, disallow going back to a logged-in page.
                _navigationManager.ClearHistory();
            },
            XYFocusRight = _contentFrame,
        });
    }
}