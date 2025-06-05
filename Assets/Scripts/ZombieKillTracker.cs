using UnityEngine;

/// <summary>
/// Singleton class to track zombie kills throughout the game
/// </summary>
public class ZombieKillTracker : MonoBehaviour
{
    private static ZombieKillTracker _instance;
    public static ZombieKillTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ZombieKillTracker");
                _instance = go.AddComponent<ZombieKillTracker>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }
    
    [Header("Kill Tracking")]
    [SerializeField] private int totalKills = 0;
    [SerializeField] private bool debugMode = true;
    
    // Events
    public delegate void ZombieKilledHandler(int newTotalKills);
    public event ZombieKilledHandler OnZombieKilled;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Call this method when a zombie is killed
    /// </summary>
    public void RegisterZombieKill()
    {
        totalKills++;
        DebugLog($"Zombie killed! Total kills: {totalKills}");
        OnZombieKilled?.Invoke(totalKills);
    }
    
    /// <summary>
    /// Get the current total kill count
    /// </summary>
    public int GetKillCount()
    {
        return totalKills;
    }
    
    /// <summary>
    /// Set the kill count (used when loading saved data)
    /// </summary>
    public void SetKillCount(int kills)
    {
        totalKills = kills;
        DebugLog($"Kill count set to: {totalKills}");
    }
    
    /// <summary>
    /// Reset kill count (for new game)
    /// </summary>
    public void ResetKills()
    {
        totalKills = 0;
        DebugLog("Kill count reset to 0");
    }
    
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[ZombieKillTracker] {message}");
        }
    }
}
