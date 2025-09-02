using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Systems.SimpleCore.Identifiers.Abstract;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Systems.SimpleCore.Identifiers
{
    /// <summary>
    ///     Represents 64-bit non-unique identifier.
    /// </summary>
    [BurstCompile] [StructLayout(LayoutKind.Explicit)] [Serializable]
    public struct ID64 : INumberIdentifier<ulong>, IEquatable<ID64>, IComparable<ID64>
    {
        [FieldOffset(0)] [SerializeField] [HideInInspector] private uint4 vectorized; // 16B

        [FieldOffset(0)] [SerializeField] [HideInInspector] private ulong value; // 8B
        [FieldOffset(8)] [SerializeField] [HideInInspector] private byte isCreated; // 1B
        [FieldOffset(9)] [SerializeField] [HideInInspector] private byte reserved0; // 1B
        [FieldOffset(10)] [SerializeField] [HideInInspector] private ushort reserved1; // 2B
        [FieldOffset(12)] [SerializeField] [HideInInspector] private uint reserved2; // 4B

        /// <inheritdoc />
        public bool IsCreated => isCreated == 1;

        /// <summary>
        ///     Creates new ID64 identifier with given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public ID64(ulong value)
        {
            // Overriden by remaining data
            vectorized = default;

            this.value = value;
            isCreated = 1;
            reserved0 = 0;
            reserved1 = 0;
            reserved2 = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(ID64 other)
        {
            return math.all(other.vectorized == vectorized);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj)
        {
            return obj is ID64 other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode()
        {
            return vectorized.GetHashCode();
        }

        [BurstDiscard] [MethodImpl(MethodImplOptions.AggressiveInlining)] [NotNull]
        public override string ToString()
        {
            return $"{value:X16}";
        }

        /// <inheritdoc />
        public ulong Value => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(ID64 other)
        {
            int valueComparison = value.CompareTo(other.value);
            if (valueComparison != 0) return valueComparison;
            return isCreated.CompareTo(other.isCreated);
        }
    }
}