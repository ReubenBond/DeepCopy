using System.Threading;

namespace DeepCopy
{
    /// <summary>
    /// Methods for creating deep copies of objects.
    /// </summary>
    public static class DeepCopier
    {
        internal static readonly CopyPolicy CopyPolicy = new CopyPolicy();
        internal static readonly MethodInfos MethodInfos = new MethodInfos();
        private static readonly ThreadLocal<CopyContext> Context = new ThreadLocal<CopyContext>(() => new CopyContext());

        /// <summary>
        /// Creates and returns a deep copy of the provided object.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="original">The object to copy.</param>
        /// <returns>A deep copy of the provided object.</returns>
        public static T Copy<T>(T original)
        {
            var context = Context.Value;
            try
            {
                return CopierGenerator<T>.Copy(original, context);
            }
            finally
            {
                context.Reset();
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
            return CopierGenerator<T>.Copy(original, context);
        }
    }
}
