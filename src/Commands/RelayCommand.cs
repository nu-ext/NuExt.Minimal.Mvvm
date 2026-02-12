using System;
using System.Runtime.CompilerServices;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides a non-generic version of <see cref="RelayCommand{T}"/> for commands without parameters.
    /// </summary>
    /// <remarks>
    /// Supports multiple execution when <see cref="CommandBase.AllowConcurrentExecution"/> is <see langword="true"/>.
    /// The command parameter passed to <see cref="Execute"/> and <see cref="CommandBase{T}.CanExecute"/> is ignored.
    /// </remarks>
    public sealed class RelayCommand : CommandBase<object?>
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic. If <see langword="null"/>, the command can always execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="execute"/> is <see langword="null"/>.</exception>
        public RelayCommand(Action execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (static () => true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        #region Methods

        ///<inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool CanExecuteCore(object? parameter)
        {
            return _canExecute.Invoke();
        }

        /// <inheritdoc/>
        public override void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            using var scope = BeginExecutionScope();
            if (!scope.Started) return;
            _execute();
        }

        #endregion

    }
}
