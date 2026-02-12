using System.Windows.Input;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Extends <see cref="ICommand"/> with execution state tracking and explicit change notification.
    /// </summary>
    public interface IRelayCommand : ICommand
    {
        /// <summary>
        /// Gets a value indicating whether an execution of the command is in progress.
        /// </summary>
        /// <value><see langword="true"/> if the command is executing; otherwise, <see langword="false"/>.</value>
        bool IsExecuting { get; }

        /// <summary>
        /// Raises the <see cref="ICommand.CanExecuteChanged"/> event.
        /// </summary>
        /// <remarks>
        /// Call this method when conditions affecting the command's ability to execute change.
        /// This typically causes UI elements bound to the command to requery <see cref="ICommand.CanExecute"/>.
        /// </remarks>
        void RaiseCanExecuteChanged();
    }
}
