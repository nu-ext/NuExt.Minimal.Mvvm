using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Defines a container for registering and resolving service instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Thread safety: registration, unregistration, and resolution are thread-safe.
    /// </para>
    /// <para>
    /// Lazy registrations (factories and type activations) are created on first successful resolution and cached
    /// for subsequent resolutions within the same container.
    /// </para>
    /// <para>
    /// Parent provider: when configured, resolution may delegate to the parent provider. Implementations may guard
    /// against reentrant parent resolution to avoid cross-level recursion.
    /// </para>
    /// <para>
    /// Disposal: services are not disposed by default when unregistered or cleared. Implementations may provide hooks
    /// (e.g., virtual callbacks) to perform cleanup.
    /// </para>
    /// </remarks>
    public interface IServiceContainer : INamedServiceProvider
    {
        /// <summary>
        /// Gets a service object of the specified type and name, optionally restricting resolution to local registrations only.
        /// </summary>
        /// <param name="serviceType">The requested service type.</param>
        /// <param name="name">
        /// The registration name. <see langword="null"/> or empty selects unnamed registrations.
        /// Name comparison is ordinal and case-sensitive.
        /// </param>
        /// <param name="localOnly">
        /// When <see langword="true"/>, resolution is restricted to registrations in this container; parent delegation is not performed.
        /// </param>
        /// <returns>The service instance, or <see langword="null"/> if not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is null.</exception>
        object? GetService(Type serviceType, string? name, bool localOnly);

        /// <summary>
        /// Retrieves all unique service instances assignable to <paramref name="serviceType"/> by cascading through the provider hierarchy.
        /// </summary>
        /// <param name="serviceType">The type of services to retrieve.</param>
        /// <returns>A non-null enumerable (distinct by reference equality); may be empty. Order is not guaranteed.</returns>
        /// <remarks>Enumeration is lazy and may trigger service activation and caching. Results may include parent provider services.</remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        IEnumerable<object> GetServices(Type serviceType);

        /// <summary>
        /// Retrieves all unique service instances assignable to <paramref name="serviceType"/>, optionally restricting enumeration to local registrations only.
        /// </summary>
        /// <param name="serviceType">The type of services to retrieve.</param>
        /// <param name="localOnly">
        /// When <see langword="true"/>, only services registered in this container are returned; parent services are excluded.
        /// </param>
        /// <returns>A non-null enumerable (distinct by reference equality); may be empty. Order is not guaranteed.</returns>
        /// <remarks>Enumeration is lazy and may trigger service activation and caching.</remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        IEnumerable<object> GetServices(Type serviceType, bool localOnly);

        /// <summary>Registers a service instance under the specified type.</summary>
        /// <param name="serviceType">The type to register the service as.</param>
        /// <param name="service">The service instance to register.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="service"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the instance is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a service already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>If a matching registration exists and <paramref name="throwIfExists"/> is <see langword="false"/>, the existing registration is replaced.</remarks>
        void RegisterService(Type serviceType, object service, bool throwIfExists = false);

        /// <summary>Registers a service instance under the specified type.</summary>
        /// <param name="serviceType">The type to register the service as.</param>
        /// <param name="service">The service instance to register.</param>
        /// <param name="name">The registration name; empty is treated as unnamed.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="service"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the instance is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a service already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>If a matching registration exists and <paramref name="throwIfExists"/> is <see langword="false"/>, the existing registration is replaced.</remarks>
        void RegisterService(Type serviceType, object service, string? name, bool throwIfExists = false);

        /// <summary>Registers a service factory under the specified type.</summary>
        /// <param name="serviceType">The type to register the service as.</param>
        /// <param name="callback">The factory that produces the service instance (invoked lazily on resolution).</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when open generics are used (detected on resolution).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown during registration when a matching service already exists and <paramref name="throwIfExists"/> is <see langword="true"/>;
        /// thrown on resolution when the factory returns <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The factory is invoked at most once per registration (lazy singleton semantics) and the produced instance is cached.
        /// </para>
        /// <para>
        /// Resolution is thread-safe: the container ensures that at most one instance is created even under concurrent resolution.
        /// </para>
        /// </remarks>
        void RegisterService(Type serviceType, Func<object> callback, bool throwIfExists = false);

        /// <summary>Registers a service factory under the specified type.</summary>
        /// <param name="serviceType">The type to register the service as.</param>
        /// <param name="callback">The factory that produces the service instance (invoked lazily on resolution).</param>
        /// <param name="name">The registration name; empty is treated as unnamed.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when open generics are used (detected on resolution).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown during registration when a matching service already exists and <paramref name="throwIfExists"/> is <see langword="true"/>;
        /// thrown on resolution when the factory returns <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The factory is invoked at most once per registration (lazy singleton semantics) and the produced instance is cached.
        /// </para>
        /// <para>
        /// Resolution is thread-safe: the container ensures that at most one instance is created even under concurrent resolution.
        /// </para>
        /// </remarks>
        void RegisterService(Type serviceType, Func<object> callback, string? name, bool throwIfExists = false);

        /// <summary>Registers a service factory where the implementation type differs from the registration type.</summary>
        /// <param name="serviceType">The registration (service) type.</param>
        /// <param name="implementationType">The concrete implementation type.</param>
        /// <param name="callback">The factory producing the implementation instance (invoked lazily on resolution).</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/>, <paramref name="implementationType"/>, or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="implementationType"/> is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown during registration when a matching service already exists and <paramref name="throwIfExists"/> is <see langword="true"/>;
        /// thrown on resolution when the factory returns <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The factory is invoked at most once per registration (lazy singleton semantics) and the produced instance is cached.
        /// </para>
        /// <para>
        /// Resolution is thread-safe: the container ensures that at most one instance is created even under concurrent resolution.
        /// </para>
        /// </remarks>
        void RegisterService(Type serviceType, Type implementationType, Func<object> callback, bool throwIfExists = false);

        /// <summary>Registers a service factory where the implementation type differs from the registration type.</summary>
        /// <param name="serviceType">The registration (service) type.</param>
        /// <param name="implementationType">The concrete implementation type.</param>
        /// <param name="callback">The factory producing the implementation instance (invoked lazily on resolution).</param>
        /// <param name="name">The registration name; empty is treated as unnamed.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/>, <paramref name="implementationType"/>, or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="implementationType"/> is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown during registration when a matching service already exists and <paramref name="throwIfExists"/> is <see langword="true"/>;
        /// thrown on resolution when the factory returns <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The factory is invoked at most once per registration (lazy singleton semantics) and the produced instance is cached.
        /// </para>
        /// <para>
        /// Resolution is thread-safe: the container ensures that at most one instance is created even under concurrent resolution.
        /// </para>
        /// </remarks>
        void RegisterService(Type serviceType, Type implementationType, Func<object> callback, string? name, bool throwIfExists = false);

        /// <summary>Registers a service type to be constructed via its public parameterless constructor on first resolution.</summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when open generics are used.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a service already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The instance is created via the public parameterless constructor on first resolution and then cached.
        /// </para>
        /// <para>
        /// Activation is thread-safe: the container ensures single activation under concurrent resolution.
        /// </para>
        /// </remarks>
        void RegisterService(Type serviceType, bool throwIfExists = false);

        /// <summary>Registers a service type to be constructed via its public parameterless constructor on first resolution.</summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="name">The registration name; empty is treated as unnamed.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when open generics are used.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a service already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The instance is created via the public parameterless constructor on first resolution and then cached.
        /// </para>
        /// <para>
        /// Activation is thread-safe: the container ensures single activation under concurrent resolution.
        /// </para>
        /// </remarks>
        void RegisterService(Type serviceType, string? name, bool throwIfExists = false);

        /// <summary>Registers an implementation type to be constructed on first resolution.</summary>
        /// <param name="serviceType">The registration (service) type.</param>
        /// <param name="implementationType">The concrete implementation type.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="implementationType"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="implementationType"/> is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a service already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>If a matching registration exists and <paramref name="throwIfExists"/> is <see langword="false"/>, the existing registration is replaced.</remarks>
        void RegisterService(Type serviceType, Type implementationType, bool throwIfExists = false);

        /// <summary>Registers an implementation type to be constructed on first resolution.</summary>
        /// <param name="serviceType">The registration (service) type.</param>
        /// <param name="implementationType">The concrete implementation type.</param>
        /// <param name="name">The registration name; empty is treated as unnamed.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="implementationType"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="implementationType"/> is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a service already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>If a matching registration exists and <paramref name="throwIfExists"/> is <see langword="false"/>, the existing registration is replaced.</remarks>
        void RegisterService(Type serviceType, Type implementationType, string? name, bool throwIfExists = false);

        /// <summary>
        /// Unregisters a service instance.
        /// </summary>
        /// <param name="service">The service instance to unregister.</param>
        /// <returns><see langword="true"/> if the service was unregistered; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="service"/> is null.
        /// </exception>
        /// <remarks>
        /// Removes all matching registration whose instance has been created and is currently registered.
        /// If the same instance is registered under multiple keys, only one registration is removed.
        /// </remarks>
        bool UnregisterService(object service);

        /// <summary>
        /// Unregisters the unnamed service of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of the service to unregister.</param>
        /// <returns><see langword="true"/> if a matching service was unregistered; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> is null.
        /// </exception>
        bool UnregisterService(Type serviceType);

        /// <summary>
        /// Unregisters a named service of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of the service to unregister.</param>
        /// <param name="name">The name of the service to unregister.</param>
        /// <returns><see langword="true"/> if a matching service was unregistered; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> is null.
        /// </exception>
        bool UnregisterService(Type serviceType, string? name);

        /// <summary>
        /// Removes all registrations from this container. 
        /// </summary>
        void Clear();

        /// <summary>
        /// Disposes local services using the specified cleanup operation and clears local registrations.
        /// </summary>
        /// <param name="cleanupOperation">Per-service cleanup function (called for each materialized local service).</param>
        /// <param name="continueOnCapturedContext">Whether to marshal continuations back to the captured context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="AggregateException">One or more exceptions occurred during the invocation of cleanup operations.</exception>
        Task CleanupAsync(Func<object, Task> cleanupOperation, bool continueOnCapturedContext);
    }
}
