using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Minimal.Mvvm
{

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
            lock (_handlers)
            {
                for (var i = 0; i < individualHandlers.Length; i++)
                {
                    _handlers.Add(new WeakHandler((TEventHandler)individualHandlers[i]));
                }
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
                if (_handlers.Count == 0) return;
                for (int i = _handlers.Count - 1; i >= 0; i--)
                {
                    var weakHandler = _handlers[i];
                    if (!weakHandler.TryGetTarget(out _))// Dead handler
                    {
                        _handlers.RemoveAt(i);
                        continue;
                    }

                    bool remove = false;
                    for (int k = 0; k < individualHandlers.Length; k++)
                    {
                        if (weakHandler.Matches((TEventHandler)individualHandlers[k])) { remove = true; break; }
                    }
                    if (remove)
                    {
                        _handlers.RemoveAt(i);
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
                System.Buffers.ArrayPool<WeakHandler>.Shared.Return(snapshot, clearArray: true);
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

            return Expression.Lambda<Action<object?, object?, TEventArgs>>(call, pTarget, pSender, pArgs).Compile();
        }
    }
}
