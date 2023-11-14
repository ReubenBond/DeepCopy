using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
// ReSharper disable StaticMemberInGenericType

namespace DeepCopy
{
    /// <summary>
    /// Generates copy delegates.
    /// </summary>
    internal static class CopierGenerator<T>
    {
        private static readonly ConcurrentDictionary<Type, DeepCopyDelegate<T>> Copiers = new ConcurrentDictionary<Type, DeepCopyDelegate<T>>();
        private static readonly Type GenericType = typeof(T);
        private static readonly DeepCopyDelegate<T> MatchingTypeCopier = CreateCopier(GenericType);
        private static readonly Func<Type, DeepCopyDelegate<T>> GenerateCopier = CreateCopier;
        
        public static T Copy(T original, CopyContext context)
        {
            // ReSharper disable once ExpressionIsAlwaysNull
            if (original == null) return original;

            var type = original.GetType();

            if (type.FullName?.Equals("System.RuntimeType", StringComparison.InvariantCulture) ?? false) return original;
            
            if (type == GenericType) return MatchingTypeCopier(original, context);

            var result = Copiers.GetOrAdd(type, GenerateCopier);
            return result(original, context);
        }

        /// <summary>
        /// Gets a copier for the provided type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A copier for the provided type.</returns>
        private static DeepCopyDelegate<T> CreateCopier(Type type)
        {
            if (type.IsArray)
            {
                return CreateArrayCopier(type);
            }

            if (DeepCopier.CopyPolicy.IsImmutable(type)) return (original, context) => original;

            // By-ref types are not supported.
            if (type.IsByRef) return ThrowNotSupportedType(type);

            var dynamicMethod = new DynamicMethod(
                type.Name + "DeepCopier",
                typeof(T),
                new[] {typeof(T), typeof(CopyContext)},
                typeof(DeepCopier).Module,
                true);

            var il = dynamicMethod.GetILGenerator();

            // Declare a variable to store the result.
            il.DeclareLocal(type);

            var needsTracking = DeepCopier.CopyPolicy.NeedsTracking(type);
            var hasCopyLabel = il.DefineLabel();
            if (needsTracking)
            {
                // C#: if (context.TryGetCopy(original, out object existingCopy)) return (T)existingCopy;
                il.DeclareLocal(typeof(object));
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloca_S, (byte) 1);
                il.Emit(OpCodes.Call, DeepCopier.MethodInfos.TryGetCopy);
                il.Emit(OpCodes.Brtrue, hasCopyLabel);
            }

            // Construct the result.
            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            if (type.IsValueType)
            {
                // Value types can be initialized directly.
                il.Emit(OpCodes.Ldloca_S, (byte)0);
                il.Emit(OpCodes.Initobj, type);
            }
            else if (constructorInfo != null)
            {
                // If a default constructor exists, use that.
                il.Emit(OpCodes.Newobj, constructorInfo);
                il.Emit(OpCodes.Stloc_0);
            }
            else
            {
                // If no default constructor exists, create an instance using GetUninitializedObject
                il.Emit(OpCodes.Ldtoken, type);
                il.Emit(OpCodes.Call, DeepCopier.MethodInfos.GetTypeFromHandle);
                il.Emit(OpCodes.Call, DeepCopier.MethodInfos.GetUninitializedObject);
                il.Emit(OpCodes.Castclass, type);
                il.Emit(OpCodes.Stloc_0);
            }

            // An instance of a value types can never appear multiple times in an object graph,
            // so only record reference types in the context.
            if (needsTracking)
            {
                // Record the object.
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Call, DeepCopier.MethodInfos.RecordObject);
            }

            // Copy each field.
            foreach (var field in DeepCopier.CopyPolicy.GetCopyableFields(type))
            {
                // Load a reference to the result.
                if (type.IsValueType)
                {
                    // Value types need to be loaded by address rather than copied onto the stack.
                    il.Emit(OpCodes.Ldloca_S, (byte)0);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc_0);
                }

                // Load the field from the result.
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);

                // Deep-copy the field if needed, otherwise just leave it as-is.
                if (!DeepCopier.CopyPolicy.IsImmutable(field.FieldType))
                {
                    // Copy the field using the generic DeepCopy.Copy<T> method.
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, DeepCopier.MethodInfos.CopyInner.MakeGenericMethod(field.FieldType));
                }

                // Store the copy of the field on the result.
                il.Emit(OpCodes.Stfld, field);
            }

            // Return the result.
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            if (needsTracking)
            {
                // only non-ValueType needsTracking
                il.MarkLabel(hasCopyLabel);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Castclass, type);
                il.Emit(OpCodes.Ret);
            }

            return dynamicMethod.CreateDelegate(typeof(DeepCopyDelegate<T>)) as DeepCopyDelegate<T>;
        }

        private static DeepCopyDelegate<T> CreateArrayCopier(Type type)
        {
            var elementType = type.GetElementType();

            var rank = type.GetArrayRank();
            var isImmutable = DeepCopier.CopyPolicy.IsImmutable(elementType);
            MethodInfo methodInfo;
            switch (rank)
            {
                case 1:
                    if (isImmutable)
                    {
                        methodInfo = DeepCopier.MethodInfos.CopyArrayRank1Shallow;
                    }
                    else
                    {
                        methodInfo = DeepCopier.MethodInfos.CopyArrayRank1;
                    }
                    break;
                case 2:
                    if (isImmutable)
                    {
                        methodInfo = DeepCopier.MethodInfos.CopyArrayRank2Shallow;
                    }
                    else
                    {
                        methodInfo = DeepCopier.MethodInfos.CopyArrayRank2;
                    }
                    break;
                default:
                    return ArrayCopier.CopyArray;
            }

            return (DeepCopyDelegate<T>) methodInfo.MakeGenericMethod(elementType).CreateDelegate(typeof(DeepCopyDelegate<T>));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DeepCopyDelegate<T> ThrowNotSupportedType(Type type)
        {
            throw new NotSupportedException($"Unable to copy object of type {type}.");
        }
    }
}
