namespace DeepCopy
{
    /// <summary>
    /// Deep copier delegate.
    /// </summary>
    /// <param name="original">Original object to be deep copied.</param>
    /// <param name="context">The context.</param>
    /// <returns>Deep copy of the original object.</returns>
    internal delegate T DeepCopyDelegate<T>(T original, CopyContext context);
}