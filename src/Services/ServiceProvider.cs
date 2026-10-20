using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm;

/// <summary>
/// Provides a thread-safe service container supporting named registrations, lazy singleton creation,
/// and optional parent delegation.
/// </summary>
/// <remarks>
/// <para>Thread safety:</para>
/// <list type="bullet">
///   <item><description>Registration and resolution are thread-safe.</description></item>
///   <item><description>Lazy initialization creates at most one instance per registration under concurrent resolution.</description></item>
/// </list>
/// <para>Dependency cycles:</para>
/// <list type="bullet">
///   <item><description>When <see cref="ThrowOnCircularDependency"/> is <see langword="true"/>, a cycle detection
///   is performed per thread and an <see cref="InvalidOperationException"/> is thrown with a readable chain.</description></item>
/// </list>
/// <para>Parent provider:</para>
/// <list type="bullet">
///   <item><description>Reentrancy to the parent is guarded to avoid cross-level infinite recursion.</description></item>
/// </list>
/// <para>Disposal:</para>
/// <list type="bullet">
///   <item><description>Services are not disposed by default when unregistered or cleared. Override
///   <see cref="OnServiceRemoved(object)"/> to customize.</description></item>
/// </list>
/// <para>Generics:</para>
/// <list type="bullet">
///   <item><description>Open generic registrations are not supported.</description></item>
/// </list>
/// </remarks>
public class ServiceProvider : IServiceContainer, IServicesProvider, IDisposable
{
    #region Nested Structs

    private readonly record struct ServiceKey(Type Type, string? Name);

    private readonly record struct ServiceValue
    {
        private sealed class Lazy
        {
            private volatile ServiceProvider? _owner;
            private bool _initialized;
            private Func<object>? _factory;
            private object? _syncLock;
            private object? _value;
            private long _order;//creation order

            public Lazy(ServiceProvider owner, object value)
            {
                _value = value;
                _initialized = true;
                _order = owner.NextOrder();
            }

            public Lazy(ServiceProvider owner, Func<object> valueFactory)
            {
                _owner = owner;
                _factory = valueFactory;
                _order = owner.NextOrder();
            }

            public bool IsValueCreated => Volatile.Read(ref _initialized);

            public long Order => Volatile.Read(ref _order);

            public object? Value
            {
                get
                {
                    if (Volatile.Read(ref _initialized))
                    {
                        return _value;
                    }

                    var value = LazyInitializer.EnsureInitialized(ref _value, ref _initialized, ref _syncLock, _factory!);

                    var current = Volatile.Read(ref _order);
                    var owner = _owner;
                    if (owner != null)
                    {
                        Interlocked.CompareExchange(ref _order, owner.NextOrder(), current);
                    }

                    // clear references to allow GC of captured state
                    _owner = null;
                    _factory = null;
                    _syncLock = null;
                    return value;
                }
            }
        }

        private readonly Lazy _lazy;

        public ServiceValue(ServiceProvider owner, object service)
        {
            Debug.Assert(service is not Func<object?> && service is not Type);
            _lazy = new Lazy(owner, service);
            Debug.Assert(IsCreated);
        }

        public ServiceValue(ServiceProvider owner, Func<object> serviceFactory)
        {
            _lazy = new Lazy(owner, serviceFactory);
            Debug.Assert(!IsCreated);
        }

        public bool IsCreated => _lazy.IsValueCreated;

        public object Service => _lazy.Value!;

        public long Order => _lazy.Order;
    }

    private readonly struct ServiceRegistration
    {
        private readonly ServiceValue _value;
        private readonly Func<object>? _factory;

        private ServiceRegistration(ServiceValue value)
        {
            _value = value;
        }

        private ServiceRegistration(Func<object> factory)
        {
            _factory = factory;
        }

        public bool IsSingleton => _factory is null;

        public bool IsTransient => _factory is not null;

        public ServiceValue Value
        {
            get
            {
                Debug.Assert(IsSingleton);
                return _value;
            }
        }

        public object GetService()
        {
            return _factory is null ? _value.Service : _factory!();
        }

        public static ServiceRegistration Singleton(ServiceValue value)
        {
            return new ServiceRegistration(value);
        }

        public static ServiceRegistration Transient(Func<object> factory)
        {
            return new ServiceRegistration(factory);
        }
    }

