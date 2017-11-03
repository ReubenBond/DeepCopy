using System;

namespace DeepCopy
{
    /// <summary>
    /// Generates copy delegates.
    /// </summary>
    internal sealed class CopierGenerator
    {
        private readonly StaticFieldBuilder fieldBuilder = new StaticFieldBuilder();
        private readonly MethodInfos methodInfos = new MethodInfos();
        private readonly DeepCopyDelegate immutableTypeCopier = (obj, context) => obj;
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
        public DeepCopyDelegate CreateCopier(Type type)
        {
            if (this.copyPolicy.IsImmutable(type)) return this.immutableTypeCopier;

            // By-ref types are not supported.
            if (type.IsByRef) return null;

            var il = new DelegateBuilder<DeepCopyDelegate>(
                this.fieldBuilder,
                type.Name + "DeepCopier",
                this.methodInfos.DeepCopierDelegate);

            // Declare local variables.
            var result = il.DeclareLocal(type);
            var typedInput = il.DeclareLocal(type);

            // Set the typed input variable from the method parameter.
            il.LoadArgument(0);
            il.CastOrUnbox(type);
            il.StoreLocal(typedInput);

            // Construct the result.
            il.CreateInstance(type, result, this.methodInfos.GetUninitializedObject);

            // Record the object.
            il.LoadArgument(1); // Load 'context' parameter.
            il.LoadArgument(0); // Load 'original' parameter.
            il.LoadLocal(result); // Load 'result' local.
            il.BoxIfValueType(type);
            il.Call(this.methodInfos.RecordObject);

            // Copy each field.
            foreach (var field in this.copyPolicy.GetCopyableFields(type))
            {
                // Load the field.
                il.LoadLocalAsReference(type, result);
                il.LoadLocal(typedInput);
                il.LoadField(field);

                // Deep-copy the field if needed, otherwise just leave it as-is.
                if (!this.copyPolicy.IsShallowCopyable(field.FieldType))
                {
                    il.BoxIfValueType(field.FieldType);
                    il.LoadArgument(1);
                    il.Call(this.methodInfos.CopyInner);
                    il.CastOrUnbox(field.FieldType);
                }

                // Store the copy of the field on the result.
                il.StoreField(field);
            }

            il.LoadLocal(result);
            il.BoxIfValueType(type);
            il.Return();
            return il.CreateDelegate();
        }
    }
}