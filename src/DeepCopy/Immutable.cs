namespace DeepCopy
{
    /// <summary>
    /// Helper class for creating immutable values.
    /// </summary>
    public static class Immutable
    {
        /// <summary>
        /// Returns an immutable wrapper over the provided value.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>An immutable wrapper over the provided value.</returns>
        public static Immutable<T> Create<T>(T value) => new Immutable<T>(value);
    }

    /// <summary>
    /// Wrapper class for creating immutable values.
    /// </summary>
    /// <typeparam name="T">The wrapped type.</typeparam>
    [Immutable]
    public struct Immutable<T>
    {
        public Immutable(T value)
        {
            this.Value = value;
        }

        public T Value { get; }
    }
}