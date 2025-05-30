using System;
using System.Collections;
using UnityEngine;
using Scripts.API;
using DevionGames.StatSystem;

public class GameDataSynchronizer : MonoBehaviour
{
    private static GameDataSynchronizer _instance;
    public static GameDataSynchronizer Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameDataSynchronizer");
                _instance = go.AddComponent<GameDataSynchronizer>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }
    
    [Header("Settings")]
    [SerializeField] private bool debugMode = true;
    
    // Events
    public delegate void PlayerDataUpdatedHandler();
    public event PlayerDataUpdatedHandler OnPlayerDataUpdated;
    
    // Properties
    public bool IsDataLoaded { get; private set; }
    
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
    
    public void LoadGameData(Action<bool, string> onComplete)
    {
        if (!GameAPI.Instance.IsLoggedIn)
        {
            onComplete?.Invoke(false, "Not logged in");
            return;
        }
        
        StartCoroutine(LoadGameDataCoroutine(onComplete));
    }
    
    private IEnumerator LoadGameDataCoroutine(Action<bool, string> onComplete)
    {
        DebugLog("Loading game data...");
        
        yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
            if (success)
            {
                IsDataLoaded = true;
                OnPlayerDataUpdated?.Invoke();
                DebugLog("Game data loaded successfully");
            }
            else
            {
                DebugLog("Failed to load game data: " + error);
            }
            
            onComplete?.Invoke(success, error);
        }));
    }
    
    public void SaveGameData(Action<bool, string> onComplete)
    {
        if (!GameAPI.Instance.IsLoggedIn)
        {
            onComplete?.Invoke(false, "Not logged in");
            return;
        }
        
        if (!IsDataLoaded)
        {
            onComplete?.Invoke(false, "No data loaded");
            return;
        }
        
        StartCoroutine(SaveGameDataCoroutine(onComplete));
    }
    
    private IEnumerator SaveGameDataCoroutine(Action<bool, string> onComplete)
    {
        DebugLog("Saving game data...");
        
        // Update player data from game before saving
        CollectDataFromGame();
        
        yield return StartCoroutine(GameAPI.Instance.SavePlayerData((success, error) => {
            if (success)
            {
                DebugLog("Game data saved successfully");
            }
            else
            {
                DebugLog("Failed to save game data: " + error);
            }
            
            onComplete?.Invoke(success, error);
        }));
    }
    
    public void ApplyDataToGame()
    {
        if (!IsDataLoaded || GameAPI.Instance.PlayerData == null)
        {
            DebugLog("Cannot apply data to game: No data loaded");
            return;
        }
        
        DebugLog("Applying data to game...");
        // TODO: Apply player data to game components
        // This is where you'd update health, money, weapons, etc. in your game
    }      private void CollectDataFromGame()
    {
        if (GameAPI.Instance.PlayerData == null)
        {
            DebugLog("Cannot collect data from game: No player data");
            DebugLog($"GameAPI.Instance exists: {GameAPI.Instance != null}");
            DebugLog($"GameAPI.Instance.IsLoggedIn: {GameAPI.Instance?.IsLoggedIn ?? false}");
            DebugLog($"GameAPI.Instance.AuthToken exists: {!string.IsNullOrEmpty(GameAPI.Instance?.AuthToken)}");
            
            // Try to force reload player data
            DebugLog("Attempting to force reload player data...");
            StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
                if (success && GameAPI.Instance.PlayerData != null)
                {
                    DebugLog("Player data reloaded successfully, retrying data collection...");
                    CollectDataFromGame(); // Retry after successful reload
                }
                else
                {
                    DebugLog($"Failed to reload player data: {error}");
                }
            }));
            return;
        }
        
        // Validate that the player data has essential fields
        if (string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id) || string.IsNullOrEmpty(GameAPI.Instance.PlayerData.username))
        {
            DebugLog($"Cannot collect data from game: Invalid player data - ID: '{GameAPI.Instance.PlayerData.id}', Username: '{GameAPI.Instance.PlayerData.username}'");
            DebugLog("Attempting to force reload player data due to invalid fields...");
            
            StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
                if (success && GameAPI.Instance.PlayerData != null && 
                    !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id))
                {
                    DebugLog("Player data reloaded successfully after validation failure, retrying...");
                    CollectDataFromGame(); // Retry after successful reload
                }
                else
                {
                    DebugLog($"Failed to reload player data after validation failure: {error}");
                }
            }));
            return;
        }
          DebugLog($"Collecting data from game for user: {GameAPI.Instance.PlayerData.username} (ID: {GameAPI.Instance.PlayerData.id})");
        
        // Collect current game state and update PlayerData
        try
        {
            // 1. Collect health from StatsHandler
            CollectHealthFromGame();
            
            // 2. Collect money from ScoreManager and StatsHandler
            CollectMoneyFromGame();
            
            // 3. Collect weapon data from guns in scene
            CollectWeaponsFromGame();
            
            DebugLog("Successfully collected all game data");
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error collecting game data: {ex.Message}");
        }
        
        // Update last login timestamp
        GameAPI.Instance.PlayerData.lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    public void ClearData()
    {
        IsDataLoaded = false;
        DebugLog("Data cleared");
    }
    
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GameDataSynchronizer] {message}");
        }
    }
    
    /// <summary>
    /// Collects current health from the StatsHandler system
    /// </summary>
    private void CollectHealthFromGame()
    {
        try
        {
            // Try to get health from DevionGames StatsHandler first
            StatsHandler playerStatsHandler = FindObjectOfType<StatsHandler>();
            if (playerStatsHandler != null)
            {
                var healthStat = playerStatsHandler.GetStat("Health");
                if (healthStat is DevionGames.StatSystem.Attribute healthAttribute)
                {
                    GameAPI.Instance.PlayerData.health = healthAttribute.CurrentValue;
                    DebugLog($"Collected health from StatsHandler: {GameAPI.Instance.PlayerData.health}");
                    return;
                }
            }
            
            // Fallback: Try to get from GameStatsIntegration
            GameStatsIntegration statsIntegration = FindObjectOfType<GameStatsIntegration>();
            if (statsIntegration != null)
            {
                statsIntegration.UpdatePlayerDataFromStats();
                DebugLog($"Updated health via GameStatsIntegration: {GameAPI.Instance.PlayerData.health}");
                return;
            }
            
            DebugLog("No health data source found, keeping current value");
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error collecting health: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Collects current money/score from ScoreManager and StatsHandler
    /// </summary>
    private void CollectMoneyFromGame()
    {
        try
        {
            int collectedMoney = 0;
            bool moneyFound = false;
            
            // Try to get money from ScoreManager first (most direct source)
            if (ScoreManager.Instance != null)
            {
                collectedMoney = ScoreManager.Score;
                moneyFound = true;
                DebugLog($"Collected money from ScoreManager: {collectedMoney}");
            }
            
            // Also try StatsHandler for "Money" stat
            if (!moneyFound)
            {
                StatsHandler playerStatsHandler = FindObjectOfType<StatsHandler>();
                if (playerStatsHandler != null)
                {
                    var moneyStat = playerStatsHandler.GetStat("Money");
                    if (moneyStat != null)
                    {
                        collectedMoney = (int)playerStatsHandler.GetStatValue("Money");
                        moneyFound = true;
                        DebugLog($"Collected money from StatsHandler: {collectedMoney}");
                    }
                }
            }
            
            // Update player data with collected money
            if (moneyFound)
            {
                GameAPI.Instance.PlayerData.money = collectedMoney;
            }
            else
            {
                DebugLog("No money data source found, keeping current value");
            }
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error collecting money: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Collects current weapon data from guns in the scene
    /// </summary>
    private void CollectWeaponsFromGame()
    {
        try
        {
            // Use the GameAPIExtensions method to sync weapons from game
            GameAPI.Instance.PlayerData.SyncWeaponsFromGame();
            DebugLog($"Collected weapon data, total weapons: {GameAPI.Instance.PlayerData.weapons?.Count ?? 0}");
            
            // Also collect ammo data from WeaponManager if available
            WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
            if (weaponManager != null)
            {
                // Update ammo counts for pistol and rifle types
                int pistolAmmo = weaponManager.GetAmmoCount("pistol");
                int rifleAmmo = weaponManager.GetAmmoCount("rifle");
                
                DebugLog($"Collected ammo - Pistol: {pistolAmmo}, Rifle: {rifleAmmo}");
                
                // Update weapon ammo in player data
                if (GameAPI.Instance.PlayerData.weapons != null)
                {
                    foreach (var weapon in GameAPI.Instance.PlayerData.weapons)
                    {
                        // Check if this is a pistol or rifle and update ammo accordingly
                        Gun gun = FindGunByName(weapon.name);
                        if (gun != null)
                        {
                            if (gun.isPistol)
                            {
                                weapon.ammo = gun.totalAmmo;
                            }
                            else
                            {
                                weapon.ammo = gun.totalAmmo;
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error collecting weapons: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Helper method to find a gun by name in the scene
    /// </summary>
    private Gun FindGunByName(string weaponName)
    {
        Gun[] allGuns = FindObjectsOfType<Gun>(true); // Include inactive guns
        foreach (Gun gun in allGuns)
        {
            if (gun.name.Equals(weaponName, System.StringComparison.OrdinalIgnoreCase))
            {
                return gun;
            }
        }
        return null;
    }
}
