namespace Minimal.Mvvm
{
    /// <summary>
    /// Exposes the service container associated with the current instance.
    /// </summary>
    public interface IServiceContainerProvider
    {
        /// <summary>
        /// Gets the service container associated with this instance.
        /// </summary>
        /// <remarks>
        /// The returned container supports named and multi-service resolution.
        /// Implementations should treat the container reference as immutable after publication.
        /// </remarks>
        IServiceContainer Services { get; }
    }
}
