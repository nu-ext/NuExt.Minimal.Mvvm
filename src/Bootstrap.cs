using System;
using System.Threading;

namespace Minimal.Mvvm;

/// <summary>
/// Lightweight registration point for project-specific adapters and factories used by Minimal.Mvvm.
/// </summary>
public static class Bootstrap
{
    /// <summary>
    /// Register an optimized CheckAccess factory for the synchronization context type <typeparamref name="TContext"/>.
    /// </summary>
    public static void RegisterCheckAccessDelegate<TContext>(Func<TContext, Func<bool>> factory)
        where TContext : SynchronizationContext
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(factory);
#else
        _ = factory ?? throw new ArgumentNullException(nameof(factory));
#endif
        SynchronizationContextExtensions.RegisterCheckAccessFactory(typeof(TContext), ctx => factory((TContext)ctx));
    }

    /// <summary>
    /// Unregister a previously registered CheckAccess factory for the specified synchronization context type.
    /// Returns true if a registration existed and was removed.
    /// </summary>
    public static bool UnregisterCheckAccessDelegate<TContext>()
        where TContext : SynchronizationContext
    {
        return SynchronizationContextExtensions.UnregisterCheckAccessFactory(typeof(TContext));
    }
}
