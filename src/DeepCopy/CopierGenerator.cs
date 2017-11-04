using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace DeepCopy
{
    /// <summary>
    /// Generates copy delegates.
    /// </summary>
    internal sealed class CopierGenerator
    {
        private readonly CachedReadConcurrentDictionary<(Type originalType, Type parameterType), Delegate> copiers
            = new CachedReadConcurrentDictionary<(Type originalType, Type parameterType), Delegate>();
        private readonly StaticFieldBuilder fieldBuilder = new StaticFieldBuilder();
        private readonly MethodInfos methodInfos = new MethodInfos();
        private readonly CopyPolicy copyPolicy;
        
        public CopierGenerator(CopyPolicy copyPolicy)
        {
            this.copyPolicy = copyPolicy;
        }

        /// <summary>
        /// Gets a copier for the provided type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A copier for the provided type.</returns>
        public DeepCopyDelegate<T> GetOrCreateCopier<T>(Type type)
        {
            if (this.copyPolicy.IsImmutable(type))
            {
                T ImmutableCopier(T original, CopyContext context) => original;
                return ImmutableCopier;
            }

            var parameterType = typeof(T);
            var key = (type, parameterType);
            if (!this.copiers.TryGetValue(key, out var untypedCopier))
            {
                untypedCopier = this.CreateCopier<T>(type);
                this.copiers.TryAdd(key, untypedCopier);
            }
            
            return (DeepCopyDelegate<T>) untypedCopier;
        }

        /// <summary>
        /// Gets a copier for the provided type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A copier for the provided type.</returns>
        private DeepCopyDelegate<T> CreateCopier<T>(Type type)
        {
            // By-ref types are not supported.
            if (type.IsByRef) return ThrowNotSupportedType<T>(type);

            var dynamicMethod = new DynamicMethod(
                type.Name + "DeepCopier",
                typeof(T),
                new[] {typeof(T), typeof(CopyContext)},
                typeof(CopierGenerator).Module,
                true);

            var il = dynamicMethod.GetILGenerator();

            // Declare a variable to store the result.
            il.DeclareLocal(type);

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
                var field = this.fieldBuilder.GetOrCreateStaticField(type);
                il.Emit(OpCodes.Ldsfld, field);
                il.Emit(OpCodes.Call, this.methodInfos.GetUninitializedObject);
                il.Emit(OpCodes.Castclass, type);
                il.Emit(OpCodes.Stloc_0);
            }

            // An instance of a value types can never appear multiple times in an object graph,
            // so only record reference types in the context.
            if (!type.IsValueType)
            {
                // Record the object.
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Call, this.methodInfos.RecordObject);
            }

            // Copy each field.
            foreach (var field in this.copyPolicy.GetCopyableFields(type))
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
                if (!this.copyPolicy.IsShallowCopyable(field.FieldType))
                {
                    // Copy the field using the generic Copy<T> method.
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, this.methodInfos.CopyInner.MakeGenericMethod(field.FieldType));
                }

                // Store the copy of the field on the result.
                il.Emit(OpCodes.Stfld, field);
            }

            // Return the result.
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
            return dynamicMethod.CreateDelegate(typeof(DeepCopyDelegate<T>)) as DeepCopyDelegate<T>;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DeepCopyDelegate<T> ThrowNotSupportedType<T>(Type type)
        {
            throw new NotSupportedException($"Unable to copy object of type {type}.");
        }
    }
}