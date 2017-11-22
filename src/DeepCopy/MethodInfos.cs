using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace DeepCopy
{
    /// <summary>
    /// Holds references to methods which are used during copying.
    /// </summary>
    internal sealed class MethodInfos
    {
        /// <summary>
        /// A reference to the <see cref="CopyContext.TryGetCopy"/> method.
        /// </summary>
        public readonly MethodInfo TryGetCopy;
        
        /// <summary>
        /// A reference to the <see cref="CopyContext.RecordCopy"/> method.
        /// </summary>
        public readonly MethodInfo RecordObject;

        /// <summary>
        /// A reference to <see cref="DeepCopier.Copy{T}(T, CopyContext)"/>
        /// </summary>
        public readonly MethodInfo CopyInner;

        /// <summary>
        /// A reference to a method which returns an uninitialized object of the provided type.
        /// </summary>
        public readonly MethodInfo GetUninitializedObject;

        /// <summary>
        /// A reference to <see cref="Type.GetTypeFromHandle"/>.
        /// </summary>
        public readonly MethodInfo GetTypeFromHandle;

        public readonly MethodInfo CopyArrayRank1Shallow;
        public readonly MethodInfo CopyArrayRank1Class;

        public readonly MethodInfo CopyArrayRank2Shallow;
        public readonly MethodInfo CopyArrayRank2;
        
        public MethodInfos()
        {
            this.GetUninitializedObject = GetFuncCall(() => FormatterServices.GetUninitializedObject(typeof(int)));
            this.GetTypeFromHandle = GetFuncCall(() => Type.GetTypeFromHandle(typeof(Type).TypeHandle));
            this.CopyInner = GetFuncCall(() => DeepCopier.Copy(default(object), default(CopyContext))).GetGenericMethodDefinition();
            this.TryGetCopy = typeof(CopyContext).GetMethod("TryGetCopy");
            this.RecordObject = GetActionCall((CopyContext ctx) => ctx.RecordCopy(default(object), default(object)));

            this.CopyArrayRank1Shallow = GetFuncCall(() => ArrayCopier.CopyArrayRank1Shallow(default(object[]), default(CopyContext))).GetGenericMethodDefinition();
            this.CopyArrayRank1Class = GetFuncCall(() => ArrayCopier.CopyArrayRank1(default(object[]), default(CopyContext))).GetGenericMethodDefinition();

            this.CopyArrayRank2Shallow = GetFuncCall(() => ArrayCopier.CopyArrayRank2Shallow(default(object[,]), default(CopyContext))).GetGenericMethodDefinition();
            this.CopyArrayRank2 = GetFuncCall(() => ArrayCopier.CopyArrayRank2(default(object[,]), default(CopyContext))).GetGenericMethodDefinition();

            MethodInfo GetActionCall<T>(Expression<Action<T>> expression)
            {
                return (expression.Body as MethodCallExpression)?.Method
                       ?? throw new ArgumentException("Expression type unsupported.");
            }

            MethodInfo GetFuncCall<T>(Expression<Func<T>> expression)
            {
                return (expression.Body as MethodCallExpression)?.Method
                       ?? throw new ArgumentException("Expression type unsupported.");
            }
        }
    }
}