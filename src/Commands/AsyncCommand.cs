using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides a non-generic version of <see cref="AsyncCommand{T}"/> for commands without parameters.
    /// </summary>
    /// <remarks>
    /// Supports multiple execution when <see cref="CommandBase.AllowConcurrentExecution"/> is <see langword="true"/>.
    /// The command parameter passed to <see cref="AsyncCommandBase{T}.Execute"/> and <see cref="CommandBase{T}.CanExecute"/> is ignored.
    /// </remarks>
    public sealed class AsyncCommand : AsyncCommandBase<object?>
    {
        private readonly Func<CancellationToken, Task> _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic with cancellation support.</param>
        /// <param name="canExecute">The logic to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The delegate receives a <see cref="CancellationToken"/> that is canceled when 
        /// <see cref="AsyncCommandBase{T}.Cancel"/> is called or when an external token is canceled.
        /// </remarks>
        public AsyncCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (static () => true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic with cancellation support.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The delegate receives a <see cref="CancellationToken"/> that is canceled when 
        /// <see cref="AsyncCommandBase{T}.Cancel"/> is called or when an external token is canceled.
        /// </remarks>
        public AsyncCommand(Func<CancellationToken, Task> execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic.</param>
        /// <param name="canExecute">The logic to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The provided <paramref name="execute"/> delegate does not accept a <see cref="CancellationToken"/>.
        /// For cancellation support, use <see cref="AsyncCommand(Func{CancellationToken, Task}, Func{bool}?)"/>.
        /// </remarks>
        public AsyncCommand(Func<Task> execute, Func<bool>? canExecute)
        {
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            _execute = _ => execute();
            _canExecute = canExecute ?? (static () => true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The provided <paramref name="execute"/> delegate does not accept a <see cref="CancellationToken"/>.
        /// For cancellation support, use <see cref="AsyncCommand(Func{CancellationToken, Task}, Func{bool}?)"/>.
        /// </remarks>
        public AsyncCommand(Func<Task> execute) : this(execute, null)
        {
        }

        #region Methods

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool CanExecuteCore(object? parameter)
        {
            return _canExecute.Invoke();
        }

        ///<inheritdoc/>
        protected override async Task ExecuteAsyncCore(object? parameter, CancellationToken cancellationToken)
        {
            await _execute(cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Global Unhandled Exception Handler

        /// <summary>
        /// Occurs when an exception from <see cref="ICommand.Execute"/> is not handled locally.
        /// </summary>
        /// <remarks>
        /// <para>This static event provides centralized handling for exceptions that escape
        /// the local <see cref="IAsyncCommand.UnhandledException"/> event handlers.</para>
        /// <para>Note: This event only captures exceptions from the async void 
        /// <see cref="ICommand.Execute(object?)"/> method. Exceptions from 
        /// <see cref="IAsyncCommand.ExecuteAsync(object)"/> methods are not reported here 
        /// and should be handled via the returned <see cref="Task"/>.</para>
        /// </remarks>
        public static event EventHandler<UnhandledCommandExceptionEventArgs>? GlobalUnhandledException;

        /// <summary>
        /// Raises the <see cref="GlobalUnhandledException"/> event.
        /// </summary>
        /// <param name="sender">The command that raised the exception.</param>
        /// <param name="exception">The unhandled exception thrown from <see cref="ICommand.Execute"/>.</param>
        /// <param name="e">The event arguments containing exception details.</param>
        /// <remarks>
        /// This method is called by command implementations when an exception from 
        /// <see cref="ICommand.Execute"/> is not marked as handled by local event subscribers.
        /// </remarks>
        internal static void OnUnhandledException(object sender, Exception exception, ref UnhandledCommandExceptionEventArgs? e)
        {
            var handler = GlobalUnhandledException;
            if (handler == null) return;

            e ??= new UnhandledCommandExceptionEventArgs(exception);
            handler.Invoke(sender, e);
        }

        #endregion
    }
}
