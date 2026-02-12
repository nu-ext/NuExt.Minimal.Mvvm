using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Represents an asynchronous command that can execute with a parameter of type T.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <remarks>
    /// Supports multiple execution when <see cref="CommandBase.AllowConcurrentExecution"/> is <see langword="true"/>.
    /// </remarks>
    public sealed class AsyncCommand<T> : AsyncCommandBase<T>
    {
        private readonly Func<T, CancellationToken, Task> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic with cancellation support.</param>
        /// <param name="canExecute">The logic to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if the execute argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// The delegate receives a <see cref="CancellationToken"/> that is canceled when 
        /// <see cref="AsyncCommandBase{T}.Cancel"/> is called or when an external token is canceled.
        /// </remarks>
        public AsyncCommand(Func<T, CancellationToken, Task> execute, Func<T, bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (static _ => true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic with cancellation support.</param>
        /// <remarks>
        /// The delegate receives a <see cref="CancellationToken"/> that is canceled when 
        /// <see cref="AsyncCommandBase{T}.Cancel"/> is called or when an external token is canceled.
        /// </remarks>
        public AsyncCommand(Func<T, CancellationToken, Task> execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic.</param>
        /// <param name="canExecute">The logic to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if the execute argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// The provided <paramref name="execute"/> delegate does not accept a <see cref="CancellationToken"/>.
        /// For cancellation support, use the constructor accepting <see cref="Func{T, CancellationToken, Task}"/>.
        /// </remarks>
        public AsyncCommand(Func<T, Task> execute, Func<T, bool>? canExecute)
        {
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            _execute = (p, _) => execute(p);
            _canExecute = canExecute ?? (static _ => true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic.</param>
        /// <exception cref="ArgumentNullException">Thrown if the execute argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// The provided <paramref name="execute"/> delegate does not accept a <see cref="CancellationToken"/>.
        /// For cancellation support, use the constructor accepting <see cref="Func{T, CancellationToken, Task}"/>.
        /// </remarks>
        public AsyncCommand(Func<T, Task> execute) : this(execute, null)
        {

        }

        #region Methods

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool CanExecuteCore(T parameter)
        {
            return _canExecute.Invoke(parameter);
        }

        ///<inheritdoc/>
        protected override async Task ExecuteAsyncCore(T parameter, CancellationToken cancellationToken)
        {
            await _execute(parameter, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
