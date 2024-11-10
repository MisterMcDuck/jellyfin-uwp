using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Behaviors;

/// <summary>
/// Focuses a control on the loaded event.
/// </summary>
public class FocusOnLoadBehavior : Behavior<Control>
{
    protected override void OnAttached()
    {
        AssociatedObject.Loaded += Loaded;
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= Loaded;
        base.OnDetaching();
    }

    private void Loaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Focus(FocusState.Programmatic);
    }
}