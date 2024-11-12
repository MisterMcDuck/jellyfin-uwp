using System;
using Jellyfin.Views;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Services;

public sealed class NavigationManager
{
    public Frame AppFrame => (Frame)Window.Current.Content;

    public void Initialize()
    {
        SystemNavigationManager.GetForCurrentView().BackRequested += BackRequested;
        Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += AcceleratorKeyActivated;
        Window.Current.CoreWindow.PointerPressed += PointerPressed;
    }

    public void NavigateToServerSelection()
    {
        AppFrame.Navigate(typeof(ServerSelection));
    }

    public void NavigateToLogin()
    {
        AppFrame.Navigate(typeof(Login));
    }

    public void NavigateToHome()
    {
        AppFrame.Navigate(typeof(Home));
    }

    public void NavigateToMovies(Guid id)
    {
        AppFrame.Navigate(typeof(Movies), id);
    }

    public void NavigateToItemDetails(Guid id)
    {
        AppFrame.Navigate(typeof(ItemDetails), id);
    }

    public void NavigateToVideo(Guid id)
    {
        AppFrame.Navigate(typeof(Video), id);
    }

    /// <summary>
    /// Indicates whether or not a back navigation can occur.
    /// </summary>
    /// <returns>True if a back navigation can occur else false.</returns>
    public bool CanGoBack()
    {
        if (AppFrame == null)
        {
            return false;
        }
        else
        {
            return AppFrame.CanGoBack;
        }
    }

    /// <summary>
    /// Indicates whether or not a forward navigation can occur.
    /// </summary>
    /// <returns>True if a forward navigation can occur else false.</returns>
    public bool CanGoForward()
    {
        if (AppFrame == null)
        {
            return false;
        }
        else
        {
            return AppFrame.CanGoForward;
        }
    }

    /// <summary>
    /// Navigates back one page.
    /// </summary>
    /// <returns>True if a back navigation occurred else false.</returns>
    public bool GoBack()
    {
        if (AppFrame.CanGoBack)
        {
            AppFrame.GoBack();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Navigates forward one page.
    /// </summary>
    /// <returns>True if a forward navigation occurred else false.</returns>
    public bool GoForward()
    {
        if (AppFrame.CanGoForward)
        {
            AppFrame.GoForward();
            return true;
        }

        return false;
    }

    private void BackRequested(object sender, BackRequestedEventArgs e)
    {
        e.Handled = GoBack();
    }

    /// <summary>
    /// Invoked on every keystroke, including system keys such as Alt key combinations, when
    /// this page is active and occupies the entire window.  Used to detect keyboard navigation
    /// between pages even when the page itself doesn't have focus.
    /// </summary>
    /// <param name="sender">Instance that triggered the event.</param>
    /// <param name="e">Event data describing the conditions that led to the event.</param>
    private void AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs e)
    {
        var virtualKey = e.VirtualKey;

        // Only investigate further when Left, Right, or the dedicated Previous or Next keys
        // are pressed
        if ((e.EventType == CoreAcceleratorKeyEventType.SystemKeyDown
            || e.EventType == CoreAcceleratorKeyEventType.KeyDown) &&
            (
            virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right
            || virtualKey == VirtualKey.GoBack || virtualKey == VirtualKey.GoForward
            )
            )
        {
            var coreWindow = Window.Current.CoreWindow;
            var downState = CoreVirtualKeyStates.Down;
            bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
            bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
            bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
            bool noModifiers = !menuKey && !controlKey && !shiftKey;
            bool onlyAlt = menuKey && !controlKey && !shiftKey;

            if ((virtualKey == VirtualKey.GoBack && noModifiers) || (virtualKey == VirtualKey.Left && onlyAlt))
            {
                e.Handled = GoBack();
            }
            else if ((virtualKey == VirtualKey.GoForward && noModifiers) || (virtualKey == VirtualKey.Right && onlyAlt))
            {
                e.Handled = GoForward();
            }
        }
    }

    /// <summary>
    /// Invoked on every mouse click, touch screen tap, or equivalent interaction when this
    /// page is active and occupies the entire window.  Used to detect browser-style next and
    /// previous mouse button clicks to navigate between pages.
    /// </summary>
    /// <param name="sender">Instance that triggered the event.</param>
    /// <param name="e">Event data describing the conditions that led to the event.</param>
    private void PointerPressed(CoreWindow sender, PointerEventArgs e)
    {
        PointerPointProperties properties = e.CurrentPoint.Properties;

        // Ignore button chords with the left, right, and middle buttons
        if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed || properties.IsMiddleButtonPressed)
        {
            return;
        }

        // If back or forward are pressed (but not both) navigate appropriately
        bool backPressed = properties.IsXButton1Pressed;
        bool forwardPressed = properties.IsXButton2Pressed;
        if (backPressed ^ forwardPressed)
        {
            if (backPressed)
            {
                e.Handled = GoBack();
            }

            if (forwardPressed)
            {
                e.Handled = GoForward();
            }
        }
    }
}