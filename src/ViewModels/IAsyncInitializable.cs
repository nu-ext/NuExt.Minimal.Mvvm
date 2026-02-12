using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Defines the contract for components that require asynchronous initialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initialization is <em>idempotent</em>: subsequent successful calls to <see cref="InitializeAsync(CancellationToken)"/>
    /// complete immediately without repeating the initialization work, and <see cref="IsInitialized"/> is <see langword="true"/>.
    /// </para>
    /// <para>
    /// Concurrency: implementations must ensure that concurrent calls do not trigger duplicate initialization. Additional callers
    /// should either await the in-flight initialization or return a completed task once initialization has succeeded.
    /// </para>
    /// <para>
    /// Thread-affinity: implementations may require being invoked on specific threads (e.g., UI thread). Such requirements should be
    /// documented by the implementing type.
    /// </para>
    /// </remarks>
    public interface IAsyncInitializable
    {
        /// <summary>
        /// Gets a value indicating whether the instance has been successfully initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Asynchronously initializes the instance.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation is canceled via <paramref name="cancellationToken"/>.
        /// </exception>
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}