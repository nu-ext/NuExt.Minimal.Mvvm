using System;
using System.Collections.Generic;
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
        /// Creates a child container (scope) that delegates to this container as parent.
        /// </summary>
        IServiceContainer CreateScope();

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
        /// Retrieves all unique service instances assignable to <paramref name="serviceType"/> by
        /// cascading through the provider hierarchy.
        /// </summary>
        /// <param name="serviceType">The type of services to retrieve.</param>
        /// <returns>
        /// A non-null enumerable whose elements are unique by reference equality within this invocation.
        /// The sequence may be empty. Order is not guaranteed.
        /// </returns>
        /// <remarks>
        /// Enumeration is lazy and may trigger service activation and caching.  
        /// Uniqueness is enforced per invocation through reference comparisons;  
        /// subsequent calls may yield different instances for transient registrations.
        /// Results may include services from a parent provider.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        IEnumerable<object> GetServices(Type serviceType);

        /// <summary>
        /// Retrieves all unique service instances assignable to <paramref name="serviceType"/>,
        /// optionally restricting enumeration to local registrations only.
        /// </summary>
        /// <param name="serviceType">The type of services to retrieve.</param>
        /// <param name="localOnly">
        /// When <see langword="true"/>, only services registered in this container are returned;  
        /// parent services are excluded.
        /// </param>
        /// <returns>
        /// A non-null enumerable whose elements are unique by reference equality within this invocation.  
        /// The sequence may be empty. Order is not guaranteed.
        /// </returns>
        /// <remarks>
        /// Enumeration is lazy and may trigger service activation.
        /// Uniqueness is enforced per invocation through reference comparisons;  
        /// subsequent calls may yield different instances for transient registrations.
        /// When <paramref name="localOnly"/> is <see langword="false"/>, the result may include
        /// services from a parent provider.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        IEnumerable<object> GetServices(Type serviceType, bool localOnly);

        /// <summary>
        /// Retrieves all service instances assignable to <paramref name="serviceType"/>,
        /// optionally restricting enumeration to local registrations and/or excluding transient factories.
        /// </summary>
        /// <param name="serviceType">The type of services to retrieve.</param>
        /// <param name="localOnly">
        /// When <see langword="true"/>, only services registered in this container are returned;  
        /// parent services are excluded.
        /// </param>
        /// <param name="includeTransient">
        /// When <see langword="true"/>, transient factories are invoked during enumeration;  
        /// when <see langword="false"/>, transient factories are skipped.
        /// </param>
        /// <returns>
        /// A non-null enumerable whose elements are unique by reference equality within this invocation.  
        /// The sequence may be empty. Order is not guaranteed.
        /// </returns>
        /// <remarks>
        /// Enumeration is lazy and may trigger service activation.  
        /// Transient factories may be invoked during enumeration and may produce different instances across
        /// separate invocations.  
        /// Uniqueness is enforced per invocation through reference comparisons;  
        /// no identity stability is guaranteed between calls.
        /// When <paramref name="localOnly"/> is <see langword="false"/>, results may include services
        /// from a parent provider.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        IEnumerable<object> GetServices(Type serviceType, bool localOnly, bool includeTransient);

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
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
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
        /// Thrown when open generics are used.
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
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
        /// <param name="throwIfExists">Whether to throw if the service already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when open generics are used.
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
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
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
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
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
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
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
        /// Unregisters all registrations whose materialized instance matches the specified <paramref name="service"/>.
        /// </summary>
        /// <param name="service">The service instance to unregister.</param>
        /// <returns>
        /// <see langword="true"/> if at least one registration was removed; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="service"/> is null.
        /// </exception>
        /// <remarks>
        /// Removes every matching registration whose instance has been created and is currently registered
        /// within this container. If the same instance was registered under multiple keys (e.g., different
        /// names or compatible service types), all such registrations are removed.
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
        /// Disposes materialized local services using the specified cleanup operation and clears cached service registrations.
        /// Transient registrations are preserved.
        /// </summary>
        /// <param name="cleanupOperation">Per-service cleanup function (called for each materialized local service).</param>
        /// <param name="continueOnCapturedContext">Whether to marshal continuations back to the captured context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="AggregateException">One or more exceptions occurred during the invocation of cleanup operations.</exception>
        Task CleanupAsync(Func<object, Task> cleanupOperation, bool continueOnCapturedContext = false);

        /// <summary>
        /// Registers a transient factory under the specified type.
        /// A new instance is created on each resolution; no instance is cached by the container.
        /// </summary>
        /// <param name="serviceType">The type to register the factory for.</param>
        /// <param name="callback">The factory that produces a new instance on each call.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a registration already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// Transient factories are always invoked on resolution. The container does not cache or reuse the created instance.
        /// </remarks>
        void RegisterTransient(Type serviceType, Func<object> callback, bool throwIfExists = false);

        /// <summary>
        /// Registers a transient factory under the specified type and name.
        /// A new instance is created on each resolution; no instance is cached by the container.
        /// </summary>
        /// <param name="serviceType">The type to register the factory for.</param>
        /// <param name="callback">The factory that produces a new instance on each call.</param>
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a registration already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// Transient factories are always invoked on resolution. The container does not cache or reuse the created instance.
        /// </remarks>
        void RegisterTransient(Type serviceType, Func<object> callback, string? name, bool throwIfExists = false);

        /// <summary>
        /// Registers a transient factory where the implementation type differs from the registration type.
        /// A new instance is created on each resolution; no instance is cached by the container.
        /// </summary>
        /// <param name="serviceType">The registration (service) type.</param>
        /// <param name="implementationType">The concrete implementation type.</param>
        /// <param name="callback">The factory producing a new implementation instance on each call.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/>, <paramref name="implementationType"/>, or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="implementationType"/> is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a registration already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// Transient factories are always invoked on resolution. The container does not cache or reuse the created instance.
        /// </remarks>
        void RegisterTransient(Type serviceType, Type implementationType, Func<object> callback, bool throwIfExists = false);

        /// <summary>
        /// Registers a transient factory where the implementation type differs from the registration type and a name is provided.
        /// A new instance is created on each resolution; no instance is cached by the container.
        /// </summary>
        /// <param name="serviceType">The registration (service) type.</param>
        /// <param name="implementationType">The concrete implementation type.</param>
        /// <param name="callback">The factory producing a new implementation instance on each call.</param>
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/>, <paramref name="implementationType"/>, or <paramref name="callback"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="implementationType"/> is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a registration already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// Transient factories are always invoked on resolution. The container does not cache or reuse the created instance.
        /// </remarks>
        void RegisterTransient(Type serviceType, Type implementationType, Func<object> callback, string? name, bool throwIfExists = false);

        /// <summary>
        /// Registers a transient constructor activation under the specified type.
        /// A new instance is created via the public parameterless constructor on each resolution; no instance is cached.
        /// </summary>
        /// <param name="serviceType">The type to register the transient activation for.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when open generics are used.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a registration already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// Transient activations always create a new instance on resolution. The container does not cache or reuse the created instance.
        /// </remarks>
        void RegisterTransient(Type serviceType, bool throwIfExists = false);

        /// <summary>
        /// Registers a transient constructor activation under the specified type and name.
        /// A new instance is created via the public parameterless constructor on each resolution; no instance is cached.
        /// </summary>
        /// <param name="serviceType">The type to register the transient activation for.</param>
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceType"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when open generics are used.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a registration already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// Transient activations always create a new instance on resolution. The container does not cache or reuse the created instance.
        /// </remarks>
        void RegisterTransient(Type serviceType, string? name, bool throwIfExists = false);

        /// <summary>
        /// Registers a transient constructor activation where the implementation type differs from the registration type.
        /// A new instance is created via the implementation's public parameterless constructor on each resolution; no instance is cached.
        /// </summary>
        /// <param name="serviceType">The registration (service) type.</param>
        /// <param name="implementationType">The concrete implementation type.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="implementationType"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="implementationType"/> is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a registration already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// Transient activations always create a new instance on resolution. The container does not cache or reuse the created instance.
        /// </remarks>
        void RegisterTransient(Type serviceType, Type implementationType, bool throwIfExists = false);

        /// <summary>
        /// Registers a transient constructor activation where the implementation type differs from the registration type and a name is provided.
        /// A new instance is created via the implementation's public parameterless constructor on each resolution; no instance is cached.
        /// </summary>
        /// <param name="serviceType">The registration (service) type.</param>
        /// <param name="implementationType">The concrete implementation type.</param>
        /// <param name="name">The registration name; empty is treated as unnamed. Name comparison is ordinal and case-sensitive.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="serviceType"/> or <paramref name="implementationType"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="implementationType"/> is not assignable to <paramref name="serviceType"/> or when open generics are used.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a registration already exists and <paramref name="throwIfExists"/> is true.
        /// </exception>
        /// <remarks>
        /// Transient activations always create a new instance on resolution. The container does not cache or reuse the created instance.
        /// </remarks>
        void RegisterTransient(Type serviceType, Type implementationType, string? name, bool throwIfExists = false);
    }
}
