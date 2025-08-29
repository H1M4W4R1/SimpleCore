using System;
using JetBrains.Annotations;
using Systems.SimpleCore.Saving.Data.Enums;

/// <summary>
///     One step in a transition path (From -> To, and whether it's an upgrade or a downgrade).
/// </summary>
public readonly struct SaveFileTransitionStep
{
    public Type From { get; }
    public Type To { get; }
    public SaveFileTransitionKind Kind { get; }

    public SaveFileTransitionStep([NotNull] Type from, [NotNull] Type to, SaveFileTransitionKind kind)
    {
        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));
        Kind = kind;
    }

    [NotNull] public override string ToString() => $"{From?.Name} -> {To?.Name} ({Kind})";
}