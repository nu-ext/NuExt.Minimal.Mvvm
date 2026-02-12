using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Represents a strongly-typed asynchronous command with cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <remarks>
    /// This interface combines <see cref="IAsyncCommand"/> and <see cref="IRelayCommand{T}"/> to provide
    /// asynchronous execution and type-safe parameter handling.
    /// </remarks>
    public interface IAsyncCommand<in T> : IAsyncCommand, IRelayCommand<T>
    {
        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        /// <remarks>
        /// Type-safe equivalent of <see cref="IAsyncCommand.ExecuteAsync(object?)"/>.
        /// Must follow the same cancellation and exception propagation rules.
        /// </remarks>
        Task ExecuteAsync(T parameter);

        /// <summary>
        /// Executes the command asynchronously with a cancellation token.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        /// <remarks>
        /// Type-safe equivalent of <see cref="IAsyncCommand.ExecuteAsync(object?, CancellationToken)"/>.
        /// Must follow the same cancellation and exception propagation rules.
        /// </remarks>
        Task ExecuteAsync(T parameter, CancellationToken cancellationToken);
    }
}
