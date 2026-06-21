using System;

public static class PlayerEvents
{
    public static event Action OnPlayerDied;

    public static event Action OnPlayerRespawned;

    public static event Action<int> OnPenaltyCountdown;

    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
    public static void RaisePlayerRespawned() => OnPlayerRespawned?.Invoke();
    public static void RaisePenaltyCountdown(int secondsRemaining) => OnPenaltyCountdown?.Invoke(secondsRemaining);
}
