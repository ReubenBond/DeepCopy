using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.ObjectPool;

namespace DeepCopy
{
    /// <summary>
    /// Methods for creating deep copies of objects.
    /// </summary>
    public static class DeepCopier
    {
        private static readonly ConcurrentDictionary<(Type originalType, Type parameterType), Delegate> Copiers = new ConcurrentDictionary<(Type originalType, Type parameterType), Delegate>();
        private static readonly CopyPolicy CopyPolicy = new CopyPolicy();
        private static readonly CopierGenerator CopierGenerator = new CopierGenerator(CopyPolicy);
        private static readonly ObjectPool<CopyContext> ContextPool = new DefaultObjectPool<CopyContext>(new ContextPoolPolicy());

        /// <summary>
        /// Creates and returns a deep copy of the provided object.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="original">The object to copy.</param>
        /// <returns>A deep copy of the provided object.</returns>
        public static T Copy<T>(T original)
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
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="original">The object to copy.</param>
        /// <param name="context">
        /// The copy context, providing referential integrity between multiple calls to this method.
        /// </param>
        /// <returns>A deep copy of the provided object.</returns>
        public static T Copy<T>(T original, CopyContext context)
        {
            if (original == null) return default(T);

            // If this object has already been copied, return that copy.
            var existingCopy = context.TryGetCopy(original);
            if (existingCopy != null) return (T)existingCopy;

            var type = original.GetType();
            if (!type.IsValueType)
            {
                // Handle arrays specially.
                var originalArray = original as Array;
                if (originalArray != null) return (T)CopyArray(originalArray, context);
            }

            var parameterType = typeof(T);
            var key = (type, parameterType);
            if (!Copiers.TryGetValue(key, out var untypedCopier))
            {
                untypedCopier = CopierGenerator.CreateCopier<T>(type);
                Copiers.TryAdd(key, untypedCopier);
            }

            if (untypedCopier == null) return ThrowNotSupportedType<T>(type);
            
            var typedCopier = (DeepCopyDelegate<T>) untypedCopier;
            return typedCopier(original, context);
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

        private static T ThrowNotSupportedType<T>(Type type)
        {
            throw new NotSupportedException($"Unable to copy object of type {type}.");
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
