using System.Text;

namespace Systems.SimpleCore.Identifiers.Abstract
{
    /// <summary>
    ///     Represents identifier with number data for given number type.
    /// </summary>
    public interface INumberIdentifier<out TNumber> : INumberIdentifier
        where TNumber : notnull
    {
        /// <summary>
        ///     Gets value of the identifier.
        /// </summary>
        public TNumber Value { get; }

        string IIdentifier.GetDebugTooltipText()
        {
            StringBuilder tooltipBuilder = new();
            tooltipBuilder.AppendLine("<b>Identifier data</b>");
            tooltipBuilder.AppendLine($"<color=#00FFFF>Value:</color> {ToString()}");
            tooltipBuilder.AppendLine(""); // spacer
            tooltipBuilder.Append(
                $"<color=#00FFFF>Is created:</color> {(IsCreated ? "<color=green>Yes</color>" : "<color=red>No</color>")}");
            return tooltipBuilder.ToString();
        }
    }

    /// <summary>
    ///     Represents identifier with number data.
    ///     Used as low-level abstraction for number identifiers and shall be only used in
    ///     "where" constraints. For actual usage, see <see cref="INumberIdentifier{TNumber}" />
    /// </summary>
    public interface INumberIdentifier : IIdentifier
    {
    }
}