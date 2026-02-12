namespace Minimal.Mvvm
{
    /// <summary>
    /// Defines a service interface for controlling the visibility and state of a window.
    /// </summary>
    public interface IWindowService : INamedService
    {
        /// <summary>
        /// Gets a value indicating whether the window is closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Gets a value indicating whether the window is visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Activates the window, bringing it to the foreground.
        /// </summary>
        void Activate();

        /// <summary>
        /// Closes the window.
        /// </summary>
        void Close();

        /// <summary>
        /// Hides the window.
        /// </summary>
        void Hide();

        /// <summary>
        /// Shows the window.
        /// </summary>
        void Show();

        /// <summary>
        /// Minimizes the window.
        /// </summary>
        void Minimize();

        /// <summary>
        /// Maximizes the window.
        /// </summary>
        void Maximize();

        /// <summary>
        /// Restores the window to its normal state.
        /// </summary>
        void Restore();
    }
}
