using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Controller for Endless Mode - manages display elements
/// </summary>
public class EndlessUIController : MonoBehaviour
{
    [Header("Endless Mode UI")]
    [SerializeField] private GameObject endlessUIPanel;
    [SerializeField] private TMP_Text survivalTimeText;
    [SerializeField] private TMP_Text difficultyLevelText;
    [SerializeField] private TMP_Text zombieCountText;
    [SerializeField] private Slider difficultyProgressSlider;
    
    [Header("Notifications")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float notificationDuration = 3f;
    
    [Header("Stats Display")]
    [SerializeField] private TMP_Text killCountText;
    [SerializeField] private TMP_Text coinsEarnedText;
    
    private EndlessManager endlessManager;
    private int killCount = 0;
    private int coinsEarned = 0;
    
    private void Start()
    {
        // Find EndlessManager
        endlessManager = FindObjectOfType<EndlessManager>();
        
        // Initialize UI
        if (endlessUIPanel != null)
        {
            endlessUIPanel.SetActive(true);
        }
        
        // Hide notification panel initially
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        
        // Subscribe to events if EndlessGameEvents exists
        try
        {
            var eventType = System.Type.GetType("EndlessGameEvents");
            if (eventType != null)
            {
                var zombieKilledEvent = eventType.GetEvent("OnZombieKilled");
                var coinsDroppedEvent = eventType.GetEvent("OnCoinsDropped");
                var difficultyIncreasedEvent = eventType.GetEvent("OnDifficultyIncreased");
                
                if (zombieKilledEvent != null)
                {
                    var addKillDelegate = System.Delegate.CreateDelegate(zombieKilledEvent.EventHandlerType, this, "AddKill");
                    zombieKilledEvent.AddEventHandler(null, addKillDelegate);
                }
                
                if (coinsDroppedEvent != null)
                {
                    var addCoinsDelegate = System.Delegate.CreateDelegate(coinsDroppedEvent.EventHandlerType, this, "AddCoins");
                    coinsDroppedEvent.AddEventHandler(null, addCoinsDelegate);
                }
            }
        }
        catch (System.Exception)
        {
            Debug.Log("EndlessGameEvents not found, using direct method calls");
        }
        
        Debug.Log("EndlessUIController initialized");
    }
    
    private void Update()
    {
        UpdateUI();
    }
    
    /// <summary>
    /// Update all UI elements
    /// </summary>
    private void UpdateUI()
    {
        if (endlessManager == null) return;
        
        // Update survival time
        if (survivalTimeText != null)
        {
            float survivalTime = endlessManager.GetSurvivalTime();
            int minutes = Mathf.FloorToInt(survivalTime / 60f);
            int seconds = Mathf.FloorToInt(survivalTime % 60f);
            survivalTimeText.text = $"{minutes:00}:{seconds:00}";
        }
        
        // Update difficulty level
        if (difficultyLevelText != null)
        {
            int level = endlessManager.GetDifficultyLevel();
            float multiplier = endlessManager.GetCurrentDifficultyMultiplier();
            difficultyLevelText.text = $"Level {level} (x{multiplier:F1})";
        }
        
        // Update zombie count
        if (zombieCountText != null)
        {
            int zombieCount = GameObject.FindGameObjectsWithTag("Zombie").Length;
            zombieCountText.text = $"Zombies: {zombieCount}";
        }
        
        // Update difficulty progress (time until next difficulty increase)
        if (difficultyProgressSlider != null)
        {
            float survivalTime = endlessManager.GetSurvivalTime();
            float progress = (survivalTime % 60f) / 60f; // 60 seconds interval
            difficultyProgressSlider.value = progress;
        }
        
        // Update stats
        if (killCountText != null)
        {
            killCountText.text = $"Kills: {killCount}";
        }
        
        if (coinsEarnedText != null)
        {
            coinsEarnedText.text = $"Coins: {coinsEarned}";
        }
    }
    
    /// <summary>
    /// Show notification message
    /// </summary>
    public void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            StartCoroutine(ShowNotificationCoroutine());
        }
    }
    
    /// <summary>
    /// Show notification for difficulty increase
    /// </summary>
    public void ShowDifficultyIncreaseNotification(int level, float multiplier)
    {
        string message = $"DIFFICULTY INCREASED!\nLevel {level}\nZombies x{multiplier:F1} stronger\nMore coins dropped!";
        ShowNotification(message);
    }
    
    /// <summary>
    /// Coroutine to display notification
    /// </summary>
    private IEnumerator ShowNotificationCoroutine()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
            yield return new WaitForSeconds(notificationDuration);
            notificationPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Increment kill count
    /// </summary>
    public void AddKill()
    {
        killCount++;
    }
    
    /// <summary>
    /// Add coins earned
    /// </summary>
    public void AddCoins(int amount)
    {
        coinsEarned += amount;
    }
    
    /// <summary>
    /// Reset stats (for new game)
    /// </summary>
    public void ResetStats()
    {
        killCount = 0;
        coinsEarned = 0;
    }
    
    /// <summary>
    /// Get current kill count
    /// </summary>
    public int GetKillCount()
    {
        return killCount;
    }
    
    /// <summary>
    /// Get current coins earned
    /// </summary>
    public int GetCoinsEarned()
    {
        return coinsEarned;
    }
}