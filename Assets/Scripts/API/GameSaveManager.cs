using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DevionGames;

public class GameSaveManager : MonoBehaviour
{
    private static GameSaveManager _instance;
    public static GameSaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameSaveManager");
                _instance = go.AddComponent<GameSaveManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    [Header("Save Settings")]
    public float autoSaveInterval = 300f; // Default: Save every 5 minutes
    public bool saveOnLevelChange = true;
    public bool saveOnExit = true;

    private bool isInitialized = false;
    private float lastSaveTime = 0f;

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
            return;
        }

        // Initialize
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        // Register scene change event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        // Register application quit event
        Application.quitting += OnApplicationQuit;

        // Start auto-save coroutine
        StartCoroutine(AutoSaveCoroutine());

        isInitialized = true;
    }

    private IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);

            if (GameAPI.Instance.IsLoggedIn)
            {
                SaveGame();
            }
        }
    }    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");

        // Skip saving on the login scene
        if (scene.name == "Login")
        {
            return;
        }

        // Load fresh player data before applying to the new scene
        if (GameAPI.Instance.IsLoggedIn)
        {
            Debug.Log("Loading fresh player data after scene change...");
            StartCoroutine(LoadFreshDataThenApply(scene.name));
        }

        // Save on level change if enabled
        if (saveOnLevelChange && GameAPI.Instance.IsLoggedIn)
        {
            // Wait a bit to make sure all objects are initialized
            StartCoroutine(SaveAfterDelay(5f)); // Increased delay to allow data loading
        }
    }
    
    private IEnumerator LoadFreshDataThenApply(string sceneName)
    {
        // Force reload fresh data from server
        bool dataLoaded = false;
        yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
            dataLoaded = success;
            if (!success)
            {
                Debug.LogWarning($"Failed to load fresh data after scene change: {error}");
            }
            else
            {
                Debug.Log("Fresh player data loaded after scene change");
            }
        }));
        
        // Apply data to the new scene whether we got fresh data or not
        if (GameDataSynchronizer.Instance != null)
        {
            // Mark data as loaded so ApplyDataToGame works
            if (dataLoaded)
            {
                GameDataSynchronizer.Instance.LoadGameData((loadSuccess, loadError) => {
                    if (loadSuccess)
                    {
                        Debug.Log("Data loaded into synchronizer, applying to game");
                        GameDataSynchronizer.Instance.ApplyDataToGame();
                    }
                });
            }
            else
            {
                // Try to apply existing data if available
                GameDataSynchronizer.Instance.ApplyDataToGame();
            }
        }
    }

    private IEnumerator SaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SaveGame();
    }

    private void OnApplicationQuit()
    {
        if (saveOnExit && GameAPI.Instance.IsLoggedIn)
        {
            SaveGameImmediate();
        }
    }    public void SaveGame()
    {
        if (Time.time - lastSaveTime < 10f)
        {
            // Don't save too frequently
            return;
        }

        // Check if properly logged in
        if (!GameAPI.Instance.IsLoggedIn)
        {
            Debug.LogWarning("Cannot save: Not logged in");
            // Try to restore auth token
            GameAPI.Instance.RestoreAuthTokenFromPrefs();
            if (!GameAPI.Instance.IsLoggedIn)
            {
                Debug.LogError("Still not logged in after token restoration attempt");
                return;
            }
        }
        
        // Ensure player data exists and is valid
        if (GameAPI.Instance.PlayerData == null || 
            string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id) || 
            string.IsNullOrEmpty(GameAPI.Instance.PlayerData.username))
        {
            Debug.LogWarning("No valid player data found, attempting to reload from server...");
            StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
                if (success && GameAPI.Instance.PlayerData != null && 
                    !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id))
                {
                    Debug.Log("Player data reloaded successfully, attempting save...");
                    SaveGame(); // Retry save
                }
                else
                {
                    Debug.LogError($"Failed to reload player data for save: {error}");
                }
            }));
            return;
        }
        
        // Check if GameDataSynchronizer exists and is loaded
        if (GameDataSynchronizer.Instance != null && !GameDataSynchronizer.Instance.IsDataLoaded)
        {
            Debug.LogWarning("GameDataSynchronizer data not loaded. Loading data first...");
            // Try to load data first, then save
            GameDataSynchronizer.Instance.LoadGameData((success, error) => {
                if (success)
                {
                    // Now try to save
                    PerformSave();
                }
                else
                {
                    Debug.LogError("Failed to load data before saving: " + error);
                    // Try to save anyway with current data
                    PerformSaveDirectly();
                }
            });
            return;
        }

        PerformSave();
    }
    
    private IEnumerator LoadDataThenSave()
    {
        yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
            if (success)
            {
                Debug.Log("Player data loaded successfully, proceeding with save");
                PerformSave();
            }
            else
            {
                Debug.LogError("Failed to load player data before saving: " + error);
            }
        }));
    }
    
    private void PerformSave()
    {
        if (GameDataSynchronizer.Instance != null)
        {
            lastSaveTime = Time.time;
            GameDataSynchronizer.Instance.SaveGameData((success, message) =>
            {
                if (success)
                {
                    Debug.Log("Game saved successfully");
                }
                else
                {
                    Debug.LogError("Failed to save game: " + message);
                }
            });
        }
        else
        {
            // Fallback to direct save
            PerformSaveDirectly();
        }
    }
    
    private void PerformSaveDirectly()
    {
        // Đồng bộ trạng thái chìa khóa trước khi save
        if (InventoryForKey.Instance != null && GameAPI.Instance.PlayerData != null)
        {
            GameAPI.Instance.PlayerData.hasKey = InventoryForKey.Instance.hasKey;
        }
        lastSaveTime = Time.time;
        StartCoroutine(GameAPI.Instance.SavePlayerData((success, message) =>
        {
            if (success)
            {
                Debug.Log("Game saved successfully (direct)");
            }
            else
            {
                Debug.LogError("Failed to save game (direct): " + message);
            }
        }));
    }

    // Synchronous save for critical moments like app quitting
    private void SaveGameImmediate()
    {
        if (!GameAPI.Instance.IsLoggedIn || !GameDataSynchronizer.Instance.IsDataLoaded)
        {
            Debug.LogWarning("Cannot perform immediate save: Not logged in or no data loaded");
            return;
        }

        Debug.Log("Performing immediate save...");
        // Collect current game state before app quits
        GameDataSynchronizer.Instance.SaveGameData(null);
    }
}
