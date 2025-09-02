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
    ///     Represents 16-bit non-unique identifier.
    /// </summary>
    [BurstCompile] [StructLayout(LayoutKind.Explicit)] [Serializable]
    public struct ID16 : INumberIdentifier<ushort>, IEquatable<ID16>
    {
        [FieldOffset(0)] [SerializeField] [HideInInspector] private ushort value;
        [FieldOffset(2)] [SerializeField] [HideInInspector] private byte isCreated;
        [FieldOffset(3)] [SerializeField] [HideInInspector] private byte reserved;

        /// <inheritdoc />
        public bool IsCreated => isCreated == 1;

        /// <summary>
        ///     Creates new ID16 identifier with given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public ID16(ushort value)
        {
            this.value = value;
            isCreated = 1;
            reserved = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(ID16 other)
        {
            return other.value == value && other.isCreated == isCreated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj)
        {
            return obj is ID16 other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override unsafe int GetHashCode()
        {
            fixed (ushort* p = &value)
            {
                return (*(int*) p).GetHashCode();
            }
        }

        [BurstDiscard] [MethodImpl(MethodImplOptions.AggressiveInlining)] [NotNull]
        public override string ToString()
        {
            return $"{value:X4}";
        }

        /// <inheritdoc />
        public ushort Value => value;
    }
}