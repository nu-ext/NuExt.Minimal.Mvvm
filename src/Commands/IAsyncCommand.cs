using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Represents an asynchronous command that supports cancellation and error handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="IRelayCommand"/> to support asynchronous execution,
    /// cancellation, and surfacing exceptions from the <see cref="ICommand.Execute(object)"/> path.
    /// </para>
    /// <para>
    /// Exceptions thrown from <see cref="ExecuteAsync(object?)"/> and
    /// <see cref="ExecuteAsync(object?, CancellationToken)"/> are propagated via the returned <see cref="Task"/>.
    /// Exceptions (other than <see cref="OperationCanceledException"/>) thrown from the fire-and-forget
    /// <see cref="ICommand.Execute(object)"/> path must be raised via <see cref="UnhandledException"/>.
    /// </para>
    /// <para>
    /// Threading: <see cref="UnhandledException"/> may be raised from any thread. Subscribers must marshal
    /// to their target context if needed.
    /// </para>
    /// </remarks>
    public interface IAsyncCommand : IRelayCommand
    {
        /// <summary>
        /// Occurs when an exception is thrown during <see cref="ICommand.Execute(object)"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This event surfaces exceptions thrown by the fire-and-forget <see cref="ICommand.Execute(object)"/> path,
        /// excluding <see cref="OperationCanceledException"/>.
        /// </para>
        /// <para>
        /// This event may be raised from any thread; subscribers are responsible for marshaling.
        /// </para>
        /// </remarks>
        event EventHandler<UnhandledCommandExceptionEventArgs>? UnhandledException;

        /// <summary>
        /// Cancels all currently executing operations for this command.
        /// </summary>
        /// <remarks>
        /// Thread-safe and idempotent.
        /// </remarks>
        void Cancel();

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        Task ExecuteAsync(object? parameter);

        /// <summary>
        /// Executes the command asynchronously with a cancellation token.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">
        /// The operation was canceled via <see cref="Cancel"/>, <paramref name="cancellationToken"/>, or both.
        /// </exception>
        Task ExecuteAsync(object? parameter, CancellationToken cancellationToken);
    }
}
