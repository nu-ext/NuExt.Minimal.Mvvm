#if NET5_0_OR_GREATER || NETSTANDARD2_1 || NUEXT_ENABLE_VALUETASK
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm
{
    /// <remarks>
    /// <para>
    /// This command is optimized for scenarios where the execution delegate often completes synchronously
    /// (e.g., validation, cached data access, state updates). Using <see cref="ValueTask"/> avoids
    /// unnecessary heap allocations in these cases, improving performance and reducing GC pressure.
    /// </para>
    /// <para>
    /// <strong>Choose this over <see cref="AsyncCommand"/> when:</strong>
    /// <list type="bullet">
    /// <item><description>The command executes frequently (e.g., UI event handlers).</description></item>
    /// <item><description>The logic commonly completes synchronously (simple validation, property updates).</description></item>
    /// <item><description>Minimizing GC pressure is critical for UI responsiveness.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Use the standard <see cref="AsyncCommand"/> when:</strong>
    /// <list type="bullet">
    /// <item><description>The operation is inherently asynchronous (I/O, network calls).</description></item>
    /// <item><description>Simplicity and predictability are prioritized over micro-optimizations.</description></item>
    /// <item><description>The command is used infrequently.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Supports multiple execution when <see cref="CommandBase.AllowConcurrentExecution"/> is <see langword="true"/>.
    /// The command parameter passed to <see cref="AsyncCommandBase{T}.Execute"/> and <see cref="CommandBase{T}.CanExecute"/> is ignored.
    /// </para>
    /// </remarks>
    public sealed class AsyncValueCommand : AsyncCommandBase<object?>
    {
        private readonly Func<CancellationToken, ValueTask> _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncValueCommand"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic with cancellation support.</param>
        /// <param name="canExecute">The logic to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The delegate receives a <see cref="CancellationToken"/> that is canceled when 
        /// <see cref="AsyncCommandBase{T}.Cancel"/> is called or when an external token is canceled.
        /// </remarks>
        public AsyncValueCommand(Func<CancellationToken, ValueTask> execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (static () => true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncValueCommand"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic with cancellation support.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The delegate receives a <see cref="CancellationToken"/> that is canceled when 
        /// <see cref="AsyncCommandBase{T}.Cancel"/> is called or when an external token is canceled.
        /// </remarks>
        public AsyncValueCommand(Func<CancellationToken, ValueTask> execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncValueCommand"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic.</param>
        /// <param name="canExecute">The logic to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The provided <paramref name="execute"/> delegate does not accept a <see cref="CancellationToken"/>.
        /// For cancellation support, use <see cref="AsyncValueCommand(Func{CancellationToken, ValueTask}, Func{bool}?)"/>.
        /// </remarks>
        public AsyncValueCommand(Func<ValueTask> execute, Func<bool>? canExecute)
        {
            _ = execute ?? throw new ArgumentNullException(nameof(execute));
            _execute = _ => execute();
            _canExecute = canExecute ?? (static () => true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncValueCommand"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The asynchronous execution logic.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The provided <paramref name="execute"/> delegate does not accept a <see cref="CancellationToken"/>.
        /// For cancellation support, use <see cref="AsyncValueCommand(Func{CancellationToken, ValueTask}, Func{bool}?)"/>.
        /// </remarks>
        public AsyncValueCommand(Func<ValueTask> execute) : this(execute, null)
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
        protected override Task ExecuteAsyncCore(object? parameter, CancellationToken cancellationToken)
        {
            ValueTask valueTask = _execute(cancellationToken);

            if (valueTask.IsCompleted)
            {
                // Ensure proper consumption of the ValueTask, including potential IValueTaskSource resources.
                valueTask.GetAwaiter().GetResult();
                return Task.CompletedTask;// do not return canceled: the operation has completed successfully
            }

            return AwaitAsync(valueTask);
        }

        private static async Task AwaitAsync(ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
        }

        #endregion
    }
}
#endif