using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DeepCopy
{
    /// <summary>
    /// Generates static fields for storing values.
    /// </summary>
    internal sealed class StaticFieldBuilder
    {
        private static readonly AssemblyBuilder AssemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(nameof(StaticFieldBuilder)),
                AssemblyBuilderAccess.RunAndCollect);

        private static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule(
            nameof(StaticFieldBuilder));

        private readonly Memoizer<object, FieldInfo> staticFields =
            new Memoizer<object, FieldInfo>(new ReferenceEqualsComparer());

        /// <summary>
        /// Gets or creates a <see langword="static"/>, <see langword="readonly"/> field which holds the specified
        /// <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The underlying type of the provided value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The field which holds the provided <paramref name="value"/>.</returns>
        public FieldInfo GetOrCreateStaticField<T>(T value)
        {
            if (this.staticFields.TryGetValue(value, out var result)) return result;

            result = CreateField(value, typeof(T));
            this.staticFields.TryAdd(value, result);

            return result;
        }

        /// <summary>
        /// Gets or creates a <see langword="static"/>, <see langword="readonly"/> field which holds the specified
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="fieldType">The type of the resulting field.</param>
        /// <returns>The field which holds the provided <paramref name="value"/>.</returns>
        public FieldInfo GetOrCreateStaticField(object value, Type fieldType)
        {
            if (!this.staticFields.TryGetValue(value, out var result))
            {
                result = CreateField(value, fieldType);
                this.staticFields.TryAdd(value, result);
            }

            return result;
        }

        /// <summary>
        /// Creates a static field in a new class and initializes it with the provided <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to initialize the field with.</param>
        /// <param name="fieldType">The type of the resulting field.</param>
        /// <returns>The newly created static field.</returns>
        private static FieldInfo CreateField(object value, Type fieldType)
        {
            // Create a new type to hold the field.
            var typeBuilder = ModuleBuilder.DefineType(
                fieldType.Name + Guid.NewGuid().ToString("N"),
                TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed);

            // Create a static field to hold the value.
            const string fieldName = "Instance";
            var field = typeBuilder.DefineField(
                fieldName,
                fieldType,
                FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.Public);

            // Create a method to initialize the field.
            const string methodName = "Initialize";
            var initMethod = typeBuilder.DefineMethod(
                methodName,
                MethodAttributes.Static | MethodAttributes.Private,
                CallingConventions.Standard,
                typeof(void),
                new[] { fieldType });
            var il = initMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);

            // Build the type.
            var declaringType = typeBuilder.CreateTypeInfo();

            // Invoke the initializer method using reflection, passing the provided value to initialize the new field.
            declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
                         .Invoke(null, new[] { value });
            return declaringType.GetField(fieldName);
        }
    }
}