namespace Minimal.Mvvm
{
    /// <summary>
    /// A generic version of <see cref="IRelayCommand"/> that provides type-safe execution.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    public interface IRelayCommand<in T> : IRelayCommand
    {
        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <returns><see langword="true"/> if this command can be executed; otherwise, <see langword="false"/>.</returns>
        bool CanExecute(T parameter);

        /// <summary>
        /// Executes the command with the specified parameter.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        void Execute(T parameter);
    }
}
