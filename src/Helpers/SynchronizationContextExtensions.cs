using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Minimal.Mvvm;

public static class SynchronizationContextExtensions
{
    private static readonly ConditionalWeakTable<Type, Func<SynchronizationContext, Func<bool>>> s_checkAccessFactories = new();

    public static bool CheckAccess(this SynchronizationContext sc)
    {
        if (s_checkAccessFactories.TryGetValue(sc.GetType(), out var factory))
        {
            return factory(sc)();
        }

        return ReferenceEquals(SynchronizationContext.Current, sc);
    }

    internal static void RegisterCheckAccessFactory(Type type, Func<SynchronizationContext, Func<bool>> factory)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        s_checkAccessFactories.AddOrUpdate(type, factory);
#else
        s_checkAccessFactories.Remove(type);
        s_checkAccessFactories.Add(type, factory);
#endif
    }

    internal static bool UnregisterCheckAccessFactory(Type type)
    {
        return s_checkAccessFactories.Remove(type);
    }
}
