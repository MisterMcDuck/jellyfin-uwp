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
    private Frame _appFrame;

    private Frame _contentFrame;

    private object _currentAppParameter;

    private object _currentContentParameter;

    public void Initialize(Frame appFrame)
    {
        if (appFrame is null)
        {
            throw new ArgumentNullException(nameof(appFrame));
        }

        if (_appFrame is not null)
        {
            throw new InvalidOperationException("Frame already initialized.");
        }

        _appFrame = appFrame;

        SystemNavigationManager.GetForCurrentView().BackRequested += BackRequested;
        Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += AcceleratorKeyActivated;
        Window.Current.CoreWindow.PointerPressed += PointerPressed;
    }

    public void RegisterContentFrame(Frame contentFrame)
    {
        if (contentFrame is null)
        {
            throw new ArgumentNullException(nameof(contentFrame));
        }

        if (_contentFrame is not null)
        {
            throw new InvalidOperationException("Cannot register content frame when one is already registered.");
        }

        _contentFrame = contentFrame;
    }

    public void ClearHistory()
    {
        _contentFrame?.BackStack.Clear();
        _contentFrame?.ForwardStack.Clear();
        _appFrame.BackStack.Clear();
        _appFrame.ForwardStack.Clear();
    }

    public void NavigateToServerSelection() => NavigateAppFrame<ServerSelection>();

    public void NavigateToLogin() => NavigateAppFrame<Login>();

    public void NavigateToHome() => NavigateContentFrame<Home>();

    public void NavigateToMovies(Guid id) => NavigateContentFrame<Movies>(new Movies.Parameters(id));

    public void NavigateToItemDetails(Guid id) => NavigateContentFrame<ItemDetails>(new ItemDetails.Parameters(id));

    public void NavigateToVideo(Guid id) => NavigateContentFrame<Video>(new Video.Parameters(id));

    private void NavigateAppFrame<TPage>(object parameter = null)
        where TPage : Page
    {
        _contentFrame = null;
        _currentContentParameter = null;
        NavigateFrame<TPage>(_appFrame, ref _currentAppParameter, parameter);
    }

    private void NavigateContentFrame<TPage>(object parameter = null)
    {
        if (_contentFrame is null)
        {
            MainPage.Parameters mainPageParameters = new(DeferredNavigationAction: () => NavigateFrame<TPage>(_contentFrame, ref _currentContentParameter, parameter));
            NavigateFrame<MainPage>(_appFrame, ref _currentAppParameter, mainPageParameters);
        }
        else
        {
            NavigateFrame<TPage>(_contentFrame, ref _currentContentParameter, parameter);
        }
    }

    private static void NavigateFrame<TPage>(Frame frame, ref object currentParameter, object parameter = null)
    {
        // Only navigate if the selected page isn't currently loaded.
        Type pageType = typeof(TPage);
        if (frame.CurrentSourcePageType == pageType && Equals(currentParameter, parameter))
        {
            return;
        }

        currentParameter = parameter;
        frame.Navigate(pageType, parameter);
    }

    /// <summary>
    /// Indicates whether or not a back navigation can occur.
    /// </summary>
    /// <returns>True if a back navigation can occur else false.</returns>
    public bool CanGoBack() => (_contentFrame is not null && _contentFrame.CanGoBack) || _appFrame.CanGoBack;

    /// <summary>
    /// Indicates whether or not a forward navigation can occur.
    /// </summary>
    /// <returns>True if a forward navigation can occur else false.</returns>
    public bool CanGoForward() => (_contentFrame is not null && _contentFrame.CanGoForward) || _appFrame.CanGoForward;

    /// <summary>
    /// Navigates back one page.
    /// </summary>
    /// <returns>True if a back navigation occurred else false.</returns>
    public bool GoBack()
    {
        if (_contentFrame is not null && _contentFrame.CanGoBack)
        {
            _contentFrame.GoBack();
            return true;
        }

        if (_appFrame.CanGoBack)
        {
            _appFrame.GoBack();
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
        if (_contentFrame is not null && _contentFrame.CanGoForward)
        {
            _contentFrame.GoForward();
            return true;
        }

        if (_appFrame.CanGoForward)
        {
            _appFrame.GoForward();
            return true;
        }

        return false;
    }

    private void BackRequested(object sender, BackRequestedEventArgs e) => e.Handled = GoBack();

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