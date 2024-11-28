using Jellyfin.Services;
using Jellyfin.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Commands;

public sealed class NavigateToViewCommand : CommandBase
{
    private readonly NavigationManager _navigationManager;

    public NavigateToViewCommand()
    {
        _navigationManager = AppServices.Instance.ServiceProvider.GetRequiredService<NavigationManager>();
    }

    public override bool CanExecute(object parameter) => true;

    public override void Execute(object parameter)
    {
        // TODO: Create a better abstraction for this
        if (parameter is UserView userView)
        {
            userView.Navigate(_navigationManager);
        }
        else if (parameter is Movie movie)
        {
            movie.Navigate(_navigationManager);
        }
    }
}