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
        
        // Force collect fresh data from game before saving to ensure we have latest state
        DebugLog("Collecting latest game state for save...");
        CollectDataFromGame();
        
        // Add verification that we have valid data to save
        if (GameAPI.Instance.PlayerData == null)
        {
            DebugLog("Error: No PlayerData after collection - cannot save");
            onComplete?.Invoke(false, "No player data to save");
            yield break;
        }
        
        if (string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id) || string.IsNullOrEmpty(GameAPI.Instance.PlayerData.username))
        {
            DebugLog($"Error: Invalid PlayerData after collection - ID: '{GameAPI.Instance.PlayerData.id}', Username: '{GameAPI.Instance.PlayerData.username}'");
            onComplete?.Invoke(false, "Invalid player data to save");
            yield break;
        }
        
        DebugLog($"Verified PlayerData ready for save - User: {GameAPI.Instance.PlayerData.username}, Health: {GameAPI.Instance.PlayerData.health}, Money: {GameAPI.Instance.PlayerData.money}, Level: {GameAPI.Instance.PlayerData.level}");
        
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
            // Force reload fresh data when applying to game
            LoadGameData((success, error) => {
                if (success)
                {
                    DebugLog("Fresh data loaded, now applying to game");
                    PerformApplyDataToGame();
                }
                else
                {
                    DebugLog($"Failed to load fresh data for game: {error}");
                }
            });
            return;
        }
        
        PerformApplyDataToGame();
    }
    
    private void PerformApplyDataToGame()
    {
        DebugLog("Applying fresh data to game...");
        // TODO: Apply player data to game components
        // This is where you'd update health, money, weapons, etc. in your game
        
        // Example: Apply checkpoint data to move player to saved position
        if (GameAPI.Instance.PlayerData.checkpoint != null && 
            !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.checkpoint.sceneId))
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene == GameAPI.Instance.PlayerData.checkpoint.sceneId)
            {
                // Apply saved position
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null && GameAPI.Instance.PlayerData.checkpoint.position != null)
                {
                    player.transform.position = GameAPI.Instance.PlayerData.checkpoint.position.ToVector3();
                    DebugLog($"Applied checkpoint position: {GameAPI.Instance.PlayerData.checkpoint.position.ToVector3()}");
                }
            }
        }
        
        DebugLog("Data applied to game successfully");
    }private void CollectDataFromGame()
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
        }        DebugLog($"Collecting data from game for user: {GameAPI.Instance.PlayerData.username} (ID: {GameAPI.Instance.PlayerData.id})");
        
        // Store current values for comparison
        float oldHealth = GameAPI.Instance.PlayerData.health;
        int oldMoney = GameAPI.Instance.PlayerData.money;
        int oldLevel = GameAPI.Instance.PlayerData.level;
        int oldKills = GameAPI.Instance.PlayerData.kills;
        
        // Collect current game state and update PlayerData
        try
        {
            // 1. Collect health from StatsHandler
            CollectHealthFromGame();
            
            // 2. Collect money from ScoreManager and StatsHandler
            CollectMoneyFromGame();
            
            // 3. Collect weapon data from guns in scene
            CollectWeaponsFromGame();
            
            // 4. Collect level and experience from StatsHandler
            CollectLevelAndExperienceFromGame();
            
            // 5. Collect zombie kills from ZombieKillTracker
            CollectZombieKillsFromGame();
              // 6. Collect player position for checkpoint
            CollectPlayerPositionFromGame();
            
            // Log what changed during collection
            bool dataChanged = false;
            if (GameAPI.Instance.PlayerData.health != oldHealth)
            {
                DebugLog($"Health changed: {oldHealth} -> {GameAPI.Instance.PlayerData.health}");
                dataChanged = true;
            }
            if (GameAPI.Instance.PlayerData.money != oldMoney)
            {
                DebugLog($"Money changed: {oldMoney} -> {GameAPI.Instance.PlayerData.money}");
                dataChanged = true;
            }
            if (GameAPI.Instance.PlayerData.level != oldLevel)
            {
                DebugLog($"Level changed: {oldLevel} -> {GameAPI.Instance.PlayerData.level}");
                dataChanged = true;
            }
            if (GameAPI.Instance.PlayerData.kills != oldKills)
            {
                DebugLog($"Kills changed: {oldKills} -> {GameAPI.Instance.PlayerData.kills}");
                dataChanged = true;
            }
            
            if (!dataChanged)
            {
                DebugLog("No game data changes detected during collection");
            }
            
            DebugLog("Successfully collected all game data");
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error collecting game data: {ex.Message}");
        }
          // Always update last login timestamp to mark this as the latest save
        GameAPI.Instance.PlayerData.lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Final validation before save
        DebugLog($"Final data state - Health: {GameAPI.Instance.PlayerData.health}, Money: {GameAPI.Instance.PlayerData.money}, Level: {GameAPI.Instance.PlayerData.level}, Kills: {GameAPI.Instance.PlayerData.kills}");
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
    
    /// <summary>
    /// Collects current level and experience from the StatsHandler system
    /// </summary>
    private void CollectLevelAndExperienceFromGame()
    {
        try
        {
            StatsHandler playerStatsHandler = FindObjectOfType<StatsHandler>();
            if (playerStatsHandler != null)
            {
                // Try to get Level stat
                var levelStat = playerStatsHandler.GetStat("Level");
                if (levelStat != null)
                {
                    GameAPI.Instance.PlayerData.level = (int)playerStatsHandler.GetStatValue("Level");
                    DebugLog($"Collected level from StatsHandler: {GameAPI.Instance.PlayerData.level}");
                }
                
                // Try to get Experience stat
                var experienceStat = playerStatsHandler.GetStat("Experience");
                if (experienceStat != null)
                {
                    GameAPI.Instance.PlayerData.experience = (int)playerStatsHandler.GetStatValue("Experience");
                    DebugLog($"Collected experience from StatsHandler: {GameAPI.Instance.PlayerData.experience}");
                }
                
                // If no stats found, try alternative names
                if (levelStat == null)
                {
                    var altLevelStat = playerStatsHandler.GetStat("level");
                    if (altLevelStat != null)
                    {
                        GameAPI.Instance.PlayerData.level = (int)playerStatsHandler.GetStatValue("level");
                        DebugLog($"Collected level from StatsHandler (alt): {GameAPI.Instance.PlayerData.level}");
                    }
                }
                
                if (experienceStat == null)
                {
                    var altExpStat = playerStatsHandler.GetStat("experience") ?? playerStatsHandler.GetStat("exp") ?? playerStatsHandler.GetStat("XP");
                    if (altExpStat != null)
                    {
                        string statName = altExpStat.Name;
                        GameAPI.Instance.PlayerData.experience = (int)playerStatsHandler.GetStatValue(statName);
                        DebugLog($"Collected experience from StatsHandler (alt: {statName}): {GameAPI.Instance.PlayerData.experience}");
                    }
                }
            }
            else
            {
                DebugLog("No StatsHandler found for level/experience collection");
            }
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error collecting level and experience: {ex.Message}");
        }
    }
      /// <summary>
    /// Collects current zombie kill count from the ZombieKillTracker
    /// </summary>
    private void CollectZombieKillsFromGame()
    {
        try
        {
            // Get kills from ZombieKillTracker
            int killCount = ZombieKillTracker.Instance.GetKillCount();
            GameAPI.Instance.PlayerData.kills = killCount;
            DebugLog($"Collected zombie kills from ZombieKillTracker: {killCount}");
            
            // Alternative: Try to get from StatsHandler if available
            StatsHandler playerStatsHandler = FindObjectOfType<StatsHandler>();
            if (playerStatsHandler != null)
            {
                var killStat = playerStatsHandler.GetStat("Kills") ?? playerStatsHandler.GetStat("kills") ?? playerStatsHandler.GetStat("ZombieKills");
                if (killStat != null)
                {
                    int statKills = (int)playerStatsHandler.GetStatValue(killStat.Name);
                    // Use the higher value between tracker and stats
                    GameAPI.Instance.PlayerData.kills = Mathf.Max(killCount, statKills);
                    DebugLog($"Also found kills in StatsHandler: {statKills}, using max: {GameAPI.Instance.PlayerData.kills}");
                }
            }
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error collecting zombie kills: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Collects current player position for checkpoint saving
    /// </summary>
    private void CollectPlayerPositionFromGame()
    {
        try
        {
            // Find the player object
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // Update checkpoint data with current position
                if (GameAPI.Instance.PlayerData.checkpoint == null)
                {
                    GameAPI.Instance.PlayerData.checkpoint = new CheckpointData();
                }
                
                GameAPI.Instance.PlayerData.checkpoint.position = new SerializableVector3(player.transform.position);
                GameAPI.Instance.PlayerData.checkpoint.sceneId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                GameAPI.Instance.PlayerData.checkpoint.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                DebugLog($"Collected player position: {player.transform.position} in scene: {GameAPI.Instance.PlayerData.checkpoint.sceneId}");
            }
            else
            {
                DebugLog("No player object found with 'Player' tag for position collection");
            }
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error collecting player position: {ex.Message}");
        }
    }
}
