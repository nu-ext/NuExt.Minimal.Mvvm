using System.Collections.Concurrent;
using System.Diagnostics;

namespace Minimal.Mvvm
{
    [DebuggerStepThrough]
    internal static class ConcurrentDictionaryExtensions
    {

        /// <summary>
        /// Tries to update an existing entry with <paramref name="newValue"/>; if the key does not exist, adds it.
        /// </summary>
        /// <returns>
        /// <c>true</c> if an existing value was updated (and <paramref name="oldValue"/> contains the previous value);
        /// <c>false</c> if a new entry was added (and <paramref name="oldValue"/> is <c>default</c>).
        /// </returns>
        public static bool TryUpdateOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict,
            TKey key, TValue newValue, out TValue? oldValue)
            where TKey : notnull
        {
            if (dict.TryAdd(key, newValue))
            {
                oldValue = default;
                return false; // false = added
            }

            // Contended path: CAS-loop
            while (true)
            {
                if (dict.TryGetValue(key, out var current))
                {
                    if (dict.TryUpdate(key, newValue, current))
                    {
                        oldValue = current;
                        return true; // true = updated
                    }
                    continue;
                }

                if (dict.TryAdd(key, newValue))
                {
                    oldValue = default;
                    return false; // false = added
                }
            }
        }
    }
}
