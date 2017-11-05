using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DeepCopy
{
    /// <summary>
    /// A thread-safe dictionary for read-heavy workloads.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    internal sealed class Memoizer<TKey, TValue>
    {
        /// <summary>
        /// The number of cache misses which are tolerated before the cache is regenerated.
        /// </summary>
        private const int CacheMissesBeforeCaching = 10;
        private readonly ConcurrentDictionary<TKey, TValue> dictionary;
        private readonly IEqualityComparer<TKey> comparer;

        /// <summary>
        /// Approximate number of reads which did not hit the cache since it was last invalidated.
        /// This is used as a heuristic that the dictionary is not being modified frequently with respect to the read volume.
        /// </summary>
        private int cacheMissReads;

        /// <summary>
        /// Cached version of <see cref="dictionary"/>.
        /// </summary>
        private Dictionary<TKey, TValue> readCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="Memoizer{TKey,TValue}"/> class.
        /// </summary>
        public Memoizer()
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Memoizer{TKey,TValue}"/> class
        /// that contains elements copied from the specified collection and uses the specified
        /// <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys.
        /// </param>
        public Memoizer(IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer;
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(comparer);
        }
        
        /// <summary>
        /// Attempts to add the specified key and value.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be a null reference (Nothing
        /// in Visual Basic) for reference types.</param>
        /// <returns>true if the key/value pair was added successfully; otherwise, false.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            if (this.dictionary.TryAdd(key, value))
            {
                this.InvalidateCache();
                return true;
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var cache = this.readCache;
            if (cache != null && cache.TryGetValue(key, out value)) return true;
            return this.GetWithoutCache().TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            set
            {
                this.dictionary[key] = value;
                this.InvalidateCache();
            }
        }

        private IDictionary<TKey, TValue> GetWithoutCache()
        {
            // If the dictionary was recently modified or the cache is being recomputed, return the dictionary directly.
            if (Interlocked.Increment(ref this.cacheMissReads) < CacheMissesBeforeCaching) return this.dictionary;

            // Recompute the cache if too many cache misses have occurred.
            this.cacheMissReads = 0;
            var result = this.readCache = new Dictionary<TKey, TValue>(this.dictionary, this.comparer);
            Thread.MemoryBarrier();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateCache()
        {
            this.cacheMissReads = 0;
            this.readCache = null;
            Thread.MemoryBarrier();
        }
    }
}
