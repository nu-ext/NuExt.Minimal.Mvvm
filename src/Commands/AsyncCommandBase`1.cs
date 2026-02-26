using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides a base class for implementing asynchronous commands with cancellation support
    /// and execution state tracking.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <remarks>
    /// <para>
    /// Extends <see cref="CommandBase{T}"/> to support asynchronous execution with proper cancellation
    /// and exception handling. The synchronous <see cref="ICommand.Execute(object)"/> entry point is implemented
    /// as an <c>async void</c> wrapper that forwards exceptions (excluding <see cref="OperationCanceledException"/>)
    /// via <see cref="UnhandledException"/>.
    /// </para>
    /// <para>
    /// Threading: events and exceptions may be raised from arbitrary threads. Consumers are responsible for marshaling 
    /// when invoking commands or handling notifications from non‑UI threads.
    /// </para>
    /// </remarks>
    public abstract class AsyncCommandBase<T> : CommandBase<T>, IAsyncCommand<T>
    {
        internal readonly ConcurrentDictionary<int, Cancelable> ExecutingTasks = new();

        #region Properties

        /// <summary>
        /// Gets or initializes a value indicating whether asynchronous continuations are marshaled 
        /// back to the captured context in the <see cref="ExecuteAsync(T, CancellationToken)"/> method.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to marshal continuations back to the original context; 
        /// otherwise, <see langword="false"/>. The default is <see langword="true"/> for UI compatibility.
        /// </value>
        /// <remarks>
        /// When <see langword="true"/>, property change notifications and <see cref="ICommand.CanExecuteChanged"/>
        /// events will be raised on the original context (typically the UI thread).
        /// Set to <see langword="false"/> for optimal performance in background scenarios.
        /// </remarks>
        public bool ContinueOnCapturedContext { get; init; } = true;

        #endregion

        #region Events

        ///<inheritdoc/>
        public event EventHandler<UnhandledCommandExceptionEventArgs>? UnhandledException;

        #endregion

        #region Methods

        ///<inheritdoc/>
        public void Cancel()
        {
            foreach (var pair in ExecutingTasks)
            {
                pair.Value.Cancel();
            }
        }

        /// <inheritdoc/>
        Task IAsyncCommand.ExecuteAsync(object? parameter)
        {
            return ExecuteAsync(GetCommandParameter(parameter));
        }

        /// <inheritdoc/>
        Task IAsyncCommand.ExecuteAsync(object? parameter, CancellationToken cancellationToken)
        {
            return ExecuteAsync(GetCommandParameter(parameter), cancellationToken);
        }

        /// <summary>
        /// Executes the command as an asynchronous operation for ICommand compatibility.
        /// Unhandled exceptions are routed to the <see cref="UnhandledException"/> event.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <remarks>
        /// <para>This method implements <see cref="ICommand.Execute(object?)"/> as an async void method.
        /// It initiates the asynchronous operation and returns immediately without waiting for completion.</para>
        /// <para>For proper exception handling and cancellation support, use 
        /// <see cref="ExecuteAsync(T, CancellationToken)"/> instead.</para>
        /// <para>Exceptions thrown during execution that are not <see cref="OperationCanceledException"/>
        /// are raised via the <see cref="UnhandledException"/> event; if not handled there, they are forwarded to 
        /// <see cref="AsyncCommand.GlobalUnhandledException"/>.</para>
        /// </remarks>
        public override async void Execute(T parameter)
        {
            try
            {
                await ExecuteAsync(parameter, CancellationToken.None).ConfigureAwait(ContinueOnCapturedContext);
            }
            catch (OperationCanceledException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                //do not throw in async void
                OnUnhandledException(ex);
            }
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(T parameter)
        {
            return ExecuteAsync(parameter, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync(T parameter, CancellationToken cancellationToken)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            using var cancelable = RegisterCancelable();

            using var scope = BeginExecutionScope();
            if (!scope.Started) return;

            cancellationToken.ThrowIfCancellationRequested();
            cancelable.ThrowIfCancellationRequested();

            using var cts = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();
            cancelable.Source = cts;

            cts.Token.ThrowIfCancellationRequested();
            cancelable.ThrowIfCancellationRequested();

            try
            {
                await ExecuteAsyncCore(parameter, cts.Token).ConfigureAwait(ContinueOnCapturedContext);
            }
            finally
            {
                cancelable.Source = null;
            }
        }

        /// <summary>
        /// When implemented in a derived class, executes the command's logic asynchronously.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <param name="cancellationToken">
        /// A cancellation token that should be observed to cancel the operation.
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>This method is called by <see cref="ExecuteAsync(T, CancellationToken)"/> 
        /// after all pre-execution checks have passed.</para>
        /// <para>The provided <paramref name="cancellationToken"/> is linked to both the
        /// external cancellation token (if any) and the command's cancellation mechanism.</para>
        /// <para>Implementations should throw <see cref="OperationCanceledException"/> 
        /// when the operation is canceled.</para>
        /// </remarks>
        protected abstract Task ExecuteAsyncCore(T parameter, CancellationToken cancellationToken);

        private Cancelable RegisterCancelable()
        {
            var taskId = IdGenerator.NewId();
            var cancelable = new Cancelable(taskId, this);
            bool result = ExecutingTasks.TryAdd(taskId, cancelable);
            Debug.Assert(result, "Failed to register execution.");
            return cancelable;
        }

        private void UnregisterCancelable(int taskId)
        {
            bool result = ExecutingTasks.TryRemove(taskId, out _);
            Debug.Assert(result, "Failed to unregister execution.");
        }

        /// <summary>
        /// Raises the <see cref="UnhandledException"/> event for exceptions from 
        /// <see cref="ICommand.Execute"/>.
        /// </summary>
        /// <param name="exception">The exception thrown from <see cref="ICommand.Execute"/>.</param>
        /// <remarks>
        /// Called exclusively from the <c>async void</c> implementation of <see cref="ICommand.Execute(object?)"/>
        /// to handle exceptions that cannot be propagated due to the void return type.
        /// </remarks>
        private void OnUnhandledException(Exception exception)
        {
            UnhandledCommandExceptionEventArgs? args = null;
            try
            {
                var handler = UnhandledException;
                if (handler != null)
                {
                    args = new UnhandledCommandExceptionEventArgs(exception);
                    try
                    {
                        handler.Invoke(this, args);
                        if (args.Handled) return;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString());
                    }
                }

                AsyncCommand.OnUnhandledException(this, exception, ref args);
            }
            finally
            {
                if (args?.Handled != true)
                {
                    Trace.WriteLine(
                        $"An Unhandled Exception has occurred in {GetType().Name}:{Environment.NewLine}{exception}");
                }
            }
        }

        #endregion

        #region Nested classes

        /// <summary>
        /// Represents a cancelable operation associated with a command execution.
        /// </summary>
        /// <remarks>
        /// Tracks cancellation state and provides a disposable handle to unregister the execution when completed.
        /// </remarks>
        internal sealed class Cancelable(int taskId, AsyncCommandBase<T> command) : IDisposable
        {
            /// <summary>
            /// Gets whether cancellation has been requested via <see cref="IAsyncCommand.Cancel"/>.
            /// </summary>
            public volatile bool IsCancellationRequested;

            /// <summary>
            /// Gets or sets the <see cref="CancellationTokenSource"/> linked to this execution.
            /// </summary>
            public volatile CancellationTokenSource? Source;

            /// <summary>
            /// Requests cancellation of this execution.
            /// </summary>
            public void Cancel()
            {
                IsCancellationRequested = true;
                Source?.Cancel();
            }

            /// <summary>
            /// Unregisters this execution and releases references.
            /// </summary>
            public void Dispose()
            {
                Source = null;
                command.UnregisterCancelable(taskId);
            }

            /// <summary>
            /// Throws <see cref="OperationCanceledException"/> if cancellation was requested.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ThrowIfCancellationRequested()
            {
                if (IsCancellationRequested)
                {
                    throw new OperationCanceledException("The operation was canceled.");
                }
            }
        }

        #endregion
    }

    #region Helpers

    internal static class IdGenerator
    {
        private static int s_counter;

        /// <summary>
        /// Generates a new unique identifier for command executions.
        /// </summary>
        /// <returns>A unique integer identifier that is never zero.</returns>
        internal static int NewId()
        {
            int newId;
            do
            {
                newId = Interlocked.Increment(ref s_counter);
            }
            while (newId == 0);
            return newId;
        }
    }

    #endregion
}
