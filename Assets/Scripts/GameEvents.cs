using System;

/// <summary>
/// Decoupled hub for Wave / Economy / Upgrade signals (GDD v3.0 - Sections II &amp; VI).
/// Gameplay systems raise these; the UI listens. Mirrors the PlayerEvents pattern so the UI
/// layer never references gameplay managers directly (and vice-versa).
/// </summary>
public static class GameEvents
{
    // --- Economy ---------------------------------------------------------
    /// <summary>A Bug was killed; argument = DataPack reward (GDD: 5 / 10 / 25).</summary>
    public static event Action<int> OnDataPackAwarded;
    /// <summary>DataPack wallet changed; argument = new total (for the HUD).</summary>
    public static event Action<int> OnDataPackChanged;

    // --- Wave / Build Progress ------------------------------------------
    /// <summary>A new wave started; argument = wave number (1-based).</summary>
    public static event Action<int> OnWaveStarted;
    /// <summary>A wave finished and the upgrade break begins; argument = finished wave number.</summary>
    public static event Action<int> OnWaveEnded;
    /// <summary>Build progress changed; argument = percent 0..100 (for the HUD bar).</summary>
    public static event Action<float> OnBuildProgressChanged;

    // --- Upgrade ---------------------------------------------------------
    /// <summary>An upgrade was purchased (UI should refresh costs / affordability).</summary>
    public static event Action OnUpgradePurchased;
    /// <summary>Player dismissed the upgrade panel (Skip/Continue) - resume the game.</summary>
    public static event Action OnContinueRequested;

    public static void RaiseDataPackAwarded(int amount) => OnDataPackAwarded?.Invoke(amount);
    public static void RaiseDataPackChanged(int total) => OnDataPackChanged?.Invoke(total);
    public static void RaiseWaveStarted(int wave) => OnWaveStarted?.Invoke(wave);
    public static void RaiseWaveEnded(int wave) => OnWaveEnded?.Invoke(wave);
    public static void RaiseBuildProgressChanged(float percent) => OnBuildProgressChanged?.Invoke(percent);
    public static void RaiseUpgradePurchased() => OnUpgradePurchased?.Invoke();
    public static void RaiseContinueRequested() => OnContinueRequested?.Invoke();
}
