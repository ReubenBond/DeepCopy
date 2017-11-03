namespace DeepCopy
{
    /// <summary>
    /// Deep copier delegate.
    /// </summary>
    /// <param name="original">Original object to be deep copied.</param>
    /// <param name="context">The serialization context.</param>
    /// <returns>Deep copy of the original object.</returns>
    internal delegate object DeepCopyDelegate(object original, CopyContext context);
}