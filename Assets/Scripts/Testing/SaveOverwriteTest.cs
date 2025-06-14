using System.Collections;
using UnityEngine;
using Scripts.API;

/// <summary>
/// Test script to verify save game overwrite functionality
/// Add this script to a GameObject in your game scene to test save functionality
/// </summary>
public class SaveOverwriteTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float testInterval = 30f; // Test save every 30 seconds
    
    [Header("Test Data")]
    [SerializeField] private int testHealth = 75;
    [SerializeField] private int testMoney = 1500;
    [SerializeField] private int testLevel = 5;
    [SerializeField] private int testKills = 25;
    
    private void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[SaveOverwriteTest] Test script initialized");
        }
        
        // Start periodic save testing
        StartCoroutine(PeriodicSaveTest());
    }
    
    private IEnumerator PeriodicSaveTest()
    {
        while (true)
        {
            yield return new WaitForSeconds(testInterval);
            
            if (GameAPI.Instance != null && GameAPI.Instance.IsLoggedIn)
            {
                TestSaveOverwrite();
            }
            else
            {
                Debug.LogWarning("[SaveOverwriteTest] Cannot test - not logged in");
            }
        }
    }
    
    /// <summary>
    /// Test the save overwrite functionality by modifying data and saving
    /// </summary>
    [ContextMenu("Test Save Overwrite")]
    public void TestSaveOverwrite()
    {
        if (GameAPI.Instance == null || !GameAPI.Instance.IsLoggedIn)
        {
            Debug.LogError("[SaveOverwriteTest] Cannot test save - not logged in");
            return;
        }
        
        if (GameAPI.Instance.PlayerData == null)
        {
            Debug.LogError("[SaveOverwriteTest] Cannot test save - no player data");
            return;
        }
        
        Debug.Log("[SaveOverwriteTest] ========== STARTING SAVE OVERWRITE TEST ==========");
        
        // Store original values
        var originalData = new {
            health = GameAPI.Instance.PlayerData.health,
            money = GameAPI.Instance.PlayerData.money,
            level = GameAPI.Instance.PlayerData.level,
            kills = GameAPI.Instance.PlayerData.kills
        };
        
        Debug.Log($"[SaveOverwriteTest] Original data - Health: {originalData.health}, Money: {originalData.money}, Level: {originalData.level}, Kills: {originalData.kills}");
        
        // Modify player data with test values
        GameAPI.Instance.PlayerData.health = testHealth;
        GameAPI.Instance.PlayerData.money = testMoney;
        GameAPI.Instance.PlayerData.level = testLevel;
        GameAPI.Instance.PlayerData.kills = testKills;
        
        Debug.Log($"[SaveOverwriteTest] Modified data - Health: {testHealth}, Money: {testMoney}, Level: {testLevel}, Kills: {testKills}");
        
        // Perform save operation
        StartCoroutine(GameAPI.Instance.SavePlayerData((success, error) =>
        {
            if (success)
            {
                Debug.Log("[SaveOverwriteTest] ✅ Save operation completed successfully");
                
                // Verify the save by reloading data from server
                StartCoroutine(VerifySaveOverwrite());
            }
            else
            {
                Debug.LogError($"[SaveOverwriteTest] ❌ Save operation failed: {error}");
            }
        }));
    }
    
    private IEnumerator VerifySaveOverwrite()
    {
        Debug.Log("[SaveOverwriteTest] Verifying save by reloading data from server...");
        
        // Wait a moment for server processing
        yield return new WaitForSeconds(2f);
        
        // Reload data from server
        yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) =>
        {
            if (success && GameAPI.Instance.PlayerData != null)
            {
                Debug.Log("[SaveOverwriteTest] Data reloaded from server successfully");
                
                // Check if data matches what we saved
                bool healthMatch = GameAPI.Instance.PlayerData.health == testHealth;
                bool moneyMatch = GameAPI.Instance.PlayerData.money == testMoney;
                bool levelMatch = GameAPI.Instance.PlayerData.level == testLevel;
                bool killsMatch = GameAPI.Instance.PlayerData.kills == testKills;
                
                Debug.Log($"[SaveOverwriteTest] Verification results:");
                Debug.Log($"  Health: Expected {testHealth}, Got {GameAPI.Instance.PlayerData.health} - {(healthMatch ? "✅ MATCH" : "❌ MISMATCH")}");
                Debug.Log($"  Money: Expected {testMoney}, Got {GameAPI.Instance.PlayerData.money} - {(moneyMatch ? "✅ MATCH" : "❌ MISMATCH")}");
                Debug.Log($"  Level: Expected {testLevel}, Got {GameAPI.Instance.PlayerData.level} - {(levelMatch ? "✅ MATCH" : "❌ MISMATCH")}");
                Debug.Log($"  Kills: Expected {testKills}, Got {GameAPI.Instance.PlayerData.kills} - {(killsMatch ? "✅ MATCH" : "❌ MISMATCH")}");
                
                if (healthMatch && moneyMatch && levelMatch && killsMatch)
                {
                    Debug.Log("[SaveOverwriteTest] ========== ✅ SAVE OVERWRITE TEST PASSED ========== ");
                }
                else
                {
                    Debug.LogError("[SaveOverwriteTest] ========== ❌ SAVE OVERWRITE TEST FAILED ========== ");
                    Debug.LogError("[SaveOverwriteTest] Server did not properly overwrite the player data!");
                }
            }
            else
            {
                Debug.LogError($"[SaveOverwriteTest] Failed to reload data for verification: {error}");
            }
        }));
    }
    
    /// <summary>
    /// Test manual data collection and save
    /// </summary>
    [ContextMenu("Test Data Collection and Save")]
    public void TestDataCollectionAndSave()
    {
        if (GameDataSynchronizer.Instance != null)
        {
            Debug.Log("[SaveOverwriteTest] Testing data collection and save...");
            
            GameDataSynchronizer.Instance.SaveGameData((success, error) =>
            {
                if (success)
                {
                    Debug.Log("[SaveOverwriteTest] ✅ Data collection and save completed successfully");
                }
                else
                {
                    Debug.LogError($"[SaveOverwriteTest] ❌ Data collection and save failed: {error}");
                }
            });
        }
        else
        {
            Debug.LogError("[SaveOverwriteTest] GameDataSynchronizer instance not found");
        }
    }
    
    /// <summary>
    /// Force update test values to current game objects
    /// </summary>
    [ContextMenu("Update Test Values From Game")]
    public void UpdateTestValuesFromGame()
    {
        // Try to get current values from game systems
        if (ScoreManager.Instance != null)
        {
            testMoney = ScoreManager.Score;
            Debug.Log($"[SaveOverwriteTest] Updated test money from ScoreManager: {testMoney}");
        }
        
        // Try to get health from stats system
        var statsHandler = FindObjectOfType<DevionGames.StatSystem.StatsHandler>();
        if (statsHandler != null)
        {
            var healthStat = statsHandler.GetStat("Health");
            if (healthStat is DevionGames.StatSystem.Attribute healthAttribute)
            {
                testHealth = (int)healthAttribute.CurrentValue;
                Debug.Log($"[SaveOverwriteTest] Updated test health from StatsHandler: {testHealth}");
            }
        }
        
        // Try to get kills from kill tracker
        if (ZombieKillTracker.Instance != null)
        {
            testKills = ZombieKillTracker.Instance.GetKillCount();
            Debug.Log($"[SaveOverwriteTest] Updated test kills from ZombieKillTracker: {testKills}");
        }
        
        Debug.Log($"[SaveOverwriteTest] Current test values - Health: {testHealth}, Money: {testMoney}, Level: {testLevel}, Kills: {testKills}");
    }
}
