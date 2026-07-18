using System;

public static class PlayerEvents
{
    public static event Action OnPlayerDied;

    public static event Action OnPlayerRespawned;

    public static event Action<int> OnPenaltyCountdown;

    /// <summary>Player HP changed; (current, max). Drives the top-left HP bar.</summary>
    public static event Action<int, int> OnPlayerHealthChanged;

    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
    public static void RaisePlayerRespawned() => OnPlayerRespawned?.Invoke();
    public static void RaisePenaltyCountdown(int secondsRemaining) => OnPenaltyCountdown?.Invoke(secondsRemaining);
    public static void RaisePlayerHealthChanged(int current, int max) => OnPlayerHealthChanged?.Invoke(current, max);
}
