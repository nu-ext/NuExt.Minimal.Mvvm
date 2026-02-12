using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides a base implementation for commands with execution state tracking and change notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Events (PropertyChanged, CanExecuteChanged) may be raised from any thread. Consumers are responsible for marshaling 
    /// to the appropriate context when invoking commands or handling notifications from non-UI threads.
    /// </para>
    /// </remarks>
    public abstract class CommandBase : INotifyPropertyChanged
    {
        private volatile int _executingCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBase"/> class.
        /// </summary>
        protected CommandBase()
        {
        }

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether concurrent execution of the command is allowed.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to allow multiple concurrent executions; otherwise, <see langword="false"/>.
        /// Default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// When this property is <see langword="true"/>, multiple invocations of the command
        /// can proceed simultaneously without blocking. When <see langword="false"/>,
        /// the command will not start a new execution while another is in progress.
        /// </remarks>
        public bool AllowConcurrentExecution { get; init; }

        /// <summary>
        /// Gets a value indicating whether an execution of the command is in progress.
        /// </summary>
        /// <value><see langword="true"/> if the command is executing; otherwise, <see langword="false"/>.</value>
        /// <remarks>
        /// <para>
        /// This property is updated atomically and can be safely read from multiple threads.
        /// </para>
        /// <para>
        /// The <see cref="INotifyPropertyChanged.PropertyChanged"/> event for this property may be raised
        /// from any thread, including threads different from the one that initiated the execution.
        /// </para>
        /// </remarks>
        public bool IsExecuting => _executingCount != 0;


        /// <summary>
        /// Gets the current execution count for this command.
        /// </summary>
        /// <remarks>
        /// This value is a transient snapshot and may change concurrently. It is primarily intended for diagnostics.
        /// </remarks>
        protected internal int ExecutingCount => _executingCount;

        #endregion

        #region Events

        private readonly WeakEvent<EventHandler, EventArgs> _canExecuteChanged = new();

        /// <summary>
        /// Occurs when <see cref="ICommand.CanExecute(object)"/> may have changed.
        /// </summary>
        /// <remarks>
        /// This event may be raised from any thread.
        /// </remarks>
        public event EventHandler? CanExecuteChanged
        {
            add => _canExecuteChanged.AddHandler(value);
            remove => _canExecuteChanged.RemoveHandler(value);
        }

        private event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc/>
        /// <remarks>
        /// This event may be raised from any thread.
        /// </remarks>
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add => PropertyChanged += value;
            remove => PropertyChanged -= value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a disposable execution scope that starts execution if possible and guarantees completion on dispose.
        /// </summary>
        /// <returns>
        /// An <see cref="ExecutionScope"/> whose <see cref="ExecutionScope.Started"/> indicates whether execution began.
        /// </returns>
        /// <example>
        /// <code>
        /// using var scope = BeginExecutionScope();
        /// if (!scope.Started)
        ///     return; // execution is already in progress (when concurrency is disallowed)
        ///
        /// // perform work here; completion is guaranteed by Dispose()
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ExecutionScope BeginExecutionScope() => new(this);

        /// <summary>
        /// Attempts to begin command execution atomically.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if execution was started;
        /// <see langword="false"/> if execution is already in progress and concurrent execution is not allowed.
        /// </returns>
        /// <remarks>
        /// When this method returns <see langword="true"/>, the caller must ensure <see cref="CompleteExecution"/>
        /// is called exactly once.
        /// </remarks>
        private bool TryStartExecution()
        {
            if (AllowConcurrentExecution)
            {
                int newCount = Interlocked.Increment(ref _executingCount);
                Debug.Assert(newCount > 0, $"Unexpected execution count: {newCount}");
                if (newCount != 1)
                {
                    return true;
                }
            }
            else
            {
                if (Interlocked.CompareExchange(ref _executingCount, 1, 0) != 0)
                {
                    return false;
                }
            }
            // 0 -> 1 - raise notifications
            OnIsExecutingChanged();
            return true;
        }

        /// <summary>
        /// Completes command execution and updates state.
        /// </summary>
        /// <remarks>
        /// This method must be called exactly once for each successful call to <see cref="TryStartExecution"/>.
        /// </remarks>
        private void CompleteExecution()
        {
            if (AllowConcurrentExecution)
            {
                int newCount = Interlocked.Decrement(ref _executingCount);
                switch (newCount)
                {
                    case < 0:
                        Trace.WriteLine($"Execution count negative: {newCount}");
                        Debug.Fail($"Execution count negative: {newCount}");
                        Interlocked.Exchange(ref _executingCount, 0);
                        return; // avoid spurious notifications when already at 0
                    case > 0:
                        return;
                }
            }
            else
            {
                int oldCount = Interlocked.Exchange(ref _executingCount, 0);
                if (oldCount != 1)
                {
                    Trace.WriteLine($"Unexpected execution count: {oldCount}");
                    Debug.Fail($"Unexpected execution count: {oldCount}");
                    if (oldCount == 0)
                    {
                        return;
                    }
                }
            }
            // 1 -> 0 - raise notifications
            OnIsExecutingChanged();
        }

        /// <summary>
        /// Occurs when the <see cref="IsExecuting"/> property value changes.
        /// </summary>
        protected virtual void OnIsExecutingChanged()
        {
            OnPropertyChanged(EventArgsCache.IsExecutingPropertyChanged);
            RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Removes all subscribers from the <see cref="PropertyChanged"/> event.
        /// </summary>
        [DebuggerHidden]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected void ClearPropertyChangedHandlers()
        {
            PropertyChanged = null;
        }

        /// <summary>
        /// Raises the <see cref="ICommand.CanExecuteChanged"/> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged.Raise(this, EventArgs.Empty);
        }

        #endregion

        protected readonly struct ExecutionScope(CommandBase owner) : IDisposable
        {
            public bool Started { get; } = owner.TryStartExecution();

            public void Dispose()
            {
                if (Started)
                {
                    owner.CompleteExecution();
                }
            }
        }

    }

    internal static partial class EventArgsCache
    {
        internal static readonly PropertyChangedEventArgs IsExecutingPropertyChanged = new(nameof(CommandBase.IsExecuting));
    }
}
