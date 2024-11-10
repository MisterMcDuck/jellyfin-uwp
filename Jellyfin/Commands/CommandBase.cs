using System.Windows.Input;
using System;

namespace Jellyfin.Commands;

/// <summary>
/// Base class for commands.
/// </summary>
public abstract class CommandBase : ICommand
{
    /// <summary>
    /// Raised when RaiseCanExecuteChanged is called.
    /// </summary>
    public event EventHandler CanExecuteChanged;

    /// <summary>
    /// Method used to raise the <see cref="CanExecuteChanged"/> event
    /// to indicate that the return value of the <see cref="CanExecute"/>
    /// method has changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        var handler = CanExecuteChanged;
        if (handler is not null)
        {
            handler(this, EventArgs.Empty);
        }
    }

    public abstract bool CanExecute(object parameter);

    public abstract void Execute(object parameter);
}