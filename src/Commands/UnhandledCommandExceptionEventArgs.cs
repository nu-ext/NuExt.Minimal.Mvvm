using System;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides data for the <see cref="IAsyncCommand.UnhandledException"/> event.
    /// </summary>
    /// <remarks>
    /// Event handlers can set the <see cref="Handled"/> property to <see langword="true"/>
    /// to indicate that the exception has been handled and should not be propagated further.
    /// </remarks>
    public sealed class UnhandledCommandExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledCommandExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public UnhandledCommandExceptionEventArgs(Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }


        /// <summary>
        /// Gets the exception that occurred during command execution.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the exception has been handled.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to indicate the exception has been handled and
        /// should not be propagated to the global handler; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool Handled { get; set; }
    }
}
