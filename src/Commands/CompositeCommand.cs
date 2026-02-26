using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Represents a command that aggregates multiple commands and executes them in registration order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Child commands can be added and removed at runtime. Each call to
    /// <see cref="ICommand.CanExecute"/> and <see cref="ICommand.Execute"/> operates
    /// on a stable snapshot of the current children. The composite forwards
    /// <see cref="ICommand.CanExecuteChanged"/> when the effective executability may change.
    /// </para>
    /// <para>
    /// Execution: child commands are invoked in order. For commands implementing <see cref="IAsyncCommand"/>,
    /// <see cref="IAsyncCommand.ExecuteAsync(object?, System.Threading.CancellationToken)"/> is awaited before proceeding to the next one.
    /// For non-asynchronous commands, <see cref="ICommand.Execute(object?)"/> is invoked. 
    /// If a command throws, execution stops and the exception is propagated.
    /// </para>
    /// <para>
    /// Supports multiple execution when <see cref="CommandBase.AllowConcurrentExecution"/> is <see langword="true"/>.
    /// </para>
    /// </remarks>
    public sealed class CompositeCommand : AsyncCommandBase<object?>, IDisposable
    {
#if NET9_0_OR_GREATER
        private enum States
        {
            NotDisposed, // default value of _state
            Disposing,
            Disposed
        }

        private volatile States _state;
#else
        private static class States
        {
            public const int NotDisposed = 0; // default value of _state
            public const int Disposing = 1;
            public const int Disposed = 2;
        }

        private volatile int _state;
#endif

        private ICommand[] _commands;

        /// <summary>
        /// Initializes a new instance with the specified commands. Duplicates are removed, preserving first occurrence.
        /// </summary>
        /// <param name="commands">The collection of commands to be aggregated.</param>
        /// <exception cref="ArgumentNullException">Thrown when the commands parameter is null.</exception>
        public CompositeCommand(IEnumerable<ICommand> commands)
        {
            _ = commands ?? throw new ArgumentNullException(nameof(commands));
            _commands = DeduplicatePreserveOrder(commands);
            SubscribeItems(_commands);
        }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance with the specified commands. Duplicates are removed, preserving first occurrence.
        /// </summary>
        /// <param name="commands">A read-only span of commands to be aggregated.</param>
        public CompositeCommand(params ReadOnlySpan<ICommand> commands)
        {
            _commands = DeduplicatePreserveOrder(commands, nameof(commands));
            SubscribeItems(_commands);
        }
#else
        /// <summary>
        /// Initializes a new instance with the specified commands. Duplicates are removed, preserving first occurrence.
        /// </summary>
        /// <param name="commands">The array of commands to be aggregated.</param>
        /// <exception cref="ArgumentNullException">Thrown when the commands parameter is null.</exception>
        public CompositeCommand(params ICommand[] commands)
        {
            _ = commands ?? throw new ArgumentNullException(nameof(commands));
            _commands = DeduplicatePreserveOrder(commands);
            SubscribeItems(_commands);
        }
#endif

        #region Properties

        /// <summary>
        /// Gets or sets the strategy for checking command executability.
        /// </summary>
        public CanExecuteMode CanExecuteMode { get; init; }

        /// <summary>
        /// Gets the current number of child commands.
        /// </summary>
        public int Count => Volatile.Read(ref _commands).Length;

        /// <summary>
        /// Gets a value indicating whether the object has been disposed.
        /// </summary>
        /// <remarks>
        /// This property is thread-safe for reading.
        /// </remarks>
        public bool IsDisposed => _state == States.Disposed;

        /// <summary>
        /// Gets a value indicating whether the object is currently in the process of being disposed.
        /// </summary>
        /// <remarks>
        /// This property is thread-safe for reading.
        /// </remarks>
        [Browsable(false)]
        public bool IsDisposing => _state == States.Disposing;

        /// <summary>
        /// Gets a value indicating whether the object is currently disposing or has been disposed.
        /// </summary>
        /// <remarks>
        /// This property is thread-safe for reading.
        /// </remarks>
        private bool IsDisposingOrDisposed => _state != States.NotDisposed;

        #endregion

        #region Methods

        private static bool Contains(ICommand[] items, ICommand command)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (ReferenceEquals(items[i], command)) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a snapshot copy of the current child commands in registration order.
        /// </summary>
        /// <remarks>The returned array does not reflect subsequent modifications.</remarks>
        public ICommand[] ToArray()
        {
            var snapshot = Volatile.Read(ref _commands);
            return snapshot.Length == 0 ? [] : (ICommand[])snapshot.Clone();
        }

        /// <summary>
        /// Attempts to add the specified command to the end of the sequence.
        /// Returns false if the same reference is already present.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ObjectDisposedException"/>
        public bool TryAdd(ICommand command)
        {
            _ = command ?? throw new ArgumentNullException(nameof(command));
            CheckDisposingOrDisposed();

            while (true)
            {
                var current = Volatile.Read(ref _commands);
                if (Contains(current, command))
                {
                    return false;
                }

                var next = new ICommand[current.Length + 1];
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                current.AsSpan().CopyTo(next);
                next[^1] = command;
#else
                Array.Copy(current, next, current.Length);
                next[next.Length - 1] = command;
#endif

                if (Interlocked.CompareExchange(ref _commands, next, current) == current)
                {
                    SubscribeOne(command);
                    RaiseCanExecuteChanged();
                    OnPropertyChanged(EventArgsCache.CountPropertyChanged);
                    return true;
                }
            }
        }

        /// <summary>
        /// Attempts to remove the specified command reference.
        /// Returns false if not found.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ObjectDisposedException"/>
        public bool TryRemove(ICommand command)
        {
            _ = command ?? throw new ArgumentNullException(nameof(command));
            CheckDisposingOrDisposed();

            while (true)
            {
                var current = Volatile.Read(ref _commands);
                int idx = -1;
                for (int i = 0; i < current.Length; i++)
                {
                    if (ReferenceEquals(current[i], command))
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx < 0)
                {
                    return false;
                }

                var next = new ICommand[current.Length - 1];
                if (idx > 0)
                {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    current.AsSpan(0, idx).CopyTo(next);
#else
                    Array.Copy(current, 0, next, 0, idx);
#endif
                }
                if (idx < current.Length - 1)
                {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    current.AsSpan(idx + 1).CopyTo(next.AsSpan(idx));
#else
                    Array.Copy(current, idx + 1, next, idx, current.Length - idx - 1);
#endif
                }

                if (Interlocked.CompareExchange(ref _commands, next, current) == current)
                {
                    UnsubscribeOne(command);
                    if (!IsDisposingOrDisposed)
                    {
                        RaiseCanExecuteChanged();
                        OnPropertyChanged(EventArgsCache.CountPropertyChanged);
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Removes all commands.
        /// </summary>
        /// <exception cref="ObjectDisposedException"/>
        public void Clear()
        {
            CheckDisposingOrDisposed();
            var oldCommands = Interlocked.Exchange(ref _commands, []);
            if (oldCommands.Length == 0) return;

            UnsubscribeItems(oldCommands);

            if (!IsDisposingOrDisposed)
            {
                RaiseCanExecuteChanged();
                OnPropertyChanged(EventArgsCache.CountPropertyChanged);
            }
        }

        #endregion

        #region Execution Methods

        /// <summary>
        /// Determines whether all aggregated commands can execute in their current state.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <returns><see langword="true"/> if the composite can execute; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description><see cref="CanExecuteMode.All"/>: returns <see langword="false"/> if any child returns <see langword="false"/>.</description></item>
        /// <item><description><see cref="CanExecuteMode.Any"/>: returns <see langword="true"/> if any child returns <see langword="true"/>.</description></item>
        /// <item><description><see cref="CanExecuteMode.First"/>: returns the <c>CanExecute</c> value of the first command only.</description></item>
        /// </list>
        /// </remarks>
        protected override bool CanExecuteCore(object? parameter)
        {
            if (IsDisposingOrDisposed)
            {
                return false;
            }

            var commands = Volatile.Read(ref _commands);
            uint count = (uint)commands.Length;

            if (count == 0)
            {
                return false;
            }

            if (count == 1 || CanExecuteMode == CanExecuteMode.First)
            {
                return commands[0].CanExecute(parameter);
            }

            switch (CanExecuteMode)
            {
                case CanExecuteMode.All:
                    for (uint i = 0; i < count; i++)
                    {
                        if (!commands[i].CanExecute(parameter))
                        {
                            return false;
                        }
                    }
                    return true;
                case CanExecuteMode.Any:
                    for (uint i = 0; i < count; i++)
                    {
                        if (commands[i].CanExecute(parameter))
                        {
                            return true;
                        }
                    }
                    return false;
            }

            throw new InvalidOperationException($"Unknown CanExecuteMode: {CanExecuteMode}");
        }

        /// <summary>
        /// Executes child commands in sequence.
        /// </summary>
        /// <param name="parameter">The command parameter passed to each child.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Respects <see cref="CanExecuteMode"/> during execution:
        /// in <c>Any</c> mode skips non-executable commands;
        /// in <c>First</c> mode re-checks executability and stops on the first failure.</description></item>
        /// <item><description>For children implementing <see cref="IAsyncCommand"/>, awaits <c>ExecuteAsync</c> with the provided token.</description></item>
        /// <item><description>Cancellation is observed between commands.</description></item>
        /// <item><description>Exceptions thrown by child commands stop the sequence and are propagated to the caller.</description></item>
        /// </list>
        /// </remarks>
        protected override async Task ExecuteAsyncCore(object? parameter, CancellationToken cancellationToken)
        {
            var commands = Volatile.Read(ref _commands);
            uint count = (uint)commands.Length;
            if (count == 0)
            {
                return;
            }

            // We only re-check CanExecute per-item for First/Any (All is validated in CanExecuteCore).
            bool checkCanExecute = CanExecuteMode != CanExecuteMode.All;

            for (uint i = 0; i < count; i++)
            {
                var command = commands[i];
                cancellationToken.ThrowIfCancellationRequested();

                if (checkCanExecute && !command.CanExecute(parameter))
                {
                    switch (CanExecuteMode)
                    {
                        case CanExecuteMode.First:
                            return;    // stop chain on first non-executable

                        case CanExecuteMode.Any:
                            continue;  // skip this one, try next
                    }
                }

                if (command is IAsyncCommand asyncCommand)
                {
                    await asyncCommand.ExecuteAsync(parameter, cancellationToken).ConfigureAwait(ContinueOnCapturedContext);
                }
                else
                {
                    command.Execute(parameter);
                }
            }
        }

        #endregion

        #region Registration Helpers

        private static ICommand[] DeduplicatePreserveOrder(IEnumerable<ICommand> source, [CallerArgumentExpression(nameof(source))] string? paramName = null)
        {
            var seen = new HashSet<ICommand>(ReferenceEqualityComparer.Instance);
            var list = new List<ICommand>();
            foreach (var cmd in source)
            {
                if (cmd is null)
                    throw new ArgumentException("The command sequence contains a null reference.", paramName);

                if (seen.Add(cmd))
                    list.Add(cmd);
            }
            return [.. list];
        }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        private static ICommand[] DeduplicatePreserveOrder(ReadOnlySpan<ICommand> source, [CallerArgumentExpression(nameof(source))] string? paramName = null)
        {
            var seen = new HashSet<ICommand>(ReferenceEqualityComparer.Instance);
            var list = new List<ICommand>();
            foreach (var cmd in source)
            {
                if (cmd is null)
                    throw new ArgumentException("The command sequence contains a null reference.", paramName);

                if (seen.Add(cmd))
                    list.Add(cmd);
            }
            return [.. list];
        }
#endif

        /// <summary>
        /// Handles the CanExecuteChanged event of the individual commands.
        /// </summary>
        private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
        {
            RaiseCanExecuteChanged();
        }

        private void SubscribeOne(ICommand command)
        {
            command.CanExecuteChanged += OnCommandCanExecuteChanged;
        }

        private void UnsubscribeOne(ICommand command)
        {
            command.CanExecuteChanged -= OnCommandCanExecuteChanged;
        }

        /// <summary>
        /// Subscribes to the CanExecuteChanged events of the aggregated commands.
        /// </summary>
        private void SubscribeItems(ICommand[] commands)
        {
            for (int i = 0; i < commands.Length; i++)
            {
                commands[i].CanExecuteChanged += OnCommandCanExecuteChanged;
            }
        }

        /// <summary>
        /// Unsubscribes from the CanExecuteChanged events of the aggregated commands.
        /// </summary>
        private void UnsubscribeItems(ICommand[] commands)
        {
            for (int i = 0; i < commands.Length; i++)
            {
                commands[i].CanExecuteChanged -= OnCommandCanExecuteChanged;
            }
        }

        #endregion

        #region Disposable

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposingOrDisposed()
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(IsDisposingOrDisposed, this);
#else
            if (IsDisposingOrDisposed) throw new ObjectDisposedException(GetType().FullName);
#endif
        }

        /// <summary>
        /// Releases all resources used by the composite command.
        /// </summary>
        /// <remarks>
        /// Unsubscribes from <see cref="ICommand.CanExecuteChanged"/> of all child commands. Idempotent.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _state, States.Disposing, States.NotDisposed) != States.NotDisposed)
            {
                return; // Already disposing or disposed
            }

            int oldCount = 0;
            try
            {
                var oldCommands = Interlocked.Exchange(ref _commands, []);
                oldCount = oldCommands.Length;
                if (oldCount > 0)
                {
                    UnsubscribeItems(oldCommands);
                }
            }
            finally
            {
                _state = States.Disposed;
                OnPropertyChanged(EventArgsCache.IsDisposedPropertyChanged);

                if (oldCount > 0)
                {
                    RaiseCanExecuteChanged();
                    OnPropertyChanged(EventArgsCache.CountPropertyChanged);
                }
                ClearPropertyChangedHandlers();
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines how <c>CompositeCommand.CanExecute(object)</c> is computed
    /// and how commands are treated during <c>Execute(object)</c>.
    /// </summary>
    public enum CanExecuteMode
    {
        /// <summary>
        /// <c>CanExecute(p)</c> is true only if every registered command
        /// returns true for the same parameter. During execution the composite
        /// does not re-check <c>CanExecute</c> per step.
        /// </summary>
        All,

        /// <summary>
        /// <c>CanExecute(p)</c> is <see langword="true"/> if and only if the first registered command
        /// returns true. During execution each command is re-validated
        /// before invocation; the sequence stops if <c>CanExecute(p)</c> is false.
        /// </summary>
        First,

        /// <summary>
        /// <c>CanExecute(p)</c> is <see langword="true"/> if at least one registered command returns true.
        /// During execution non-executable commands are skipped; executable ones are invoked.
        /// </summary>
        Any
    }

    internal static partial class EventArgsCache
    {
        internal static readonly PropertyChangedEventArgs CountPropertyChanged = new(nameof(CompositeCommand.Count));
        internal static readonly PropertyChangedEventArgs IsDisposedPropertyChanged = new(nameof(CompositeCommand.IsDisposed));
    }
}
