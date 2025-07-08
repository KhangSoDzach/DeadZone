using System;
using UnityEngine;



public static class EndlessGameEvents
{
    // Events for tracking game statistics
    public static event Action OnZombieKilled;
    public static event Action<int> OnCoinsDropped;
    public static event Action<int, float> OnDifficultyIncreased;
    

    public static void ZombieKilled()
    {
        OnZombieKilled?.Invoke();
    }

    public static void CoinsDropped(int amount)
    {
        OnCoinsDropped?.Invoke(amount);
    }

    public static void DifficultyIncreased(int level, float multiplier)
    {
        OnDifficultyIncreased?.Invoke(level, multiplier);
    }
    

    public static void ClearAllEvents()
    {
        OnZombieKilled = null;
        OnCoinsDropped = null;
        OnDifficultyIncreased = null;
    }
}
