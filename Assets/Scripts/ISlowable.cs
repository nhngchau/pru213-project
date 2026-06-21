/// <summary>
/// Contract for entities whose Move Speed can be modified by external sources
/// (GDD v3.0 - Memory Leak 'Sludge'). Source-keyed so each effect can be reverted
/// independently and safely, even when several overlap at once. Decouples the Sludge
/// hazard from the concrete PlayerController.
/// </summary>
public interface ISlowable
{
    /// <summary>Register or refresh a speed multiplier from a source (e.g. 0.5 = 50% speed).</summary>
    void AddSpeedModifier(object source, float multiplier);

    /// <summary>Remove a source's multiplier and recompute. Safe to call if the source is absent.</summary>
    void RemoveSpeedModifier(object source);
}
