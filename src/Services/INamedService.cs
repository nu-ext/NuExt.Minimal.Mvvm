namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides a named identifier for a service.
    /// </summary>
    public interface INamedService
    {
        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        string? Name { get; }
    }
}
