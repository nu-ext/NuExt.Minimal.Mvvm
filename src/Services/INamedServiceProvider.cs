using System;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Extends <see cref="IServiceProvider"/> to support named service resolution.
    /// </summary>
    public interface INamedServiceProvider : IServiceProvider
    {
        /// <summary>
        /// Gets a named service object of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of service to retrieve.</param>
        /// <param name="name">
        /// The name of the service to resolve, or <see langword="null"/> to resolve an unnamed/default service.
        /// An empty string is treated as unnamed.
        /// </param>
        /// <returns>
        /// A service object of the specified type, or null if no matching service is found.
        /// </returns>
        /// <remarks>
        /// Name comparison uses ordinal, case-sensitive semantics.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> is null.
        /// </exception>

        object? GetService(Type serviceType, string? name);
    }
}
