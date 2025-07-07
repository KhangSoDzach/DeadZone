using System;
using UnityEngine;

/// <summary>
/// Static events for Endless Mode communication
/// </summary>
public static class EndlessGameEvents
{
    // Events for tracking game statistics
    public static event Action OnZombieKilled;
    public static event Action<int> OnCoinsDropped;
    public static event Action<int, float> OnDifficultyIncreased;
    
    /// <summary>
    /// Call when a zombie is killed
    /// </summary>
    public static void ZombieKilled()
    {
        OnZombieKilled?.Invoke();
    }
    
    /// <summary>
    /// Call when coins are dropped
    /// </summary>
    public static void CoinsDropped(int amount)
    {
        OnCoinsDropped?.Invoke(amount);
    }
    
    /// <summary>
    /// Call when difficulty increases
    /// </summary>
    public static void DifficultyIncreased(int level, float multiplier)
    {
        OnDifficultyIncreased?.Invoke(level, multiplier);
    }
    
    /// <summary>
    /// Clear all event listeners (call on scene change)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnZombieKilled = null;
        OnCoinsDropped = null;
        OnDifficultyIncreased = null;
    }
}
