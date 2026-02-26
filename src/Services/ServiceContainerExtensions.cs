using System;
using System.Collections.Generic;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Extension methods for <see cref="IServiceContainer"/>.
    /// </summary>
    public static class ServiceContainerExtensions
    {
        #region Resolving

        /// <summary>
        /// Gets a service object of type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of service object to get.</typeparam>
        /// <param name="container">The service container.</param>
        /// <returns>A service object of type <typeparamref name="TService"/> or null if there is no such service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static TService? GetService<TService>(this IServiceContainer container) where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            return (TService?)container.GetService(serviceType: typeof(TService), name: null);
        }

        /// <summary>
        /// Gets a named service object of type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of service object to get.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="name">
        /// The name of the service to resolve. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <returns>A service object of type <typeparamref name="TService"/> or null if there is no such service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static TService? GetService<TService>(this IServiceContainer container, string? name) where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            return (TService?)container.GetService(serviceType: typeof(TService), name: name);
        }

        /// <summary>Gets a service of type <typeparamref name="TService"/> or throws if not found.</summary>
        /// <typeparam name="TService">The type of service object to get.</typeparam>
        /// <param name="container">The service container.</param>
        /// <returns>A service object of type <typeparamref name="TService"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
        public static TService GetRequiredService<TService>(this IServiceContainer container) where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            return (TService?)container.GetService(typeof(TService), name: null)
                   ?? throw new InvalidOperationException($"Service of type {typeof(TService)} is not registered.");
        }

        /// <summary>Gets a named service of type <typeparamref name="TService"/> or throws if not found.</summary>
        /// <typeparam name="TService">The type of service object to get.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="name">
        /// The name of the service to resolve. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <returns>A service object of type <typeparamref name="TService"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
        public static TService GetRequiredService<TService>(this IServiceContainer container, string? name) where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            return (TService?)container.GetService(typeof(TService), name)
                   ?? throw new InvalidOperationException(
                       $"Service of type {typeof(TService)} with name '{name}' is not registered.");
        }

        /// <summary>Gets a service of type <typeparamref name="TService"/> from the local container only.</summary>
        /// <typeparam name="TService">The type of service object to get.</typeparam>
        /// <param name="container">The service container.</param>
        /// <returns>A service object of type <typeparamref name="TService"/> or null if there is no such service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static TService? GetLocalService<TService>(this IServiceContainer container)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            return (TService?)container.GetService(typeof(TService), name: null, localOnly: true);
        }

        /// <summary>Gets a named service of type <typeparamref name="TService"/> from the local container only.</summary>
        /// <typeparam name="TService">The type of service object to get.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="name">
        /// The name of the service to resolve. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <returns>A service object of type <typeparamref name="TService"/> or null if there is no such service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static TService? GetLocalService<TService>(this IServiceContainer container, string? name)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            return (TService?)container.GetService(typeof(TService), name, localOnly: true);
        }

        /// <summary>
        /// Gets all registered services of type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of service object to get.</typeparam>
        /// <param name="container">The service container.</param>
        /// <returns>
        /// A non-null enumerable of distinct instances (by reference); may be empty. Order is not guaranteed.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        /// <exception cref="InvalidCastException">
        /// Thrown when a registered service cannot be cast to <typeparamref name="TService"/>.
        /// This indicates a configuration error where a service was registered under an incompatible type.
        /// </exception>
        /// <remarks>Enumeration is lazy and may trigger service activation; parent provider results may be included.</remarks>
        public static IEnumerable<TService> GetServices<TService>(this IServiceContainer container) where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            foreach (var service in container.GetServices(typeof(TService)))
            {
                yield return (TService)service;
            }
        }

        /// <summary>Gets all local services of type <typeparamref name="TService"/>.</summary>
        /// <typeparam name="TService">The type of service object to get.</typeparam>
        /// <param name="container">The service container.</param>
        /// <returns>
        /// A non-null enumerable of distinct instances (by reference); may be empty. Order is not guaranteed.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        /// <exception cref="InvalidCastException">
        /// Thrown when a registered service cannot be cast to <typeparamref name="TService"/>.
        /// This indicates a configuration error where a service was registered under an incompatible type.
        /// </exception>
        /// <remarks>
        /// Enumeration is lazy and may trigger service activation.
        /// </remarks>
        public static IEnumerable<TService> GetLocalServices<TService>(this IServiceContainer container)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            foreach (var service in container.GetServices(typeof(TService), localOnly: true))
            {
                yield return (TService)service;
            }
        }

        #endregion

        #region Scope

        /// <summary>
        /// Registers a service instance under the specified type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type to register the service as.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="service">The service instance to register.</param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static void RegisterService<TService>(this IServiceContainer container, TService service, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterService(serviceType: typeof(TService), service: service, name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named service instance under the specified type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type to register the service as.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="service">The service instance to register.</param>
        /// <param name="name">
        /// The name of the service to register. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static void RegisterService<TService>(this IServiceContainer container, TService service, string? name, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterService(serviceType: typeof(TService), service: service, name: name, throwIfExists);
        }

        /// <summary>
        /// Registers a service factory under the specified type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type to register the service as.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="callback">The factory function to create the service instance.</param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> or <paramref name="callback"/> is null.</exception>
        public static void RegisterService<TService>(this IServiceContainer container, Func<TService> callback, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(callback);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
            container.RegisterService(serviceType: typeof(TService), callback: callback, name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named service factory under the specified type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type to register the service as.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="callback">The factory function to create the service instance.</param>
        /// <param name="name">
        /// The name of the service to register. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> or <paramref name="callback"/> is null.</exception>
        public static void RegisterService<TService>(this IServiceContainer container, Func<TService> callback, string? name, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(callback);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
            container.RegisterService(serviceType: typeof(TService), callback: callback, name: name, throwIfExists);
        }

        /// <summary>
        /// Registers a service factory where the implementation type differs from the registration type.
        /// </summary>
        /// <typeparam name="TService">The type to register the service as (typically an interface or base class).</typeparam>
        /// <typeparam name="TImplementation">The concrete type that will be created by the factory.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="callback">The factory function to create the service instance.</param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> or <paramref name="callback"/> is null.</exception>
        public static void RegisterService<TService, TImplementation>(this IServiceContainer container, Func<TImplementation> callback, bool throwIfExists = false)
            where TService : class
            where TImplementation : class, TService
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(callback);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
            container.RegisterService(serviceType: typeof(TService), implementationType: typeof(TImplementation), callback: callback, name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named service factory where the implementation type differs from the registration type.
        /// </summary>
        /// <typeparam name="TService">The type to register the service as (typically an interface or base class).</typeparam>
        /// <typeparam name="TImplementation">The concrete type that will be created by the factory.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="callback">The factory function to create the service instance.</param>
        /// <param name="name">
        /// The name of the service to register. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> or <paramref name="callback"/> is null.</exception>
        public static void RegisterService<TService, TImplementation>(this IServiceContainer container, Func<TImplementation> callback, string? name, bool throwIfExists = false)
            where TService : class
            where TImplementation : class, TService
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(callback);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
            container.RegisterService(serviceType: typeof(TService), implementationType: typeof(TImplementation), callback: callback, name: name, throwIfExists);
        }

        /// <summary>
        /// Registers a service of the specified type <typeparamref name="TService"/> by invoking its default constructor.
        /// </summary>
        /// <typeparam name="TService">The type of the service to register.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static void RegisterService<TService>(this IServiceContainer container, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterService(serviceType: typeof(TService), name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named service of the specified type <typeparamref name="TService"/> by invoking its default constructor.
        /// </summary>
        /// <typeparam name="TService">The type of the service to register.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="name">
        /// The name of the service to register. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static void RegisterService<TService>(this IServiceContainer container, string? name, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterService(serviceType: typeof(TService), name: name, throwIfExists);
        }

        /// <summary>
        /// Registers a service type where the implementation type differs from the registration type.
        /// </summary>
        /// <typeparam name="TService">The type to register the service as (typically an interface or base class).</typeparam>
        /// <typeparam name="TImplementation">The concrete type to instantiate when resolving the service.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static void RegisterService<TService, TImplementation>(this IServiceContainer container, bool throwIfExists = false)
            where TService : class
            where TImplementation : class, TService
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterService(serviceType: typeof(TService), implementationType: typeof(TImplementation), name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named service type where the implementation type differs from the registration type.
        /// </summary>
        /// <typeparam name="TService">The type to register the service as (typically an interface or base class).</typeparam>
        /// <typeparam name="TImplementation">The concrete type to instantiate when resolving the service.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="name">
        /// The name of the service to register. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <param name="throwIfExists">Specifies whether to throw an exception if the service already exists.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static void RegisterService<TService, TImplementation>(this IServiceContainer container, string? name, bool throwIfExists = false)
            where TService : class
            where TImplementation : class, TService
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterService(serviceType: typeof(TService), implementationType: typeof(TImplementation), name: name, throwIfExists);
        }

        /// <summary>
        /// Unregisters a service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the service to unregister.</typeparam>
        /// <param name="container">The service container.</param>
        /// <returns><see langword="true"/> if the service was successfully unregistered; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static bool UnregisterService<TService>(this IServiceContainer container) where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            return container.UnregisterService(serviceType: typeof(TService), name: null);
        }

        /// <summary>
        /// Unregisters a named service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the service to unregister.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="name">
        /// The name of the service to unregister. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <returns><see langword="true"/> if the service was successfully unregistered; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null.</exception>
        public static bool UnregisterService<TService>(this IServiceContainer container, string? name) where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            return container.UnregisterService(serviceType: typeof(TService), name: name);
        }

        #endregion

        #region Transient

        /// <summary>
        /// Registers a transient factory under the specified type <typeparamref name="TService"/>.
        /// A new instance is created on each resolution; no instance is cached by the container.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="callback">The factory that produces a new instance on each call.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="container"/> or <paramref name="callback"/> is null.
        /// </exception>
        public static void RegisterTransient<TService>(this IServiceContainer container, Func<TService> callback, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(callback);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
            container.RegisterTransient(serviceType: typeof(TService), callback: callback, name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named transient factory under the specified type <typeparamref name="TService"/>.
        /// A new instance is created on each resolution; no instance is cached by the container.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="callback">The factory that produces a new instance on each call.</param>
        /// <param name="name">
        /// The registration name. <see langword="null"/> or empty selects unnamed registrations. Name comparison is ordinal and case-sensitive.
        /// </param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="container"/> or <paramref name="callback"/> is null.
        /// </exception>
        public static void RegisterTransient<TService>(this IServiceContainer container, Func<TService> callback, string? name, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(callback);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
            container.RegisterTransient(serviceType: typeof(TService), callback: callback, name: name, throwIfExists);
        }

        /// <summary>
        /// Registers a transient factory where the implementation type differs from the registration type.
        /// A new instance is created on each resolution; no instance is cached by the container.
        /// </summary>
        /// <typeparam name="TService">The registration (service) type.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type produced by the factory.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="callback">The factory producing a new implementation instance on each call.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="container"/> or <paramref name="callback"/> is null.
        /// </exception>
        public static void RegisterTransient<TService, TImplementation>(this IServiceContainer container, Func<TImplementation> callback, bool throwIfExists = false)
            where TService : class
            where TImplementation : class, TService
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(callback);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
            container.RegisterTransient(serviceType: typeof(TService), implementationType: typeof(TImplementation), callback: callback, name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named transient factory where the implementation type differs from the registration type.
        /// A new instance is created on each resolution; no instance is cached by the container.
        /// </summary>
        /// <typeparam name="TService">The registration (service) type.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type produced by the factory.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="callback">The factory producing a new implementation instance on each call.</param>
        /// <param name="name">
        /// The registration name. <see langword="null"/> or empty selects unnamed registrations. Name comparison is ordinal and case-sensitive.
        /// </param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="container"/> or <paramref name="callback"/> is null.
        /// </exception>
        public static void RegisterTransient<TService, TImplementation>(this IServiceContainer container, Func<TImplementation> callback, string? name, bool throwIfExists = false)
            where TService : class
            where TImplementation : class, TService
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(callback);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
            container.RegisterTransient(serviceType: typeof(TService), implementationType: typeof(TImplementation), callback: callback, name: name, throwIfExists);
        }

        /// <summary>
        /// Registers a transient constructor activation under the specified type <typeparamref name="TService"/>.
        /// A new instance is created via the public parameterless constructor on each resolution; no instance is cached.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="container"/> is null.
        /// </exception>
        public static void RegisterTransient<TService>(this IServiceContainer container, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterTransient(serviceType: typeof(TService), name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named transient constructor activation under the specified type <typeparamref name="TService"/>.
        /// A new instance is created via the public parameterless constructor on each resolution; no instance is cached.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="name">
        /// The registration name. <see langword="null"/> or empty selects unnamed registrations. Name comparison is ordinal and case-sensitive.
        /// </param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="container"/> is null.
        /// </exception>
        public static void RegisterTransient<TService>(this IServiceContainer container, string? name, bool throwIfExists = false)
            where TService : class
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterTransient(serviceType: typeof(TService), name: name, throwIfExists);
        }

        /// <summary>
        /// Registers a transient constructor activation where the implementation type differs from the registration type.
        /// A new instance is created via the implementation's public parameterless constructor on each resolution; no instance is cached.
        /// </summary>
        /// <typeparam name="TService">The registration (service) type.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type to instantiate.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="container"/> is null.
        /// </exception>
        public static void RegisterTransient<TService, TImplementation>(this IServiceContainer container, bool throwIfExists = false)
            where TService : class
            where TImplementation : class, TService
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterTransient(serviceType: typeof(TService), implementationType: typeof(TImplementation), name: null, throwIfExists);
        }

        /// <summary>
        /// Registers a named transient constructor activation where the implementation type differs from the registration type.
        /// A new instance is created via the implementation's public parameterless constructor on each resolution; no instance is cached.
        /// </summary>
        /// <typeparam name="TService">The registration (service) type.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type to instantiate.</typeparam>
        /// <param name="container">The service container.</param>
        /// <param name="name">
        /// The registration name. <see langword="null"/> or empty selects unnamed registrations. Name comparison is ordinal and case-sensitive.
        /// </param>
        /// <param name="throwIfExists">Whether to throw if a registration with the same key already exists.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="container"/> is null.
        /// </exception>
        public static void RegisterTransient<TService, TImplementation>(this IServiceContainer container, string? name, bool throwIfExists = false)
            where TService : class
            where TImplementation : class, TService
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            _ = container ?? throw new ArgumentNullException(nameof(container));
#endif
            container.RegisterTransient(serviceType: typeof(TService), implementationType: typeof(TImplementation), name: name, throwIfExists);
        }

        #endregion
    }
}
