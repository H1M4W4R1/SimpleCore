using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Systems.SimpleCore.Identifiers.Abstract;
using Unity.Mathematics;
using UnityEngine;

namespace Systems.SimpleCore.Identifiers
{
    /// <summary>
    ///     128-bit unique identifier inspired by Twitter/X Snowflake.
    ///     Snowflake128 consists of:
    ///     <ul>
    ///         <li> 64 bits for <b>timestamp</b></li>
    ///         <li> 32 bits for additional identifier data</li>
    ///         <li> 16 bits for additional data</li>
    ///         <li> 8 bits reserved for future use</li>
    ///         <li> 8 bits representing that identifier was created</li>
    ///     </ul>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)] [Serializable]
    public struct Snowflake128 : IUniqueIdentifier, IEquatable<Snowflake128>
    {
        /// <summary>
        ///     Local counter for id creation
        /// </summary>
        private static uint idGeneratorCounter;

        [FieldOffset(0)] [SerializeField] [HideInInspector] private int4 vectorized;
        [FieldOffset(0)] [SerializeField] [HideInInspector] private long timestamp;
        [FieldOffset(8)] [SerializeField] [HideInInspector] private uint identifierData;
        [FieldOffset(12)] [SerializeField] [HideInInspector] private ushort additionalData;
        [FieldOffset(14)] [SerializeField] [HideInInspector] private byte reserved;
        [FieldOffset(15)] [SerializeField] [HideInInspector] private byte created;

        /// <inheritdoc />
        public bool IsCreated => created == 1;

        /// <summary>
        ///     Creates new Snowflake128 identifier with given timestamp, identifier data and additional data.
        /// </summary>
        public Snowflake128(long timestamp, uint identifierData, ushort additionalData)
        {
            // This value is overriden by remaining data and thus should be ignored 
            vectorized = default;

            this.timestamp = timestamp;
            this.identifierData = identifierData;
            this.additionalData = additionalData;
            reserved = 0;
            created = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(Snowflake128 other)
        {
            return math.all(other.vectorized == vectorized);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj)
        {
            return obj is Snowflake128 other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode()
        {
            return vectorized.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Snowflake128 left, Snowflake128 right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Snowflake128 left, Snowflake128 right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] [NotNull] public override string ToString()
        {
            return $"{timestamp:X16}-{identifierData:X8}-{additionalData:X4}-{reserved:X2}-{created:X2}";
        }

        public static Snowflake128 Empty => default;
        public static Snowflake128 New => new(DateTime.UtcNow.Ticks, idGeneratorCounter++, 0);

        public string GetDebugTooltipText()
        {
            StringBuilder tooltipBuilder = new();
            tooltipBuilder.AppendLine("<b>Identifier data</b>");
            tooltipBuilder.AppendLine($"<color=#00FFFF>Ticks:</color> {timestamp:X16}");
            tooltipBuilder.AppendLine(
                $"<color=#00FFFF>Creation date [UTC]:</color> {new DateTime(timestamp):yyyy-MM-dd HH:mm:ss}");
            tooltipBuilder.AppendLine($"<color=#00FFFF>Cyclic index:</color> {identifierData:X8}");
            tooltipBuilder.AppendLine($"<color=#00FFFF>Additional data:</color> {additionalData:X4}");
            tooltipBuilder.AppendLine(""); // spacer
            tooltipBuilder.Append(
                $"<color=#00FFFF>Is created:</color> {(created > 0 ? "<color=green>Yes</color>" : "<color=red>No</color>")}");
            return tooltipBuilder.ToString();
        }
    }
}