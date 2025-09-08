using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Systems.SimpleCore.Identifiers.Abstract;
using Unity.Burst.CompilerServices;
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
    public struct Snowflake128 : IUniqueIdentifier, IEquatable<Snowflake128>, IComparable<Snowflake128>
    {
        /// <summary>
        ///     Local counter for id creation
        /// </summary>
        private static uint idGeneratorCounter;

        [FieldOffset(0)] [SerializeField] [HideInInspector] private int4 vectorized;
        [FieldOffset(0)] [SerializeField] [HideInInspector] private long timestamp;
        [FieldOffset(8)] [SerializeField] [HideInInspector] private ulong cyclicIndex;

        /// <inheritdoc />
        public bool IsCreated => timestamp != 0;

        /// <summary>
        ///     Creates new Snowflake128 identifier with given timestamp, identifier data and additional data.
        /// </summary>
        public Snowflake128(long timestamp, ulong cyclicIndex)
        {
            // This value is overriden by remaining data and thus should be ignored 
            vectorized = default;

            this.timestamp = timestamp;
            this.cyclicIndex = cyclicIndex;
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
            return $"{timestamp:X16}-{cyclicIndex:X16}";
        }

        public static Snowflake128 Empty => default;
        public static Snowflake128 New() => new(DateTime.UtcNow.Ticks, idGeneratorCounter++);

        public string GetDebugTooltipText()
        {
            StringBuilder tooltipBuilder = new();
            tooltipBuilder.AppendLine("<b>Identifier data</b>");
            tooltipBuilder.AppendLine($"<color=#00FFFF>Ticks:</color> {timestamp:X16}");
            tooltipBuilder.AppendLine(
                $"<color=#00FFFF>Creation date [UTC]:</color> {new DateTime(timestamp):yyyy-MM-dd HH:mm:ss}");
            tooltipBuilder.AppendLine($"<color=#00FFFF>Cyclic index:</color> {cyclicIndex:X8}");
            tooltipBuilder.AppendLine(""); // spacer
            tooltipBuilder.Append(
                $"<color=#00FFFF>Is created:</color> {(IsCreated ? "<color=green>Yes</color>" : "<color=red>No</color>")}");
            return tooltipBuilder.ToString();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(Snowflake128 other)
        {
            int ticksCompareResult = timestamp.CompareTo(other.timestamp);
            if (Hint.Unlikely(ticksCompareResult == 0)) return cyclicIndex.CompareTo(other.cyclicIndex);
            return ticksCompareResult;
        }
    }
}