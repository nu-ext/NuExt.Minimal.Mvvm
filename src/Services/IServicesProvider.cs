using System;
using System.Collections.Generic;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Defines a mechanism for retrieving all registered services of a specified type.
    /// </summary>
    /// <remarks>
    /// This interface complements <see cref="IServiceProvider"/> by providing access
    /// to multiple service instances rather than a single instance.
    /// </remarks>
    public interface IServicesProvider
    {
        /// <summary>
        /// Retrieves all registered service instances of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of services to retrieve.</param>
        /// <returns>
        /// A non-null enumerable sequence of service instances. The sequence may be empty; order is not guaranteed.
        /// </returns>
        /// <remarks>
        /// Enumeration may be lazy and may trigger service activation.
        /// Implementations may enumerate a moment-in-time snapshot and may observe concurrent registrations or removals.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        IEnumerable<object> GetServices(Type serviceType);
    }
}
