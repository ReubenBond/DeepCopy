using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DeepCopy
{
    /// <summary>
    /// Wrapper around <see cref="System.Reflection.Emit.ILGenerator"/> to ease the pain of generating delegates.
    /// </summary>
    /// <typeparam name="TDelegate">The delegate type.</typeparam>
    internal sealed class DelegateBuilder<TDelegate> where TDelegate : class
    {
        private readonly DynamicMethod dynamicMethod;
        private readonly StaticFieldBuilder fields;

        /// <summary>Creates a new instance of the <see cref="DelegateBuilder{TDelegate}"/> class.</summary>
        /// <param name="fields">The field builder.</param>
        /// <param name="name">The name of the new delegate.</param>
        /// <param name="methodInfo">
        /// The method info for <typeparamref name="TDelegate"/> delegates, used for determining parameter types.
        /// </param>
        public DelegateBuilder(StaticFieldBuilder fields, string name, MethodInfo methodInfo)
        {
            this.fields = fields;
            var returnType = methodInfo.ReturnType;
            var parameterTypes = GetParameterTypes(methodInfo);
            this.dynamicMethod = new DynamicMethod(
                name,
                returnType,
                parameterTypes,
                typeof(DelegateBuilder<>).Module,
                true);
            this.IL = this.dynamicMethod.GetILGenerator();
        }

        public ILGenerator IL { get; }

        /// <summary>
        /// Declares a local variable with the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The newly declared local.</returns>
        public LocalBuilder DeclareLocal(Type type) => this.IL.DeclareLocal(type);

        /// <summary>
        /// Loads the argument at the given index onto the stack.
        /// </summary>
        /// <param name="index">
        /// The index of the argument to load.
        /// </param>
        public void LoadArgument(ushort index)
        {
            switch (index)
            {
                case 0:
                    this.IL.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    this.IL.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    this.IL.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    this.IL.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (index < 0xFF)
                    {
                        this.IL.Emit(OpCodes.Ldarg_S, (byte)index);
                    }
                    else
                    {
                        this.IL.Emit(OpCodes.Ldarg, index);
                    }

                    break;
            }
        }
        
        /// <summary>
        /// Pops the stack and stores it in the specified local.
        /// </summary>
        /// <param name="local">The local variable to store into.</param>
        public void StoreLocal(LocalBuilder local)
        {
            var index = local.LocalIndex;
            switch (index)
            {
                case 0:
                    this.IL.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    this.IL.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    this.IL.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    this.IL.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    if (index < 0xFF)
                    {
                        this.IL.Emit(OpCodes.Stloc_S, (byte)index);
                    }
                    else
                    {
                        this.IL.Emit(OpCodes.Stloc, local);
                    }

                    break;
            }
        }

        /// <summary>
        /// Pushes the specified local onto the stack.
        /// </summary>
        /// <param name="local">The local variable to load from.</param>
        public void LoadLocal(LocalBuilder local)
        {
            var index = local.LocalIndex;
            switch (index)
            {
                case 0:
                    this.IL.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    this.IL.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    this.IL.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    this.IL.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    if (index < 0xFF)
                    {
                        this.IL.Emit(OpCodes.Ldloc_S, (byte)index);
                    }
                    else
                    {
                        this.IL.Emit(OpCodes.Ldloc, local);
                    }

                    break;
            }
        }

        /// <summary>
        /// Loads the specified field onto the stack from the referenced popped from the stack.
        /// </summary>
        /// <param name="field">The field.</param>
        public void LoadField(FieldInfo field)
        {
            if (field.IsStatic)
            {
                this.IL.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                this.IL.Emit(OpCodes.Ldfld, field);
            }
        }

        /// <summary>
        /// Boxes the value on the top of the stack.
        /// </summary>
        /// <param name="type">The value type.</param>
        public void Box(Type type) => this.IL.Emit(OpCodes.Box, type);

        /// <summary>
        /// Loads the specified type and pushes it onto the stack.
        /// </summary>
        /// <param name="type">The type to load.</param>
        public void LoadType(Type type)
        {
            var field = this.fields.GetOrCreateStaticField(type);
            this.IL.Emit(OpCodes.Ldsfld, field);
        }

        /// <summary>
        /// Calls the specified method.
        /// </summary>
        /// <param name="method">The method to call.</param>
        public void Call(MethodInfo method)
        {
            if (method.IsFinal || !method.IsVirtual) this.IL.Emit(OpCodes.Call, method);
            else this.IL.Emit(OpCodes.Callvirt, method);
        }

        /// <summary>
        /// Returns from the current method.
        /// </summary>
        public void Return() => this.IL.Emit(OpCodes.Ret);

        /// <summary>
        /// Pops the value on the top of the stack and stores it in the specified field on the object popped from the top of the stack.
        /// </summary>
        /// <param name="field">The field to store into.</param>
        public void StoreField(FieldInfo field)
        {
            if (field.IsStatic)
            {
                this.IL.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                this.IL.Emit(OpCodes.Stfld, field);
            }
        }

        /// <summary>
        /// Pushes the address of the specified local onto the stack.
        /// </summary>
        /// <param name="local">The local variable.</param>
        public void LoadLocalAddress(LocalBuilder local)
        {
            var index = local.LocalIndex;
            if (index < 0xFF)
            {
                this.IL.Emit(OpCodes.Ldloca_S, (byte)index);
            }
            else
            {
                this.IL.Emit(OpCodes.Ldloca, local);
            }
        }

        /// <summary>
        /// Unboxes the value on the top of the stack.
        /// </summary>
        /// <param name="type">The value type.</param>
        public void UnboxAny(Type type) => this.IL.Emit(OpCodes.Unbox_Any, type);

        /// <summary>
        /// Casts the object on the top of the stack to the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void CastClass(Type type) => this.IL.Emit(OpCodes.Castclass, type);

        /// <summary>
        /// Initializes the value type on the stack, setting all fields to their default value.
        /// </summary>
        /// <param name="type">The value type.</param>
        public void InitObject(Type type) => this.IL.Emit(OpCodes.Initobj, type);

        /// <summary>
        /// Constructs a new instance of the object with the specified constructor.
        /// </summary>
        /// <param name="constructor">The constructor to call.</param>
        public void NewObject(ConstructorInfo constructor)
        {
            this.IL.Emit(OpCodes.Newobj, constructor);
        }

        /// <summary>
        /// Builds a delegate from the previously emitted instructions.
        /// </summary>
        /// <returns>The delegate.</returns>
        public TDelegate CreateDelegate()
        {
            return this.dynamicMethod.CreateDelegate(typeof(TDelegate)) as TDelegate;
        }

        /// <summary>
        /// Pushes the specified local variable as a reference onto the stack.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="local">The local.</param>
        public void LoadLocalAsReference(Type type, LocalBuilder local)
        {
            if (type.IsValueType)
            {
                this.LoadLocalAddress(local);
            }
            else
            {
                this.LoadLocal(local);
            }
        }

        /// <summary>
        /// Boxes the value on the top of the stack if it's a value type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void BoxIfValueType(Type type)
        {
            if (type.IsValueType)
            {
                this.Box(type);
            }
        }

        /// <summary>
        /// Casts or unboxes the value at the top of the stack into the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void CastOrUnbox(Type type)
        {
            if (type.IsValueType)
            {
                this.UnboxAny(type);
            }
            else
            {
                this.CastClass(type);
            }
        }

        /// <summary>
        /// Creates a new instance of the specified type and stores it in the specified local.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="local">The local.</param>
        /// <param name="getUninitializedObject">The method used to get an uninitialized instance of a type.</param>
        public void CreateInstance(Type type, LocalBuilder local, MethodInfo getUninitializedObject)
        {
            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            if (type.IsValueType)
            {
                this.LoadLocalAddress(local);
                this.InitObject(type);
            }
            else if (constructorInfo != null)
            {
                // Use the default constructor.
                this.NewObject(constructorInfo);
                this.StoreLocal(local);
            }
            else
            {
                this.LoadType(type);
                this.Call(getUninitializedObject);
                this.CastClass(type);
                this.StoreLocal(local);
            }
        }

        private static Type[] GetParameterTypes(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var result = new Type[parameters.Length];
            for (var i = 0; i < parameters.Length; ++i)
            {
                result[i] = parameters[i].ParameterType;
            }

            return result;
        }
    }
}