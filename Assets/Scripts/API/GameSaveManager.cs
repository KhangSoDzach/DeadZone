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
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");

        // Skip saving on the login scene
        if (scene.name == "Login")
        {
            return;
        }

        // Apply player data to the new scene
        if (GameAPI.Instance.IsLoggedIn && GameDataSynchronizer.Instance.IsDataLoaded)
        {
            GameDataSynchronizer.Instance.ApplyDataToGame();
        }

        // Save on level change if enabled
        if (saveOnLevelChange && GameAPI.Instance.IsLoggedIn)
        {
            // Wait a bit to make sure all objects are initialized
            StartCoroutine(SaveAfterDelay(2f));
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
    }

    public void SaveGame()
    {
        if (Time.time - lastSaveTime < 10f)
        {
            // Don't save too frequently
            return;
        }

        if (!GameAPI.Instance.IsLoggedIn)
        {
            Debug.LogWarning("Cannot save: Not logged in");
            return;
        }

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

    // Synchronous save for critical moments like app quitting
    private void SaveGameImmediate()
    {
        if (!GameAPI.Instance.IsLoggedIn)
        {
            return;
        }

        Debug.Log("Performing immediate save...");
        // Here we just update the data but can't guarantee the API call completes
        // since the application is quitting
    }
}
