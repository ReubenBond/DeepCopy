using System.Collections.Generic;

namespace DeepCopy
{
    /// <summary>
    /// Records details about copied objects.
    /// </summary>
    public sealed class CopyContext
    {
        private readonly Dictionary<object, object> copies
            = new Dictionary<object, object>(ReferenceEqualsComparer.Instance);

        /// <summary>
        /// Records <paramref name="copy"/> as a copy of <paramref name="original"/>.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <param name="copy">The copy of <paramref name="original"/>.</param>
        public void RecordCopy(object original, object copy)
        {
            if (!this.copies.ContainsKey(original))
            {
                this.copies[original] = copy;
            }
        }

        /// <summary>
        /// Returns the copy of <paramref name="original"/> if it has been copied or <see langword="null"/> if it has not yet been copied.
        /// </summary>
        /// <param name="original">The original object.</param>
        /// <returns>The copy of <paramref name="original"/> or <see langword="null"/> if no copy has been made.</returns>
        public object TryGetCopy(object original)
        {
            var found = this.copies.TryGetValue(original, out var copy);
            return found ? copy : null;
        }

        /// <summary>
        /// Resets this instance so that it can be reused.
        /// </summary>
        internal void Reset()
        {
            this.copies.Clear();
        }
    }
}