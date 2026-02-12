using System;
using System.Runtime.CompilerServices;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Represents a command that can execute with a parameter of type T.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <remarks>
    /// Supports multiple execution when <see cref="CommandBase.AllowConcurrentExecution"/> is <see langword="true"/>.
    /// </remarks>
    public sealed class RelayCommand<T> : CommandBase<T>
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic. If null, the command can always execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (static _ => true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        public RelayCommand(Action<T> execute) : this(execute, null)
        {
        }

        #region Methods

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool CanExecuteCore(T parameter)
        {
            return _canExecute.Invoke(parameter);
        }

        /// <inheritdoc/>
        public override void Execute(T parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            using var scope = BeginExecutionScope();
            if (!scope.Started) return;

            _execute(parameter);
        }

        #endregion
    }
}
