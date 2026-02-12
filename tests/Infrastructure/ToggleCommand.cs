using System.Windows.Input;

namespace Minimal.Mvvm.Tests.Infrastructure
{
    /// <summary>
    /// Simple ICommand that allows toggling CanExecute and raising CanExecuteChanged.
    /// </summary>
    internal sealed class ToggleCommand(bool initial = true, Action? onExecute = null) : ICommand
    {
        private readonly Action? _onExecute = onExecute;
        private bool _canExecute = initial;

        public bool CanExecute(object? parameter) => _canExecute;

        public void Execute(object? parameter) => _onExecute?.Invoke();

        public event EventHandler? CanExecuteChanged;

        public void SetCanExecute(bool value)
        {
            if (_canExecute == value) return;
            _canExecute = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}
