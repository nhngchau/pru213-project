using System;

/// <summary>
/// Decoupled event hub for player life-cycle signals (GDD v3.0 - Section V).
/// Publishers (PlayerHealth) and subscribers (EnemyBehavior, PenaltyUI) never reference each
/// other directly - they only know this static hub. This keeps the Respawn/Penalty flow modular.
/// </summary>
public static class PlayerEvents
{
    /// <summary>Raised the moment PlayerHP reaches 0 (player enters Downtime).</summary>
    public static event Action OnPlayerDied;

    /// <summary>Raised after the 5s penalty, once the player is back in play.</summary>
    public static event Action OnPlayerRespawned;

    /// <summary>Penalty countdown tick. Argument = whole seconds remaining (0 = hide UI).</summary>
    public static event Action<int> OnPenaltyCountdown;

    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
    public static void RaisePlayerRespawned() => OnPlayerRespawned?.Invoke();
    public static void RaisePenaltyCountdown(int secondsRemaining) => OnPenaltyCountdown?.Invoke(secondsRemaining);
}
