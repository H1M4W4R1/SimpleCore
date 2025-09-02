using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Systems.SimpleCore.Identifiers.Abstract;
using Unity.Burst;
using UnityEngine;

namespace Systems.SimpleCore.Identifiers
{
    /// <summary>
    ///     Represents 32-bit non-unique identifier.
    /// </summary>
    [BurstCompile] [StructLayout(LayoutKind.Explicit)] [Serializable]
    public struct ID32 : INumberIdentifier<uint>, IEquatable<ID32>
    {
        [FieldOffset(0)] [SerializeField] [HideInInspector] private uint value;
        [FieldOffset(4)] [SerializeField] [HideInInspector] private byte isCreated;
        [FieldOffset(5)]  [SerializeField] [HideInInspector]private byte reserved0;
        [FieldOffset(6)]  [SerializeField] [HideInInspector]private ushort reserved1;

        /// <inheritdoc />
        public bool IsCreated => isCreated == 1;

        /// <summary>
        ///     Creates new ID32 identifier with given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public ID32(
            uint value)
        {
            this.value = value;
            isCreated = 1;
            reserved0 = 0;
            reserved1 = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(
            ID32 other)
        {
            return other.value == value && other.isCreated == isCreated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(
            object obj)
        {
            return obj is ID32 other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override unsafe int GetHashCode()
        {
            fixed (uint* p = &value)
            {
                return (*(long*) p).GetHashCode();
            }
        }

        [BurstDiscard] [MethodImpl(MethodImplOptions.AggressiveInlining)] [NotNull]
        public override string ToString()
        {
            return $"{value:X8}";
        }

        /// <inheritdoc />
        public uint Value => value;
    }
}