    private ref struct Scope
    {
        private ServiceProvider? _provider;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Scope(ServiceProvider provider)
        {
            _provider = provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            var provider = _provider;
            if (provider != null)
            {
                _provider = null;
                provider.ExitScope();
            }
        }
    }

    #endregion

    private static readonly IServiceContainer s_default = new ServiceProvider();
    private static volatile IServiceContainer? s_custom;

    private readonly ConcurrentDictionary<ServiceKey, ServiceRegistration> _registrations = new();

    private readonly IServiceProvider? _parentProvider;
    private readonly Func<Type, string?, object?>? _upstreamResolve;
    private readonly Func<Type, IEnumerable<object>>? _upstreamEnumerate;

    private volatile int _disposed; // 0 = false, 1 = true
    private long _orderSeq;

    /// <summary>
    /// Thread-local guard flag preventing reentrant calls to the parent service provider.
    /// </summary>
    /// <remarks>
    /// When set to <see langword="true"/>, indicates that this container is currently querying
    /// its parent provider. Any subsequent attempt to resolve services through the parent
    /// will be blocked to avoid circular resolution between container levels.
    /// </remarks>
    private readonly ThreadLocal<bool> _parentReentrancyGuard = new();

    /// <summary>
    /// Thread-local stack tracking the service resolution chain for circular dependency detection.
    /// </summary>
    /// <remarks>
    /// Maintains the current resolution path within this container. Each service key is pushed
    /// onto the stack during resolution and checked for duplicates to detect cycles.
    /// The stack tracks the resolution path; cycle checks are performed only when <see cref="ThrowOnCircularDependency"/> is <see langword="true"/>.
    /// </remarks>
    private readonly ThreadLocal<Stack<ServiceKey>> _dependencyValidationStack = new(() => new Stack<ServiceKey>());

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceProvider"/> class.
    /// </summary>
    public ServiceProvider()
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceProvider"/> class with a parent service provider.
    /// </summary>
    /// <param name="parentServiceProvider">The parent service provider.</param>
    public ServiceProvider(IServiceProvider parentServiceProvider)
    {
        _parentProvider = parentServiceProvider ?? throw new ArgumentNullException(nameof(parentServiceProvider));
    }

