using Jellyfin.Views;

namespace Jellyfin.Commands;

public sealed class NavigateToViewCommand : CommandBase
{
    public override bool CanExecute(object parameter) => true;

    public override void Execute(object parameter)
    {
        // TODO: Create a better abstraction for this
        if (parameter is UserView userView)
        {
            userView.Select();
        }
    }
}