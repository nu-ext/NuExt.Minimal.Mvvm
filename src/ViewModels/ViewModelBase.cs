using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Base class for ViewModels providing property change notification and a hierarchical service location mechanism.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements the <strong>Service Locator pattern</strong> via its 
    /// <see cref="GetService{T}(System.String)"/> method.
    /// This design is a pragmatic choice for composite UI and MVVM scenarios where:
    /// <list type="bullet">
    /// <item>Dependencies cannot be fully known at ViewModel construction time.</item>
    /// <item>Child ViewModels need to resolve context-specific services from a parent.</item>
    /// <item>UI Behaviors or other runtime components must inject services into an existing ViewModel instance.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract class ViewModelBase : BindableBase, IAsyncInitializable, IParentedViewModel, IParameterizedViewModel, IServiceContainerProvider, INamedServiceProvider, IServicesProvider
    {
        private readonly Lazy<IServiceContainer> _services;
        private readonly IServiceContainer? _fallbackServices;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        protected ViewModelBase() : this(fallbackServices: null)
        {
        }

        /// <summary>Initializes the ViewModel with an optional fallback service container used when local and parent resolution miss.</summary>
        /// <param name="fallbackServices">The fallback container. May be <see langword="null"/>.</param>
        protected ViewModelBase(IServiceContainer? fallbackServices)
        {
            _fallbackServices = fallbackServices;
            _services = new Lazy<IServiceContainer>(() => new ServiceProvider(this));
        }

        #region Properties

        private volatile bool _isInitialized;
        /// <inheritdoc/>
        public bool IsInitialized => _isInitialized;

        /// <inheritdoc/>
        public object? Parameter
        {
            get;
            set
            {
                if (field == value) return;
                if (!CanSetProperty(field, value)) return;
                field = value;
                OnPropertyChanged(EventArgsCache.ParameterPropertyChanged);
            }
        }

        /// <inheritdoc/>
        public object? ParentViewModel
        {
            get;
            set
            {
                var oldValue = field;
                if (field == value) return;
                if (value is IParentedViewModel parent)
                {
                    var current = parent;
                    while (current != null)
                    {
                        if (ReferenceEquals(current, this))
                        {
                            ThrowInvalidParentViewModelAssignment();
                        }
                        current = current.ParentViewModel as IParentedViewModel;
                    }
                }

                if (!CanSetProperty(oldValue, value)) return;
                field = value;
                OnPropertyChanged(EventArgsCache.ParentViewModelPropertyChanged);
                OnParentViewModelChanged(oldValue, value);
            }
        }

        /// <summary>Gets the service container for dependency injection and service resolution.</summary>
        /// <remarks>
        /// The container is created lazily on first access and delegates to this instance for parent resolution.
        /// Publication is thread-safe; subsequent accesses return the same instance.
        /// </remarks>
        public IServiceContainer Services => _services.Value;

        protected bool IsServicesCreated => _services.IsValueCreated;

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called after <see cref="ParentViewModel"/> has changed.
        /// </summary>
        /// <param name="oldValue">Previous parent.</param>
        /// <param name="newValue">New parent.</param>
        /// <remarks>
        /// Override to update parent-dependent state or subscriptions.
        /// </remarks>
        protected virtual void OnParentViewModelChanged(object? oldValue, object? newValue)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>An instance of the requested service, or <see langword="null"/> if the service is not available.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetService<T>() where T : class
        {
            return (T?)((INamedServiceProvider)this).GetService(typeof(T), name: null);
        }

        /// <summary>
        /// Gets the named service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <param name="name">
        /// The name of the service to resolve. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <returns>An instance of the requested service, or <see langword="null"/> if the service is not available.</returns>
        /// <remarks>Empty name is treated as unnamed.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetService<T>(string? name) where T : class
        {
            return (T?)((INamedServiceProvider)this).GetService(typeof(T), name: name);
        }

        /// <summary>
        /// Gets the service of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service to get.</param>
        /// <returns>A service of type <paramref name="serviceType"/>. 
        /// If there is no service of type <paramref name="serviceType"/>, returns <see langword="null"/>.</returns>
        object? IServiceProvider.GetService(Type serviceType)
        {
            return ((INamedServiceProvider)this).GetService(serviceType, name: null);
        }

        /// <summary>
        /// Gets the named service of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service to get.</param>
        /// <param name="name">
        /// The name of the service to resolve. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <returns>
        /// A service of type <paramref name="serviceType"/>; or <see langword="null"/> if not found.
        /// </returns>
        /// <remarks>
        /// <para>Empty name is treated as unnamed.</para>
        /// <para>
        /// Resolution order: 1) local container (<see cref="Services"/>), 2) fallback container (if any),
        /// 3) parent ViewModel (named lookup if supported), 4) <see cref="ServiceProvider.Default"/>.
        /// </para>
        /// </remarks>
        object? INamedServiceProvider.GetService(Type serviceType, string? name)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(serviceType);
#else
            _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
#endif
            var service = Services.GetService(serviceType, name);
            if (service != null) return service;

            service = _fallbackServices?.GetService(serviceType, name);
            if (service != null) return service;

            switch (ParentViewModel)
            {
                case INamedServiceProvider parent:
                    service = parent.GetService(serviceType, name);
                    break;
                case IServiceProvider serviceProvider when name is null:
                    service = serviceProvider.GetService(serviceType);
                    break;
            }
            return service ?? ServiceProvider.Default.GetService(serviceType, name);
        }

        /// <summary>
        /// Retrieves all unique service instances of the specified type by cascading through the ViewModel chain
        /// and the configured fallback container. Does not include <see cref="ServiceProvider.Default"/>.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>
        /// An enumerable sequence of distinct service instances of type <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>Does not consult <see cref="ServiceProvider.Default"/>.</remarks>
        public IEnumerable<T> GetServices<T>()
        {
            foreach (var service in GetServices(typeof(T)))
            {
                yield return (T)service;
            }
        }

        /// <summary>
        /// Retrieves all unique service instances of the specified type by cascading through the ViewModel chain
        /// and the configured fallback container. Does not include <see cref="ServiceProvider.Default"/>.
        /// </summary>
        /// <param name="serviceType">The type of services to retrieve.</param>
        /// <returns>
        /// An enumerable sequence of distinct service instances of the specified type.
        /// </returns>
        /// <remarks>
        /// Enumerates services only from scoped sources (local → fallback → parent).
        /// Does not consult <see cref="ServiceProvider.Default"/> by design to keep enumeration deterministic.
        /// </remarks>
        public IEnumerable<object> GetServices(Type serviceType)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(serviceType);
