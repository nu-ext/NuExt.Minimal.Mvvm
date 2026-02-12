using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Minimal.Mvvm
{
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>, IEqualityComparer
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        private ReferenceEqualityComparer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

}
