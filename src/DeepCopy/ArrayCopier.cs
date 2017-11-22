using System;

namespace DeepCopy
{
    internal static class ArrayCopier
    {
        internal static T[] CopyArrayRank1<T>(T[] originalArray, CopyContext context) where T: class 
        {
            if (context.TryGetCopy(originalArray, out var existingCopy)) return (T[]) existingCopy;
            var length = originalArray.Length;
            var result = new T[length];
            context.RecordCopy(originalArray, result);
            for (var i = 0; i < length; i++)
            {
                var original = originalArray[i];
                if (original != null)
                {
                    if (context.TryGetCopy(original, out var existingElement))
                    {
                        result[i] = (T) existingElement;
                    }
                    else
                    {
                        var copy = CopierGenerator<T>.Copy(original, context);
                        context.RecordCopy(original, copy);
                        result[i] = copy;
                    }
                }
            }
            return result;
        }

        internal static T[,] CopyArrayRank2<T>(T[,] originalArray, CopyContext context)
        {
            if (context.TryGetCopy(originalArray, out var existingCopy)) return (T[,])existingCopy;

            var lenI = originalArray.GetLength(0);
            var lenJ = originalArray.GetLength(1);
            var result = new T[lenI, lenJ];
            context.RecordCopy(originalArray, result);
            for (var i = 0; i < lenI; i++)
            for (var j = 0; j < lenJ; j++)
            {
                var original = originalArray[i, j];
                if (original != null)
                {
                    if (context.TryGetCopy(original, out var existingElement))
                    {
                        result[i, j] = (T) existingElement;
                    }
                    else
                    {
                        var copy = CopierGenerator<T>.Copy(original, context);
                        context.RecordCopy(original, copy);
                        result[i, j] = copy;
                    }
                }
            }
            return result;
        }

        internal static T[] CopyArrayRank1Shallow<T>(T[] array, CopyContext context)
        {
            if (context.TryGetCopy(array, out var existingCopy)) return (T[])existingCopy;

            var length = array.Length;
            var result = new T[length];
            context.RecordCopy(array, result);
            Array.Copy(array, result, length);
            return result;
        }

        internal static T[,] CopyArrayRank2Shallow<T>(T[,] array, CopyContext context)
        {
            if (context.TryGetCopy(array, out var existingCopy)) return (T[,])existingCopy;

            var lenI = array.GetLength(0);
            var lenJ = array.GetLength(1);
            var result = new T[lenI, lenJ];
            context.RecordCopy(array, result);
            Array.Copy(array, result, array.Length);
            return result;
        }

        internal static T CopyArray<T>(T array, CopyContext context)
        {
            if (context.TryGetCopy(array, out var existingCopy)) return (T)existingCopy;

            var originalArray = array as Array;
            if (originalArray == null) throw new InvalidCastException($"Cannot cast non-array type {array?.GetType()} to Array.");
            var elementType = array.GetType().GetElementType();
            
            var rank = originalArray.Rank;
            var lengths = new int[rank];
            for (var i = 0; i < rank; i++)
            {
                lengths[i] = originalArray.GetLength(i);
            }

            var copyArray = Array.CreateInstance(elementType, lengths);
            context.RecordCopy(originalArray, copyArray);

            if (DeepCopier.CopyPolicy.IsImmutable(elementType))
            {
                Array.Copy(originalArray, copyArray, originalArray.Length);
            }

            var index = new int[rank];
            var sizes = new int[rank];
            sizes[rank - 1] = 1;
            for (var k = rank - 2; k >= 0; k--)
            {
                sizes[k] = sizes[k + 1] * lengths[k + 1];
            }
            for (var i = 0; i < originalArray.Length; i++)
            {
                var k = i;
                for (var n = 0; n < rank; n++)
                {
                    var offset = k / sizes[n];
                    k = k - offset * sizes[n];
                    index[n] = offset;
                }
                var original = originalArray.GetValue(index);
                if (original != null)
                {
                    if (context.TryGetCopy(original, out var existingElement))
                    {
                        copyArray.SetValue(existingElement, index);
                    }
                    else
                    {
                        var copy = DeepCopier.Copy(originalArray.GetValue(index), context);
                        context.RecordCopy(original, copy);
                        copyArray.SetValue(copy, index);
                    }
                }
            }

            return (T)(object)copyArray;
        }
    }
}