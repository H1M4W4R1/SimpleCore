using JetBrains.Annotations;

namespace Systems.SimpleCore.Identifiers.Abstract
{
    /// <summary>
    ///     Represents an identifier - a value that can be used to identify an object.
    ///     Warning: this does not imply that the identifier is unique. For that purpose
    ///     see <see cref="IUniqueIdentifier" />.
    /// </summary>
    public interface IIdentifier
    {
        /// <summary>
        ///     Checks if the identifier was created.
        /// </summary>
        public bool IsCreated { get; }

        /// <summary>
        ///     Get nicely-formatted string representation of the identifier.
        /// </summary>
        [NotNull] public string GetDebugTooltipText();
    }
}