    internal ServiceProvider(Func<Type, string?, object?> upstreamResolve, Func<Type, IEnumerable<object>> upstreamEnumerate)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(upstreamResolve);
        ArgumentNullException.ThrowIfNull(upstreamEnumerate);
#else
        _ = upstreamResolve ?? throw new ArgumentNullException(nameof(upstreamResolve));
        _ = upstreamEnumerate ?? throw new ArgumentNullException(nameof(upstreamEnumerate));
#endif
        _upstreamResolve = upstreamResolve;
        _upstreamEnumerate = upstreamEnumerate;
    }

    #region Properties

    /// <summary>
    /// Gets or sets the default service provider instance.
    /// </summary>
    /// <remarks>
    /// Setting this property to <see langword="null"/> resets the default provider to the built-in instance.
    /// </remarks>
    public static IServiceContainer Default
    {
        get => s_custom ?? s_default;
        set => s_custom = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether an <see cref="InvalidOperationException"/>
    /// is thrown when a circular dependency is detected.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to throw an exception on circular dependency detection; otherwise, <see langword="false"/>.
    /// The default value is <see langword="true"/>.
    /// </value>
    /// <remarks>
    /// When set to <see langword="false"/>, the container will not validate dependency chains for cycles.
    /// Disabling this validation may lead to undefined behavior, including <see cref="StackOverflowException"/>.
    /// It is recommended to keep this enabled and refactor services to eliminate circular dependencies.
    /// </remarks>
    public bool ThrowOnCircularDependency { get; init; } = true;

    #endregion

    #region Scope Methods

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IServiceContainer CreateScope()
    {
        return new ServiceProvider(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ServiceKey CreateServiceKey(Type serviceType, string? name)
    {
        return new ServiceKey(serviceType, !string.IsNullOrEmpty(name) ? name : null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Scope EnterScope(ServiceKey key)
    {
        var stack = _dependencyValidationStack.Value!;
        if (ThrowOnCircularDependency)
        {
            if (stack.Contains(key))
            {
                ThrowCircularDependencyException(stack, key);
            }
        }
        stack.Push(key);
        return new Scope(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ExitScope()
    {
        var stack = _dependencyValidationStack.Value!;
        Debug.Assert(stack.Count >= 1);
        stack.Pop();
    }

    /// <summary>
    /// Checks if the object has been disposed and throws an <see cref="ObjectDisposedException"/> if it has.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the object is already disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CheckDisposed()
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
#else
        if (_disposed == 1) throw new ObjectDisposedException(GetType().FullName);
#endif
    }

    #endregion

    #region Service instance creation

    /// <summary>
    /// Creates a service instance via the public parameterless constructor of <paramref name="serviceType"/>.
    /// Runs within the resolution scope and invokes <see cref="OnServiceActivated(object)"/> on success.
    /// </summary>
    /// <param name="key">The service key (type and optional name) for the current resolution.</param>
    /// <param name="serviceType">The implementation type to instantiate.</param>
    /// <returns>The created service instance.</returns>
    /// <exception cref="InvalidOperationException">Circular dependency detected.</exception>
    /// <exception cref="MissingMethodException">No public parameterless constructor found.</exception>
    /// <exception cref="System.Reflection.TargetInvocationException">Constructor threw an exception.</exception>
    /// <exception cref="MemberAccessException">Constructor cannot be invoked due to access restrictions.</exception>

    private object CreateServiceViaConstructor(ServiceKey key, Type serviceType)
    {
        using var scope = EnterScope(key);
        var service = Activator.CreateInstance(serviceType)!;
        OnServiceActivated(key, service);
        return service;
    }

    /// <summary>
    /// Creates a service via <paramref name="callback"/>, validates it against <paramref name="expectedType"/>,
    /// runs within the resolution scope, and invokes <see cref="OnServiceActivated(object)"/> on success.
    /// If a non-null instance is produced but activation fails (e.g., type validation), 
    /// <see cref="OnServiceActivationFailed(object)"/> is invoked.
    /// </summary>
    /// <param name="key">The service key (type and optional name) for the current resolution.</param>
    /// <param name="callback">The factory that produces the service instance.</param>
    /// <param name="expectedType">The expected service or implementation type.</param>
    /// <returns>The created service instance.</returns>
    /// <exception cref="InvalidOperationException">Factory returned <see langword="null"/> or a circular dependency was detected.</exception>
    /// <exception cref="ArgumentException">Produced instance is not assignable to <paramref name="expectedType"/>.</exception>
    /// <exception cref="Exception">
    /// Any exception thrown by the factory is propagated; 
    /// <see cref="OnServiceActivationFailed(object)"/> is not raised in that case.
    /// </exception>

    private object CreateServiceViaFactory(ServiceKey key, Func<object> callback, Type expectedType)
    {
        using var scope = EnterScope(key);
        var service = callback();
        ThrowIfFactoryReturnedNull(service, expectedType);
        try
        {
            ValidateTypeCompatibility(expectedType, service.GetType(), nameof(callback));
            OnServiceActivated(key, service);
        }
        catch
        {
            OnServiceActivationFailed(key, service);
            throw;
        }
        return service;
    }

    #endregion

    #region Resolving

    /// <inheritdoc />
    public object? GetService(Type serviceType)
        => GetService(serviceType: serviceType, name: null, localOnly: false);

    /// <inheritdoc />
    public object? GetService(Type serviceType, string? name)
        => GetService(serviceType: serviceType, name: name, localOnly: false);

    /// <inheritdoc />
    public object? GetService(Type serviceType, string? name, bool localOnly)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
#endif
        CheckDisposed();

        if (_parentReentrancyGuard.Value)
        {
            return null;
        }

        /* priorities:
        * 1. exact match (type+name)
        * 2. if named (name != null):
        *      2.1 local search name match & type assignment
        *      2.2 parent search (with name provided)
        *      2.3 exit
        * 3. if unnamed (name == null):
        *      3.1 local search by null name & type assignment
        *      3.2 local search by non-null name & type assignment
        *      3.3 parent search (no name provided)
        *      3.4 exit
        */

        var key = CreateServiceKey(serviceType, name);

        //1. exact match (type+name)
        if (_registrations.TryGetValue(key, out var registration))
        {
            return registration.GetService();//<--- service creation point or NEW instance each time
        }

        //2. if named (name != null):
        if (key.Name != null)
        {
            #region 2.1 local search name match & type assignment
            // registration named assignable
            foreach (var pair in _registrations)
            {
                if (key.Name != pair.Key.Name) continue;
                registration = pair.Value;
                if (registration is { IsSingleton: true, Value.IsCreated: true })
                {
                    var service = registration.Value.Service;
                    if (serviceType.IsInstanceOfType(service))
                    {
                        return service;
                    }
                    continue;
                }
                if (serviceType.IsAssignableFrom(pair.Key.Type))
                {
                    return registration.GetService();//<--- service creation point or transient creation point
                }
            }
            #endregion
        }
        else //3. if unnamed (name == null):
        {
            #region 3.1 local search by null name & type assignment
            // registration unnamed assignable
            foreach (var pair in _registrations)
            {
                if (pair.Key.Name != null) continue;
                registration = pair.Value;
                if (registration is { IsSingleton: true, Value.IsCreated: true })
                {
                    var service = registration.Value.Service;
                    if (serviceType.IsInstanceOfType(service))
                    {
                        return service;
                    }
                    continue;
                }
                if (serviceType.IsAssignableFrom(pair.Key.Type))
                {
                    return registration.GetService();//<--- service creation point or transient creation point
                }
            }
            #endregion

            #region 3.2 local search by non-null name & type assignment
            // registration named assignable (fallback within unnamed request)
            foreach (var pair in _registrations)
            {
                if (pair.Key.Name == null) continue;
                registration = pair.Value;
                if (registration is { IsSingleton: true, Value.IsCreated: true })
                {
                    var service = registration.Value.Service;
                    if (serviceType.IsInstanceOfType(service))
                    {
                        return service;
                    }
                    continue;
                }
                if (serviceType.IsAssignableFrom(pair.Key.Type))
                {
                    return registration.GetService();//<--- service creation point or transient creation point
                }
            }
            #endregion
        }

        //2.2 parent search (with name provided)
        //3.3 parent search (no name provided)

        if (localOnly)
        {
            return null;
        }

        if (_upstreamResolve is not null)
        {
            _parentReentrancyGuard.Value = true;
            try
            {
                // Delegate-based upstream resolution
                return _upstreamResolve(serviceType, key.Name);
            }
            finally
            {
                _parentReentrancyGuard.Value = false;
            }
        }

        if (_parentProvider is null)
        {
            return null;
        }

        _parentReentrancyGuard.Value = true;
        try
        {
            return _parentProvider switch
            {
                IServiceContainer serviceContainer => serviceContainer.GetService(serviceType, key.Name, localOnly: false),//2.2+3.3
                INamedServiceProvider namedServiceProvider => namedServiceProvider.GetService(serviceType, key.Name),//2.2+3.3
                not null when key.Name == null => _parentProvider.GetService(serviceType),//3.3
                _ => null
            };
        }
        finally
        {
            _parentReentrancyGuard.Value = false;
        }
    }

    /// <inheritdoc />
    IEnumerable<object> IServicesProvider.GetServices(Type serviceType)
        => GetServices(serviceType: serviceType);

    /// <inheritdoc />
    public IEnumerable<object> GetServices(Type serviceType)
        => GetServices(serviceType, localOnly: false, includeTransient: true);

    /// <inheritdoc />
    public IEnumerable<object> GetServices(Type serviceType, bool localOnly)
        => GetServices(serviceType, localOnly: localOnly, includeTransient: true);

    /// <inheritdoc />
    public IEnumerable<object> GetServices(Type serviceType, bool localOnly, bool includeTransient)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
#endif
        CheckDisposed();

        if (_parentReentrancyGuard.Value)
        {
            yield break;
        }

        HashSet<object>? services = null;

        foreach (var pair in _registrations)
        {
            var registration = pair.Value;
            if (registration is { IsSingleton: true, Value.IsCreated: true })
            {
                var service = registration.Value.Service;
                if (serviceType.IsInstanceOfType(service) && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(service))
                {
                    yield return service;
                }
                continue;
            }

            if (registration.IsTransient && !includeTransient) continue;

            if (serviceType.IsAssignableFrom(pair.Key.Type))
            {
                var instance = registration.GetService();//<--- service creation point or transient creation point
                if ((services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(instance))
                {
                    yield return instance;
                }
            }
        }

        if (localOnly)
        {
            yield break;
        }

        if (_upstreamEnumerate is not null)
        {
            _parentReentrancyGuard.Value = true;
            try
            {
                // Delegate-based upstream enumeration
                foreach (var service in _upstreamEnumerate(serviceType))
                {
                    Debug.Assert(service != null, $"Service is null for type {serviceType}");
                    if (service != null && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(service))
                    {
                        yield return service;
                    }
                }
            }
            finally
            {
                _parentReentrancyGuard.Value = false;
            }
            yield break;
        }

        if (_parentProvider is null)
        {
            yield break;
        }

        _parentReentrancyGuard.Value = true;
        try
        {
            switch (_parentProvider)
            {
                case IServiceContainer serviceContainer:
                    foreach (var service in serviceContainer.GetServices(serviceType, localOnly: false, includeTransient))
                    {
                        Debug.Assert(service != null, $"Service is null for type {serviceType}");
                        if (service != null && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(service))
                        {
                            yield return service;
                        }
                    }
                    break;
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
                default:
                    var typedService = _parentProvider.GetService(serviceType);
                    if (typedService != null && (services ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(typedService))
                    {
                        yield return typedService;
                    }
                    break;
            }
        }
        finally
        {
            _parentReentrancyGuard.Value = false;
        }
    }

    #endregion

    #region Instance registration

    /// <inheritdoc />
    public void RegisterService(Type serviceType, object service, bool throwIfExists = false)
    {
        RegisterService(serviceType: serviceType, service: service, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterService(Type serviceType, object service, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(service);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        _ = service ?? throw new ArgumentNullException(nameof(service));
#endif
        CheckDisposed();

        ValidateTypeCompatibility(serviceType, service.GetType(), nameof(service));

        var key = CreateServiceKey(serviceType, name);
        RegisterServiceCore(key, new ServiceValue(this, service: service), throwIfExists);
    }

    #endregion

    #region Factory registration

    /// <inheritdoc />
    public void RegisterService(Type serviceType, Func<object> callback, bool throwIfExists = false)
    {
        RegisterService(serviceType: serviceType, callback: callback, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterService(Type serviceType, Func<object> callback, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(callback);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
        CheckDisposed();

        ThrowOpenGenericTypesNotSupported(serviceType, nameof(serviceType));
        var key = CreateServiceKey(serviceType, name);
        RegisterServiceCore(key, new ServiceValue(this, serviceFactory: () => CreateServiceViaFactory(key, callback: callback, expectedType: serviceType)), throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterService(Type serviceType, Type implementationType, Func<object> callback, bool throwIfExists = false)
    {
        RegisterService(serviceType: serviceType, implementationType: implementationType, callback: callback, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterService(Type serviceType, Type implementationType, Func<object> callback, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(callback);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        _ = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
        CheckDisposed();

        ValidateTypeCompatibility(serviceType, implementationType, nameof(implementationType));

        var key = CreateServiceKey(serviceType, name);
        RegisterServiceCore(key, new ServiceValue(this, serviceFactory: () => CreateServiceViaFactory(key, callback: callback, expectedType: implementationType)), throwIfExists);
    }

    #endregion

    #region Type registration

    /// <inheritdoc />
    public void RegisterService(Type serviceType, bool throwIfExists = false)
    {
        RegisterService(serviceType: serviceType, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterService(Type serviceType, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
#endif
        CheckDisposed();

        ThrowOpenGenericTypesNotSupported(serviceType, nameof(serviceType));
        var key = CreateServiceKey(serviceType, name);
        RegisterServiceCore(key, new ServiceValue(this, serviceFactory: () => CreateServiceViaConstructor(key, serviceType: serviceType)), throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterService(Type serviceType, Type implementationType, bool throwIfExists = false)
    {
        RegisterService(serviceType: serviceType, implementationType: implementationType, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterService(Type serviceType, Type implementationType, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        _ = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
#endif
        CheckDisposed();

        ValidateTypeCompatibility(serviceType, implementationType, nameof(implementationType));

        var key = CreateServiceKey(serviceType, name);
        RegisterServiceCore(key, new ServiceValue(this, serviceFactory: () => CreateServiceViaConstructor(key, serviceType: implementationType)), throwIfExists);
    }

    #endregion

    #region Scope

    private void RegisterServiceCore(ServiceKey key, ServiceValue value, bool throwIfExists)
    {
        var registration = ServiceRegistration.Singleton(value);
        if (throwIfExists)
        {
            if (!_registrations.TryAdd(key, registration))
            {
                ThrowServiceExistsException(key.Type);
            }

            if (value.IsCreated)
            {
                OnServiceAdded(key, value.Service);
            }

            return;
        }

        if (_registrations.TryUpdateOrAdd(key, registration, out var oldRegistration) && oldRegistration is { IsSingleton: true, Value.IsCreated: true })
        {
            OnServiceRemoved(key, oldRegistration.Value.Service);
        }

        if (value.IsCreated)
        {
            OnServiceAdded(key, value.Service);
        }
    }

    private void OnServiceAdded(ServiceKey key, object service)
    {
        Debug.WriteLine("OnServiceAdded: {0} {1}", key, service);
        OnServiceAdded(service);
    }

    private void OnServiceActivated(ServiceKey key, object service)
    {
        Debug.WriteLine("OnServiceActivated: {0} {1}", key, service);
        OnServiceActivated(service);
    }

    private void OnServiceActivationFailed(ServiceKey key, object service)
    {
        Debug.WriteLine("OnServiceActivationFailed: {0} {1}", key, service);
        OnServiceActivationFailed(service);
    }

    private void OnServiceRemoved(ServiceKey key, object service)
    {
        Debug.WriteLine("OnServiceRemoved: {0} {1}", key, service);
        OnServiceRemoved(service);
    }

    /// <summary>Called when a created service instance is added to the container.</summary>
    /// <param name="service">The registered service instance.</param>
    /// <remarks>
    /// Invoked for instance registrations (or when a previously materialized instance is added).
    /// Lazy registrations (factories/types) do not trigger this callback at registration time.
    /// </remarks>
    protected virtual void OnServiceAdded(object service)
    {
        Debug.Assert(service != null);
    }

    /// <summary>Called when a service instance has been successfully created and activated.</summary>
    /// <param name="service">The activated service instance.</param>
    /// <remarks>
    /// This method is invoked after a service has been instantiated (via constructor or factory)
    /// and all its dependencies have been resolved. It is called exactly once per service instance
    /// at the point of its first creation.
    /// </remarks>
    protected virtual void OnServiceActivated(object service)
    {
        Debug.Assert(service != null);
    }

    /// <summary>
    /// Called when service activation fails after the service instance has been created.
    /// </summary>
    /// <param name="service">The service instance that was created but failed activation (e.g., due to type mismatch).</param>
    /// <remarks>
    /// This method is called in a catch block after the service instance has been created but an exception occurred
    /// during activation (e.g., type validation failed). Use this method to clean up the service instance, such as
    /// calling <see cref="IDisposable.Dispose"/> if the service implements <see cref="IDisposable"/>.
    /// </remarks>
    protected virtual void OnServiceActivationFailed(object service)
    {
        Debug.Assert(service != null);
    }

    /// <summary>Called when a service is removed from the container.</summary>
    /// <param name="service">The service instance being removed.</param>
    /// <remarks>
    /// This method is invoked when a service is explicitly unregistered or replaced.
    /// Override this method to perform resource cleanup (e.g., calling <see cref="IDisposable.Dispose"/>).
    /// The base implementation does not dispose services.
    /// </remarks>
    protected virtual void OnServiceRemoved(object service)
    {
        Debug.Assert(service != null);
    }

    /// <inheritdoc />
    public bool UnregisterService(object service)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(service);
#else
        _ = service ?? throw new ArgumentNullException(nameof(service));
#endif
        CheckDisposed();

        bool result = false;
        foreach (var pair in _registrations)
        {
            var registration = pair.Value;
            if (!registration.IsSingleton || !registration.Value.IsCreated) continue;
            if (registration.Value.Service != service) continue;
            result |= UnregisterService(pair.Key);
        }
        return result;
    }

    /// <inheritdoc />
    public bool UnregisterService(Type serviceType)
    {
        return UnregisterService(serviceType: serviceType, name: null);
    }

    /// <inheritdoc />
    public bool UnregisterService(Type serviceType, string? name)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
#endif
        CheckDisposed();

        var key = CreateServiceKey(serviceType, name);
        return UnregisterService(key);
    }

    private bool UnregisterService(ServiceKey key)
    {
        var removed = _registrations.TryRemove(key, out var registration);

        if (removed && registration is { IsSingleton: true, Value.IsCreated: true })
        {
            OnServiceRemoved(key, registration.Value.Service);
        }

        return removed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long NextOrder() => Interlocked.Increment(ref _orderSeq);

    /// <inheritdoc />
    public void Clear()
    {
        var entries = _registrations.ToArray();
        _registrations.Clear();

        foreach (var pair in entries)
        {
            var registration = pair.Value;
            if (registration is { IsSingleton: true, Value.IsCreated: true })
            {
                OnServiceRemoved(pair.Key, registration.Value.Service);
            }
        }
    }

    /// <inheritdoc/>
    public async Task CleanupAsync(Func<object, Task> cleanupOperation, bool continueOnCapturedContext)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(cleanupOperation);
#else
        _ = cleanupOperation ?? throw new ArgumentNullException(nameof(cleanupOperation));
#endif
        CheckDisposed();

        var entries = new List<ServiceValue>(_registrations.Count);
        foreach (var pair in _registrations)
        {
            var registration = pair.Value;
            if (registration is { IsSingleton: true, Value.IsCreated: true })
            {
                entries.Add(registration.Value);
            }
        }
        _registrations.Clear();

        entries.Sort(static (a, b) => b.Order.CompareTo(a.Order));

#if NET472_OR_GREATER || NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        var seen = new HashSet<object>(entries.Count, ReferenceEqualityComparer.Instance);
#else
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
#endif

        List<Exception>? exceptions = null;
        foreach (var value in entries)
        {
            Debug.Assert(value.IsCreated);
            Debug.Assert(value.Order != 0, "Created service must have a non-zero Order.");
            var instance = value.Service;
            if (!seen.Add(instance))
            {
                continue;
            }
            try
            {
                await cleanupOperation(instance).ConfigureAwait(continueOnCapturedContext);
            }
            catch (Exception ex)
            {
                (exceptions ??= new List<Exception>(4)).Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(exceptions);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Clear();
        Debug.Assert(!_dependencyValidationStack.IsValueCreated || _dependencyValidationStack.Value!.Count == 0);
        _parentReentrancyGuard.Dispose();
        _dependencyValidationStack.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Helpers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateTypeCompatibility(Type serviceType, Type implementationType, string paramName)
    {
        if (serviceType.IsGenericTypeDefinition || implementationType.IsGenericTypeDefinition)
        {
            throw new ArgumentException($"Open generic types are not supported. Register closed generic types instead.", paramName);
        }

        if (!serviceType.IsAssignableFrom(implementationType))
        {
            throw new ArgumentException($"Type {implementationType} must be assignable to {serviceType}",
                paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowOpenGenericTypesNotSupported(Type serviceType, string? paramName)
    {
        if (serviceType.IsGenericTypeDefinition)
        {
            throw new ArgumentException("Open generic types are not supported. Register closed generic types instead.", paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfFactoryReturnedNull(object? service, Type expectedType)
    {
        if (service is null)
        {
            throw new InvalidOperationException($"Factory callback returned null for type {expectedType}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowServiceExistsException(Type serviceType)
    {
        throw new InvalidOperationException($"Service of type {serviceType} is already registered.");
    }

    private static void ThrowCircularDependencyException(Stack<ServiceKey> stack, ServiceKey current)
    {
        var sb = new System.Text.StringBuilder(255);
        sb.AppendLine("Circular dependency detected: ");
        var arr = stack.ToArray();
        for (int i = arr.Length - 1; i >= 0; i--)
        {
            FormatKey(sb, arr[i]).Append(" -> ");
        }
        FormatKey(sb, current);
        throw new InvalidOperationException(sb.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static System.Text.StringBuilder FormatKey(System.Text.StringBuilder sb, ServiceKey key)
            => key.Name is null ? sb.Append($"({key.Type.FullName})")
                : sb.Append($"({key.Type.FullName}, \"{key.Name}\")");
    }

    #endregion

    #region Transient

    /// <inheritdoc />
    public void RegisterTransient(Type serviceType, Func<object> callback, bool throwIfExists = false)
    {
        RegisterTransient(serviceType: serviceType, callback: callback, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterTransient(Type serviceType, Func<object> callback, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(callback);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
        CheckDisposed();

        ThrowOpenGenericTypesNotSupported(serviceType, nameof(serviceType));
        var key = CreateServiceKey(serviceType, name);
        RegisterTransientCore(key, serviceFactory: () => CreateServiceViaFactory(key, callback: callback, expectedType: serviceType), throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterTransient(Type serviceType, Type implementationType, Func<object> callback, bool throwIfExists = false)
    {
        RegisterTransient(serviceType: serviceType, implementationType: implementationType, callback: callback, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterTransient(Type serviceType, Type implementationType, Func<object> callback, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(callback);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        _ = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        _ = callback ?? throw new ArgumentNullException(nameof(callback));
#endif
        CheckDisposed();

        ValidateTypeCompatibility(serviceType, implementationType, nameof(implementationType));

        var key = CreateServiceKey(serviceType, name);
        RegisterTransientCore(key, serviceFactory: () => CreateServiceViaFactory(key, callback: callback, expectedType: implementationType), throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterTransient(Type serviceType, bool throwIfExists = false)
    {
        RegisterTransient(serviceType: serviceType, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterTransient(Type serviceType, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
#endif
        CheckDisposed();

        ThrowOpenGenericTypesNotSupported(serviceType, nameof(serviceType));
        var key = CreateServiceKey(serviceType, name);
        RegisterTransientCore(key, serviceFactory: () => CreateServiceViaConstructor(key, serviceType: serviceType), throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterTransient(Type serviceType, Type implementationType, bool throwIfExists = false)
    {
        RegisterTransient(serviceType: serviceType, implementationType: implementationType, name: null, throwIfExists);
    }

    /// <inheritdoc />
    public void RegisterTransient(Type serviceType, Type implementationType, string? name, bool throwIfExists = false)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
#else
        _ = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        _ = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
#endif
        CheckDisposed();

        ValidateTypeCompatibility(serviceType, implementationType, nameof(implementationType));

        var key = CreateServiceKey(serviceType, name);
        RegisterTransientCore(key, serviceFactory: () => CreateServiceViaConstructor(key, serviceType: implementationType), throwIfExists);
    }

    private void RegisterTransientCore(ServiceKey key, Func<object> serviceFactory, bool throwIfExists)
    {
        var registration = ServiceRegistration.Transient(serviceFactory);
        if (throwIfExists)
        {
            if (!_registrations.TryAdd(key, registration))
            {
                ThrowServiceExistsException(key.Type);
            }
        }
        else
        {
            if (_registrations.TryUpdateOrAdd(key, registration, out var oldRegistration) && oldRegistration is { IsSingleton: true, Value.IsCreated: true })
            {
                OnServiceRemoved(key, oldRegistration.Value.Service);
            }
        }
    }

    #endregion
}