using System;
using System.Threading;

namespace Minimal.Mvvm;

/// <summary>
/// Provides a lightweight registration point for Minimal.Mvvm-specific adapters.
/// </summary>
/// <remarks>
/// Use this class to register optimized access-check delegates for known
/// <see cref="SynchronizationContext"/> implementations used by Minimal.Mvvm.
/// </remarks>
public static class Bootstrap
{
    /// <summary>
    /// Registers an optimized access-check delegate for the synchronization context type
    /// <typeparamref name="TContext"/>.
    /// </summary>
    /// <typeparam name="TContext">
    /// The concrete <see cref="SynchronizationContext"/> type to register.
    /// </typeparam>
    /// <param name="checkAccess">
    /// A delegate that determines whether the calling thread has access to the specified
    /// <typeparamref name="TContext"/> instance.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="checkAccess"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// If a delegate is already registered for <typeparamref name="TContext"/>, it is replaced.
    /// </remarks>
    public static void RegisterCheckAccessDelegate<TContext>(Func<TContext, bool> checkAccess)
        where TContext : SynchronizationContext
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(checkAccess);
#else
        _ = checkAccess ?? throw new ArgumentNullException(nameof(checkAccess));
#endif
        SynchronizationContextExtensions.RegisterCheckAccessDelegate(typeof(TContext), ctx => checkAccess((TContext)ctx));
    }

    /// <summary>
    /// Unregisters a previously registered access-check delegate for the synchronization context type
    /// <typeparamref name="TContext"/>.
    /// </summary>
    /// <typeparam name="TContext">
    /// The concrete <see cref="SynchronizationContext"/> type whose registration should be removed.
    /// </typeparam>
    /// <returns>
    /// <see langword="true"/> if a registration existed and was removed; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool UnregisterCheckAccessDelegate<TContext>()
        where TContext : SynchronizationContext
    {
        return SynchronizationContextExtensions.UnregisterCheckAccessDelegate(typeof(TContext));
    }
}