#else
            _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
#endif
            HashSet<object>? services = null;
            foreach (var service in Services.GetServices(serviceType))
            {
                Debug.Assert(service != null, $"Service is null for type {serviceType}");
                if (service != null && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(service))
                {
                    yield return service;
                }
            }

            if (_fallbackServices != null)
            {
                foreach (var service in _fallbackServices.GetServices(serviceType))
                {
                    Debug.Assert(service != null, $"Service is null for type {serviceType}");
                    if (service != null && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(service))
                    {
                        yield return service;
                    }
                }
            }

            switch (ParentViewModel)
            {
                case IServicesProvider servicesProvider:
                    foreach (var service in servicesProvider.GetServices(serviceType))
                    {
                        Debug.Assert(service != null, $"Service is null for type {serviceType}");
                        if (service != null && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(service))
                        {
                            yield return service;
                        }
                    }
                    break;
                case INamedServiceProvider namedServiceProvider:
                    var nullNamedService = namedServiceProvider.GetService(serviceType, null);
                    if (nullNamedService != null && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(nullNamedService))
                    {
                        yield return nullNamedService;
                    }
                    break;
                case IServiceProvider serviceProvider:
                    var typedService = serviceProvider.GetService(serviceType);
                    if (typedService != null && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(typedService))
                    {
                        yield return typedService;
                    }
                    break;
            }
        }

        /// <summary>Gets a service of type <typeparamref name="T"/> from the local container only.</summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>An instance of the requested service, or <see langword="null"/> if the service is not available.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T? GetLocalService<T>() where T : class
        {
            return GetLocalService<T>(name: null);
        }

        /// <summary>Gets a named service of type <typeparamref name="T"/> from the local container only.</summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <param name="name">
        /// The name of the service to resolve. This can be used to distinguish between multiple services of the same type.
        /// </param>
        /// <returns>An instance of the requested service, or <see langword="null"/> if the service is not available.</returns>
        /// <remarks>
        /// Name comparison is ordinal and case-sensitive; empty is treated as unnamed.
        /// Does not consult the fallback container, parent, or the default provider.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T? GetLocalService<T>(string? name) where T : class
        {
            return (T?)Services.GetService(typeof(T), name, localOnly: true);
        }

        /// <summary>Gets all services of type <typeparamref name="T"/> from the local container only.</summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>
        /// An enumerable sequence of distinct service instances of type <typeparamref name="T"/>.
        /// </returns>
        protected IEnumerable<T> GetLocalServices<T>() where T : class
        {
            foreach (var service in Services.GetServices(typeof(T), localOnly: true))
            {
                yield return (T)service;
            }
        }

        /// <summary>
        /// Gets a service from the local container; if not found, tries the optional fallback container.
        /// Does not consult ParentViewModel or ServiceProvider.Default.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T? GetLocalOrFallbackService<T>() where T : class
        {
            return GetLocalOrFallbackService<T>(name: null);
        }

        /// <summary>
        /// Gets a service from the local container; if not found, tries the optional fallback container.
        /// Does not consult ParentViewModel or ServiceProvider.Default.
        /// </summary>
        /// <remarks>Name comparison is ordinal and case-sensitive; empty is treated as unnamed.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T? GetLocalOrFallbackService<T>(string? name) where T : class
        {
            return (T?)Services.GetService(typeof(T), name, localOnly: true) ?? (T?)_fallbackServices?.GetService(typeof(T), name);
        }

        /// <summary>
        /// Asynchronously initializes the ViewModel. This method is thread-safe and idempotent.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token to cancel the initialization process.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        /// <remarks>
        /// If the ViewModel is already initialized, the method returns immediately.
        /// Otherwise, it acquires an initialization lock, calls <see cref="InitializeAsyncCore"/>,
        /// updates the initialized state, and notifies property change after releasing the lock.
        /// </remarks>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized) return;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_isInitialized) return;

                await InitializeAsyncCore(cancellationToken);

                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }

            OnPropertyChanged(EventArgsCache.IsInitializedPropertyChanged);
        }

        /// <summary>
        /// Asynchronously uninitializes the ViewModel if it was initialized. This method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token to cancel the uninitialization process.</param>
        /// <returns>A task that represents the asynchronous uninitialization operation.</returns>
        /// <remarks>
        /// If the ViewModel is not initialized, the method returns immediately.
        /// Otherwise, it acquires the initialization lock, calls <see cref="UninitializeAsyncCore"/>,
        /// updates the initialized state, and notifies property change after releasing the lock.
        /// </remarks>
        public async Task UninitializeAsync(CancellationToken cancellationToken = default)
        {
            if (!_isInitialized) return;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (!_isInitialized) return;

                await UninitializeAsyncCore(cancellationToken);
                _isInitialized = false;
            }
            finally
            {
                _initLock.Release();
            }

            OnPropertyChanged(EventArgsCache.IsInitializedPropertyChanged);
        }

        /// <summary>
        /// When overridden in a derived class, asynchronously performs the initialization logic for the ViewModel.
        /// This method should contain any custom initialization logic required by the derived ViewModel class.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the initialization process.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        protected virtual Task InitializeAsyncCore(CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
        }

        /// <summary>
        /// When overridden in a derived class, asynchronously performs the uninitialization logic for the ViewModel.
        /// This method should contain any custom uninitialization logic required by the derived ViewModel class.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the uninitialization process.</param>
        /// <returns>A task that represents the asynchronous uninitialization operation.</returns>
        protected virtual Task UninitializeAsyncCore(CancellationToken cancellationToken)
        {
            if (!IsServicesCreated)
            {
                return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
            }
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken)
                : Services.CleanupAsync(static s => { (s as IDisposable)?.Dispose(); return Task.CompletedTask; }, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowInvalidParentViewModelAssignment()
        {
            throw new InvalidOperationException("Cyclic parent reference detected.");
        }

        #endregion
    }

    internal static partial class EventArgsCache
    {
        internal static readonly PropertyChangedEventArgs IsInitializedPropertyChanged = new(nameof(ViewModelBase.IsInitialized));
        internal static readonly PropertyChangedEventArgs ParameterPropertyChanged = new(nameof(ViewModelBase.Parameter));
        internal static readonly PropertyChangedEventArgs ParentViewModelPropertyChanged = new(nameof(ViewModelBase.ParentViewModel));
    }
}
