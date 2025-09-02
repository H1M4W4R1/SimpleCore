namespace Systems.SimpleCore.Identifiers.Abstract
{
    /// <summary>
    ///     Represents a unique identifier.
    ///     This identifier is unique in the context of the system that created it.
    /// </summary>
    /// <remarks>
    ///     Your implementation must ensure that the identifier is unique.
    /// </remarks>
    public interface IUniqueIdentifier : IIdentifier
    {
    }
}