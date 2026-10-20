using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Minimal.Mvvm;


/// <summary>
/// Weak event storage for delegate types compatible with (object, EventArgs).
/// </summary>
public sealed class WeakEvent<TEventHandler, TEventArgs> where TEventHandler : Delegate
{
    private sealed class WeakHandler
    {
        private readonly WeakReference? _target;
        private readonly MethodInfo _method;
        private readonly Action<object?, object?, TEventArgs> _handler;

        public WeakHandler(TEventHandler handler)
        {
            _target = handler.Target is not null ? new WeakReference(handler.Target) : null;
            _method = handler.Method;
            _handler = GetOrCreateInvoker(_method);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(TEventHandler handler)
        {
            return handler.Target == _target?.Target && handler.Method == _method;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTarget(out object? target)
        {
            if (_target is null)// static
            {
                target = null;
                return true;
            }
            target = _target.Target;
            return target is not null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryInvoke(object? sender, TEventArgs args)
        {
            if (!TryGetTarget(out var target))
            {
                return false;
            }
            _handler.Invoke(target, sender, args);
            return true;
        }
    }

    private readonly List<WeakHandler> _handlers = new(4);

    public int Count
    {
        get { lock (_handlers) { return _handlers.Count; } }
    }

    /// <summary>
    /// Adds a handler using weak semantics.
    /// </summary>
    public void AddHandler(TEventHandler? handler)
    {
        if (handler is null)
        {
            return;
        }

        var individualHandlers = handler.GetInvocationList();
        var weakHandlers = new WeakHandler[individualHandlers.Length];

        // Create invokers outside the lock because initial compilation can be expensive
        for (var i = 0; i < individualHandlers.Length; i++)
        {
            weakHandlers[i] = new WeakHandler((TEventHandler)individualHandlers[i]);
        }

        lock (_handlers)
        {
            _handlers.AddRange(weakHandlers);
        }
    }

    /// <summary>
    /// Removes a previously added handler. Also prunes dead handlers.
    /// </summary>
    public void RemoveHandler(TEventHandler? handler)
    {
        if (handler is null)
            return;

        var individualHandlers = handler.GetInvocationList();
        lock (_handlers)
        {
            // Prune dead handlers before matching the invocation list
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (!_handlers[i].TryGetTarget(out _))// Dead handler
                {
                    _handlers.RemoveAt(i);
                }
            }

            if (individualHandlers.Length > _handlers.Count) return;

            // Search backwards to remove the last matching invocation list consistent with Delegate.Remove semantics
            for (int i = _handlers.Count - individualHandlers.Length; i >= 0; i--)
            {
                bool matches = true;
                for (int k = 0; k < individualHandlers.Length; k++)
                {
                    if (!_handlers[i + k].Matches((TEventHandler)individualHandlers[k]))
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                {
                    _handlers.RemoveRange(i, individualHandlers.Length);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Raises the event to all live handlers. Dead handlers are pruned.
    /// </summary>
    public void Raise(object? sender, TEventArgs args)
    {
        WeakHandler[] snapshot;
        int count;
        lock (_handlers)
        {
            count = _handlers.Count;
            if (count == 0) return;

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            snapshot = System.Buffers.ArrayPool<WeakHandler>.Shared.Rent(count);
            _handlers.CopyTo(snapshot, 0);
#else
            snapshot = _handlers.ToArray();
#endif
        }
        bool hasDead = false;
        try
        {
            for (var i = 0; i < count; i++)
            {
                if (!snapshot[i].TryInvoke(sender, args))
                {
                    hasDead = true;
                }
            }
        }
        finally
        {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            // Clear pooled references to avoid retaining MethodInfo and invoker metadata
            Array.Clear(snapshot, 0, count);
            System.Buffers.ArrayPool<WeakHandler>.Shared.Return(snapshot, clearArray: false);
#endif
        }

        if (!hasDead) return;

        lock (_handlers)
        {
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (!_handlers[i].TryGetTarget(out _))
                {
                    _handlers.RemoveAt(i);
                }
            }
        }
    }

    // IMPORTANT:
    // We intentionally use ConditionalWeakTable instead of ConcurrentDictionary. Reason: keys must NOT keep strong roots
    // to MethodInfo/assembly metadata, otherwise long-lived static caches will prevent collectible AssemblyLoadContexts
    // (or dynamic assemblies) from being unloaded.
    private static readonly ConditionalWeakTable<MethodInfo, Action<object?, object?, TEventArgs>> s_invokers = new();

    private static Action<object?, object?, TEventArgs> GetOrCreateInvoker(MethodInfo method)
        => s_invokers.GetValue(method, CompileOpenInvoker);

    private static Action<object?, object?, TEventArgs> CompileOpenInvoker(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (method.ReturnType != typeof(void) || parameters.Length != 2
            || parameters[0].ParameterType != typeof(object)
            || !parameters[1].ParameterType.IsAssignableFrom(typeof(TEventArgs)))
            throw new InvalidOperationException($"Handler method must be (object sender, {typeof(TEventArgs).Name} args).");

        var targetType = method.DeclaringType ?? throw new InvalidOperationException("Method must have declaring type.");

        var pTarget = Expression.Parameter(typeof(object), "target");
        var pSender = Expression.Parameter(typeof(object), "sender");
        var pArgs = Expression.Parameter(typeof(TEventArgs), "args");

        var castTarget = Expression.Convert(pTarget, targetType);
        var castSender = Expression.Convert(pSender, parameters[0].ParameterType);
        var castArgs = Expression.Convert(pArgs, parameters[1].ParameterType);

        var call = method.IsStatic
            ? Expression.Call(method, castSender, castArgs)
            : Expression.Call(castTarget, method, castSender, castArgs);

        var lambda = Expression.Lambda<Action<object?, object?, TEventArgs>>(call, pTarget, pSender, pArgs);

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return lambda.Compile(preferInterpretation: !RuntimeFeature.IsDynamicCodeSupported);
#else
        return lambda.Compile();
#endif
    }
}