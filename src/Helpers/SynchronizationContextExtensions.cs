using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Minimal.Mvvm;

internal static class SynchronizationContextExtensions
{
    private static readonly ConditionalWeakTable<Type, Func<SynchronizationContext, bool>> s_checkAccessDelegates = new();

    internal static bool CheckAccess(this SynchronizationContext sc)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(sc);
#else
        _ = sc ?? throw new ArgumentNullException(nameof(sc));
#endif
        if (ReferenceEquals(SynchronizationContext.Current, sc))
        {
            return true;
        }

        return s_checkAccessDelegates.TryGetValue(sc.GetType(), out var checkAccess) && checkAccess(sc);
    }

    internal static void RegisterCheckAccessDelegate(Type type, Func<SynchronizationContext, bool> checkAccess)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(checkAccess);
#else
        _ = type ?? throw new ArgumentNullException(nameof(type));
        _ = checkAccess ?? throw new ArgumentNullException(nameof(checkAccess));
#endif
        if (!typeof(SynchronizationContext).IsAssignableFrom(type))
        {
            throw new ArgumentException($"The type must derive from {typeof(SynchronizationContext).FullName}.", nameof(type));
        }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        s_checkAccessDelegates.AddOrUpdate(type, checkAccess);
#else
        lock (s_checkAccessDelegates)
        {
            s_checkAccessDelegates.Remove(type);
            s_checkAccessDelegates.Add(type, checkAccess);
        }
#endif
    }

    internal static bool UnregisterCheckAccessDelegate(Type type)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(type);
#else
        _ = type ?? throw new ArgumentNullException(nameof(type));
#endif
        return s_checkAccessDelegates.Remove(type);
    }
}
