using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Saving.Data.Transitions
{
    /// <summary>
    ///     Result of a transition path search.
    /// </summary>
    public readonly struct TransitionInfo
    {
        /// <summary>Start type requested.</summary>
        public Type From { get; }

        /// <summary>Target type requested.</summary>
        public Type To { get; }

        /// <summary>True when a path was found (From may equal To).</summary>
        public bool IsPossible { get; }

        /// <summary>Ordered steps to convert From -> To. Empty when From == To and IsPossible == true.</summary>
        [NotNull] public IReadOnlyList<SaveFileTransitionStep> Steps { get; }

        public TransitionInfo(
            Type from,
            Type to,
            bool isPossible,
            [CanBeNull] IReadOnlyList<SaveFileTransitionStep> steps)
        {
            From = from;
            To = to;
            IsPossible = isPossible;
            Steps = steps ?? Array.Empty<SaveFileTransitionStep>();
        }

        [NotNull] public override string ToString()
        {
            if (!IsPossible) return $"No transition path from {From?.Name} to {To?.Name}.";
            if (Steps.Count == 0) return $"No-op transition (same type): {From?.Name}.";
            return $"Transition {From?.Name} -> {To?.Name}: {string.Join(" | ", Steps.Select(s => s.ToString()))}";
        }
    }
}