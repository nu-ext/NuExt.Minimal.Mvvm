using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides extension methods for handling asynchronous commands.
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Notifies that the <see cref="ICommand.CanExecute"/> property has changed.
        /// </summary>
        public static void NotifyCanExecuteChanged(this IRelayCommand command)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(command);
#else
            _ = command ?? throw new ArgumentNullException(nameof(command));
#endif
            command.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Waits asynchronously until the specified command has finished executing.
        /// </summary>
        /// <param name="command">The asynchronous command to wait for.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="command"/> is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the wait is canceled.</exception>
        public static async Task WaitAsync(this IAsyncCommand command, CancellationToken cancellationToken = default)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(command);
#else
            _ = command ?? throw new ArgumentNullException(nameof(command));
#endif
            if (command is not INotifyPropertyChanged npc || command.IsExecuting == false)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(IAsyncCommand.IsExecuting) && command.IsExecuting == false)
                {
                    tcs.TrySetResult(true);
                }
            }

            npc.PropertyChanged += OnPropertyChanged;
            try
            {
                if (command.IsExecuting == false)
                {
                    tcs.TrySetResult(true);
                }

                using (cancellationToken.CanBeCanceled
                           ? cancellationToken
#if NET
                           .UnsafeRegister(static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(), tcs)
#else
                           .Register(static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(), tcs, useSynchronizationContext: false)
#endif
                           : null as IDisposable)
                {
                    await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                npc.PropertyChanged -= OnPropertyChanged;
            }
        }
    }
}
