using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;

namespace DeepCopy
{
    /// <summary>
    /// Methods for creating deep copies of objects.
    /// </summary>
    public static class DeepCopier
    {
        private static readonly ConcurrentDictionary<Type, DeepCopyDelegate> Copiers = new ConcurrentDictionary<Type, DeepCopyDelegate>();
        private static readonly CopyPolicy CopyPolicy = new CopyPolicy();
        private static readonly CopierGenerator CopierGenerator = new CopierGenerator(CopyPolicy);
        private static readonly Func<Type, DeepCopyDelegate> CreateDelegate = CopierGenerator.CreateCopier;
        private static readonly ObjectPool<CopyContext> ContextPool = new DefaultObjectPool<CopyContext>(new ContextPoolPolicy());

        /// <summary>
        /// Creates and returns a deep copy of the provided object.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="original">The object to copy.</param>
        /// <returns>A deep copy of the provided object.</returns>
        public static T Copy<T>(T original) => (T)Copy((object)original);

        /// <summary>
        /// Creates and returns a deep copy of the provided object.
        /// </summary>
        /// <param name="original">The object to copy.</param>
        /// <returns>A deep copy of the provided object.</returns>
        public static object Copy(object original)
        {
            var context = ContextPool.Get();
            try
            {
                return Copy(original, context);
            }
            finally
            {
                ContextPool.Return(context);
            }
        }

        /// <summary>
        /// Creates and returns a deep copy of the provided object.
        /// </summary>
        /// <param name="original">The object to copy.</param>
        /// <param name="context">
        /// The copy context, providing referential integrity between multiple calls to this method.
        /// </param>
        /// <returns>A deep copy of the provided object.</returns>
        public static object Copy(object original, CopyContext context)
        {
            if (original is null) return null;

            // If this object has already been copied, return that copy.
            var existingCopy = context.TryGetCopy(original);
            if (existingCopy != null) return existingCopy;

            // Handle arrays specially.
            if (original is Array originalArray) return CopyArray(originalArray, context);

            var copier = Copiers.GetOrAdd(original.GetType(), CreateDelegate);
            if (copier == null) return ThrowNotSupportedType(original);

            return copier(original, context);
        }

        /// <summary>
        /// Returns a copy of the provided array.
        /// </summary>
        /// <param name="originalArray">The original array.</param>
        /// <param name="context">The copy context.</param>
        /// <returns>A copy of the original array.</returns>
        private static object CopyArray(Array originalArray, CopyContext context)
        {
            // Special-case for empty rank-1 arrays.
            if (originalArray.Rank == 1 && originalArray.GetLength(0) == 0)
            {
                return originalArray;
            }

            // Special-case for arrays of immutable types.
            var elementType = originalArray.GetType().GetElementType();
            if (CopyPolicy.IsImmutable(elementType))
            {
                return originalArray.Clone();
            }
            
            var rank = originalArray.Rank;
            var lengths = new int[rank];
            for (var i = 0; i < rank; i++)
                lengths[i] = originalArray.GetLength(i);

            var copyArray = Array.CreateInstance(elementType, lengths);
            context.RecordCopy(originalArray, copyArray);

            switch (rank)
            {
                case 1:
                    for (var i = 0; i < lengths[0]; i++)
                        copyArray.SetValue(Copy(originalArray.GetValue(i), context), i);
                    break;
                case 2:
                    for (var i = 0; i < lengths[0]; i++)
                    for (var j = 0; j < lengths[1]; j++)
                        copyArray.SetValue(Copy(originalArray.GetValue(i, j), context), i, j);
                    break;
                default:
                    var index = new int[rank];
                    var sizes = new int[rank];
                    sizes[rank - 1] = 1;
                    for (var k = rank - 2; k >= 0; k--)
                        sizes[k] = sizes[k + 1] * lengths[k + 1];

                    for (var i = 0; i < originalArray.Length; i++)
                    {
                        var k = i;
                        for (var n = 0; n < rank; n++)
                        {
                            var offset = k / sizes[n];
                            k = k - offset * sizes[n];
                            index[n] = offset;
                        }

                        copyArray.SetValue(Copy(originalArray.GetValue(index), context), index);
                    }

                    break;
            }

            return copyArray;
        }

        private static object ThrowNotSupportedType(object original)
        {
            throw new NotSupportedException($"Unable to copy object of type {original.GetType()}.");
        }

        private sealed class ContextPoolPolicy : IPooledObjectPolicy<CopyContext>
        {
            /// <inheritdoc />
            public CopyContext Create() => new CopyContext();

            /// <inheritdoc />
            public bool Return(CopyContext context)
            {
                context.Reset();
                return true;
            }
        }
    }
}
