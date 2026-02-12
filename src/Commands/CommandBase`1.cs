using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides a base class for implementing commands with typed parameter.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    public abstract class CommandBase<T> : CommandBase, IRelayCommand<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBase{T}"/> class.
        /// </summary>
        protected CommandBase()
        {
        }

        #region Methods

        /// <inheritdoc/>
        bool ICommand.CanExecute(object? parameter)
        {
            if (!Cast<T>.TryTo(parameter, out var typedParam))
            {
                return false;
            }
            return CanExecute(typedParam);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecute(T parameter)
        {
            if (!AllowConcurrentExecution && IsExecuting)
            {
                return false;
            }
            return CanExecuteCore(parameter);
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <returns><see langword="true"/> if the command can execute; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method is not called when <see cref="CommandBase.IsExecuting"/> is <see langword="true"/> 
        /// and <see cref="CommandBase.AllowConcurrentExecution"/> is <see langword="false"/>.
        /// </remarks>
        protected abstract bool CanExecuteCore(T parameter);

        /// <inheritdoc/>
        void ICommand.Execute(object? parameter)
        {
            Execute(GetCommandParameter(parameter));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Implementations must wrap the execution in an execution scope:
        /// <code>
        /// using var scope = BeginExecutionScope();
        /// if (!scope.Started) return;
        /// // do work
        /// </code>
        /// </remarks>
        public abstract void Execute(T parameter);

        /// <summary>
        /// Converts the given parameter to the specified generic type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="parameter">The parameter to be converted. Can be <see langword="null"/>.</param>
        /// <returns>
        /// The converted parameter of type <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// Thrown when the parameter cannot be converted to <typeparamref name="T"/>.
        /// </exception>
        protected internal T GetCommandParameter(object? parameter)
        {
            return Cast<T>.To(parameter);
        }

        #endregion
    }
}
