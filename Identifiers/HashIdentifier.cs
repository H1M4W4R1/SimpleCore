using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Systems.SimpleCore.Identifiers.Abstract;

namespace Systems.SimpleCore.Identifiers
{
    /// <summary>
    ///     Hash-based identifier
    /// </summary>
    public readonly struct HashIdentifier : INumberIdentifier<ulong>,
        IEquatable<HashIdentifier>, IComparable<HashIdentifier>
    {
        public readonly ulong value;
        
        public bool IsCreated => value != 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public HashIdentifier(ulong value)
        {
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static HashIdentifier New([NotNull] Type type)
        {
            return new HashIdentifier(ComputeTypeHash(type));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static HashIdentifier New([NotNull] object obj)
        {
            Type type = obj.GetType();
            return new HashIdentifier(ComputeTypeHash(type));
        }

        /// <summary>
        /// Computes a deterministic (per process run) 64-bit hash for the given type.
        /// Works across polymorphic instances (different subclasses should give different hashes).
        /// </summary>
        public static ulong ComputeTypeHash([NotNull] Type type)
        {
            const ulong OFFSET = 14695981039346656037UL;
            const ulong PRIME = 1099511628211UL;

            ulong hash = OFFSET;
            hash ^= (uint) type.GetHashCode();
            hash *= PRIME;
            hash ^= (uint) type.Assembly.GetHashCode();
            hash *= PRIME;
            
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(HashIdentifier other)
            => value == other.value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(HashIdentifier other)
            => value.CompareTo(other.value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj)
            => obj is HashIdentifier other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode()
            => value.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] [NotNull] public override string ToString()
            => value.ToString("X");
        
        /// <inheritdoc/>
        public ulong Value => value;
    }
}