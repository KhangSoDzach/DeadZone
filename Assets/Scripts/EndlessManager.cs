using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class EndlessManager : MonoBehaviour
{
    [Header("Endless Mode Settings")]
    [SerializeField] private bool isEndlessMode = true;
    [SerializeField] private float difficultyIncreaseInterval = 60f; // Tăng độ khó mỗi 60 giây
    [SerializeField] private float initialDifficultyMultiplier = 1.0f;
    [SerializeField] private float difficultyIncreaseRate = 0.3f; // Mỗi lần tăng 0.3
    [SerializeField] private float maxDifficultyMultiplier = 5.0f;
    
    [Header("Zombie Spawning")]
    [SerializeField] private GameObject[] zombiePrefabs; // Các loại zombie để spawn
    [SerializeField] private Transform[] spawnPoints; // Điểm spawn zombie
    [SerializeField] private float spawnInterval = 5f; // Khoảng cách giữa các lần spawn
    [SerializeField] private int maxZombiesAtOnce = 15; // Tối đa zombie cùng lúc
    [SerializeField] private float spawnRangeFromPlayer = 30f; // Tầm spawn xung quanh player
    
    [Header("Vision Enhancement")]
    [SerializeField] private float visionIncreaseDistance = 40f; // Khoảng cách để tăng tầm nhìn
    [SerializeField] private float enhancedVisionRadius = 50f; // Tầm nhìn tăng cường
    [SerializeField] private float endlessModeBaseVision = 35f; // Tầm nhìn cơ bản trong Endless Mode
    [SerializeField] private bool forceMaxVisionForAll = true; // Buộc tất cả zombie có tầm nhìn tối đa
    
    [Header("UI Elements")]
    [SerializeField] private GameObject difficultyNotificationPanel;
    [SerializeField] private TMP_Text difficultyNotificationText;
    [SerializeField] private TMP_Text survivalTimeText;
    [SerializeField] private TMP_Text difficultyLevelText;
    [SerializeField] private float notificationDisplayTime = 3f;
    

    private float currentDifficultyMultiplier;
    private float gameStartTime;
    private float lastDifficultyIncrease;
    private int currentDifficultyLevel;
    private Transform playerTransform;
    private List<GameObject> activeZombies = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private Coroutine difficultyCoroutine;
    
    public static EndlessManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (!isEndlessMode) return;
        
        InitializeEndlessMode();
    }
    
    private void InitializeEndlessMode()
    {
        Debug.Log("Initializing Endless Mode...");
        

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player not found! Endless Mode may not work properly.");
        }
        
        // Initialize values
        currentDifficultyMultiplier = initialDifficultyMultiplier;
        gameStartTime = Time.time;
        lastDifficultyIncrease = Time.time;
        currentDifficultyLevel = 1;

        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(ContinuousZombieSpawning());
        }

        if (difficultyCoroutine == null)
        {
            difficultyCoroutine = StartCoroutine(DifficultyScaling());
        }


        StartCoroutine(InitialVisionEnhancement());


        UpdateUI();

        Debug.Log("Endless Mode initialized successfully!");
    }
    
    private void Update()
    {
        if (!isEndlessMode) return;
        
        UpdateUI();
        CleanupDeadZombies();
        CheckAndEnhanceZombieVision();
        
        DebugZombieVision(); 
    }
    private IEnumerator ContinuousZombieSpawning()
    {
        while (isEndlessMode)
        {
            // Check if we should spawn more zombies
            if (activeZombies.Count < maxZombiesAtOnce && playerTransform != null)
            {
                SpawnZombie();
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    private IEnumerator DifficultyScaling()
    {
        while (isEndlessMode)
        {
            yield return new WaitForSeconds(difficultyIncreaseInterval);
            
            if (currentDifficultyMultiplier < maxDifficultyMultiplier)
            {
                IncreaseDifficulty();
            }
        }
    }
    

    private void SpawnZombie()
    {
        if (zombiePrefabs.Length == 0 || playerTransform == null) return;
        

        GameObject zombiePrefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        

        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        // Spawn zombie
        GameObject zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        

        ApplyDifficultyToZombie(zombie);
        

        activeZombies.Add(zombie);
        
        Debug.Log($"Spawned {zombie.name} at difficulty level {currentDifficultyLevel}");
    }
    

    private Vector3 GetRandomSpawnPosition()
    {
        if (playerTransform == null) return Vector3.zero;
        

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return spawnPoint.position;
        }
        
 
        Vector2 randomCircle = Random.insideUnitCircle * spawnRangeFromPlayer;
        Vector3 spawnPosition = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
        {
            spawnPosition.y = hit.point.y;
        }
        
        return spawnPosition;
    }
    

    private void ApplyDifficultyToZombie(GameObject zombie)
    {

        var zombie1 = zombie.GetComponent<Zombie_1>();
        var zombie2 = zombie.GetComponent<Zombie_2>();
        var zombie3 = zombie.GetComponent<Zombie_3>();
        var zombie4 = zombie.GetComponent<Zombie_4>();
        var zombieMiniboss = zombie.GetComponent<ZombieMiniboss>();
        var boss = zombie.GetComponent<Boss>();
        

        if (zombie1 != null)
        {
            ApplyEndlessScaling(zombie1, currentDifficultyMultiplier);
            SetZombieVision(zombie1, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        else if (zombie2 != null)
        {
            ApplyEndlessScaling(zombie2, currentDifficultyMultiplier);
            SetZombieVision(zombie2, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        else if (zombie3 != null)
        {
            ApplyEndlessScaling(zombie3, currentDifficultyMultiplier);
            SetZombieVision(zombie3, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        else if (zombie4 != null)
        {
            ApplyEndlessScaling(zombie4, currentDifficultyMultiplier);
            SetZombieVision(zombie4, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        else if (zombieMiniboss != null)
        {
            ApplyEndlessScaling(zombieMiniboss, currentDifficultyMultiplier);
            SetZombieVision(zombieMiniboss, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        else if (boss != null)
        {
            ApplyEndlessScaling(boss, currentDifficultyMultiplier);
            SetZombieVision(boss, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        
        Debug.Log($"Applied Endless Mode scaling and enhanced vision to {zombie.name}");
    }
    

    private void ApplyEndlessScaling(MonoBehaviour zombie, float multiplier)
    {
        // Use reflection to access fields and modify them
        var type = zombie.GetType();
        
        // Scale health - try both public and private fields
        var healthField = type.GetField("zombieHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (healthField != null)
        {
            float currentHealth = (float)healthField.GetValue(zombie);
            healthField.SetValue(zombie, currentHealth * multiplier);
            Debug.Log($"Scaled {zombie.name} health from {currentHealth} to {currentHealth * multiplier}");
        }
        
        // Scale attack damage
        var damageField = type.GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (damageField != null)
        {
            float currentDamage = (float)damageField.GetValue(zombie);
            damageField.SetValue(zombie, currentDamage * multiplier);
            Debug.Log($"Scaled {zombie.name} damage from {currentDamage} to {currentDamage * multiplier}");
        }
        
        // Scale money drop (increase coin drop rate and amount)
        var dropChanceField = type.GetField("dropChance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (dropChanceField != null)
        {
            float currentDropChance = (float)dropChanceField.GetValue(zombie);
            float newDropChance = Mathf.Min(currentDropChance * multiplier, 1f);
            dropChanceField.SetValue(zombie, newDropChance);
            Debug.Log($"Scaled {zombie.name} drop chance from {currentDropChance} to {newDropChance}");
        }
        
        var minCoinsField = type.GetField("minCoinsDropped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (minCoinsField != null)
        {
            int currentMinCoins = (int)minCoinsField.GetValue(zombie);
            int newMinCoins = Mathf.RoundToInt(currentMinCoins * multiplier);
            minCoinsField.SetValue(zombie, newMinCoins);
            Debug.Log($"Scaled {zombie.name} min coins from {currentMinCoins} to {newMinCoins}");
        }
        
        var maxCoinsField = type.GetField("maxCoinsDropped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (maxCoinsField != null)
        {
            int currentMaxCoins = (int)maxCoinsField.GetValue(zombie);
            int newMaxCoins = Mathf.RoundToInt(currentMaxCoins * multiplier);
            maxCoinsField.SetValue(zombie, newMaxCoins);
            Debug.Log($"Scaled {zombie.name} max coins from {currentMaxCoins} to {newMaxCoins}");
        }
        
        // Update remainHeath to match new zombieHealth
        var remainHealthField = type.GetField("remainHeath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (remainHealthField != null && healthField != null)
        {
            remainHealthField.SetValue(zombie, healthField.GetValue(zombie));
        }
    }

    private void IncreaseDifficulty()
    {
        currentDifficultyLevel++;
        currentDifficultyMultiplier += difficultyIncreaseRate;
        lastDifficultyIncrease = Time.time;
        
        // Show notification
        ShowDifficultyNotification();
        
        // Notify UI Controller
        var endlessUI = FindObjectOfType<EndlessUIController>();
        if (endlessUI != null)
        {
            endlessUI.ShowDifficultyIncreaseNotification(currentDifficultyLevel, currentDifficultyMultiplier);
        }
        
        Debug.Log($"Difficulty increased! Level: {currentDifficultyLevel}, Multiplier: {currentDifficultyMultiplier:F1}");
    }

    private void ShowDifficultyNotification()
    {
        if (difficultyNotificationPanel != null && difficultyNotificationText != null)
        {
            difficultyNotificationText.text = $"Difficulty Increased!\nLevel {currentDifficultyLevel}\nZombies are stronger and drop more coins!";
            
            StartCoroutine(ShowNotificationCoroutine());
        }
    }
    

    private IEnumerator ShowNotificationCoroutine()
    {
        if (difficultyNotificationPanel != null)
        {
            difficultyNotificationPanel.SetActive(true);
            yield return new WaitForSeconds(notificationDisplayTime);
            difficultyNotificationPanel.SetActive(false);
        }
    }
    

    private void UpdateUI()
    {
        // Update survival time
        if (survivalTimeText != null)
        {
            float survivalTime = Time.time - gameStartTime;
            int minutes = Mathf.FloorToInt(survivalTime / 60f);
            int seconds = Mathf.FloorToInt(survivalTime % 60f);
            survivalTimeText.text = $"Survival Time: {minutes:00}:{seconds:00}";
        }
        
        // Update difficulty level
        if (difficultyLevelText != null)
        {
            difficultyLevelText.text = $"Difficulty Level: {currentDifficultyLevel} (x{currentDifficultyMultiplier:F1})";
        }
    }
    
    private void CleanupDeadZombies()
    {
        for (int i = activeZombies.Count - 1; i >= 0; i--)
        {
            if (activeZombies[i] == null)
            {
                activeZombies.RemoveAt(i);
            }
        }
    }
    
    private void CheckAndEnhanceZombieVision()
    {
        if (playerTransform == null) return;
        
        // If forceMaxVisionForAll is enabled, ensure all zombies always have max vision
        if (forceMaxVisionForAll)
        {
            foreach (GameObject zombie in activeZombies)
            {
                if (zombie == null) continue;
                EnhanceZombieVisionToMax(zombie);
            }
            return;
        }
        
        // Otherwise, only enhance vision for distant zombies
        foreach (GameObject zombie in activeZombies)
        {
            if (zombie == null) continue;
            
            float distanceToPlayer = Vector3.Distance(zombie.transform.position, playerTransform.position);
            
            // If zombie is too far, enhance its vision
            if (distanceToPlayer > visionIncreaseDistance)
            {
                EnhanceZombieVision(zombie);
            }
        }
    }
    

    private void EnhanceZombieVisionToMax(GameObject zombie)
    {
        var zombie1 = zombie.GetComponent<Zombie_1>();
        var zombie2 = zombie.GetComponent<Zombie_2>();
        var zombie3 = zombie.GetComponent<Zombie_3>();
        var zombie4 = zombie.GetComponent<Zombie_4>();
        var zombieMiniboss = zombie.GetComponent<ZombieMiniboss>();
        var boss = zombie.GetComponent<Boss>();
        
        // Set maximum vision for all zombie types
        if (zombie1 != null)
        {
            SetZombieVision(zombie1, enhancedVisionRadius);
        }
        else if (zombie2 != null)
        {
            SetZombieVision(zombie2, enhancedVisionRadius);
        }
        else if (zombie3 != null)
        {
            SetZombieVision(zombie3, enhancedVisionRadius);
        }
        else if (zombie4 != null)
        {
            SetZombieVision(zombie4, enhancedVisionRadius);
        }
        else if (zombieMiniboss != null)
        {
            SetZombieVision(zombieMiniboss, enhancedVisionRadius);
        }
        else if (boss != null)
        {
            SetZombieVision(boss, enhancedVisionRadius);
        }
    }
    

    private void EnhanceZombieVision(GameObject zombie)
    {
        var zombie1 = zombie.GetComponent<Zombie_1>();
        var zombie2 = zombie.GetComponent<Zombie_2>();
        var zombie3 = zombie.GetComponent<Zombie_3>();
        var zombie4 = zombie.GetComponent<Zombie_4>();
        var zombieMiniboss = zombie.GetComponent<ZombieMiniboss>();
        var boss = zombie.GetComponent<Boss>();
        
        // Enhance observation radius for different zombie types
        if (zombie1 != null)
        {
            SetZombieVision(zombie1, enhancedVisionRadius);
        }
        else if (zombie2 != null)
        {
            SetZombieVision(zombie2, enhancedVisionRadius);
        }
        else if (zombie3 != null)
        {
            SetZombieVision(zombie3, enhancedVisionRadius);
        }
        else if (zombie4 != null)
        {
            SetZombieVision(zombie4, enhancedVisionRadius);
        }
        else if (zombieMiniboss != null)
        {
            SetZombieVision(zombieMiniboss, enhancedVisionRadius);
        }
        else if (boss != null)
        {
            SetZombieVision(boss, enhancedVisionRadius);
        }
    }
    

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DebugZombieVision()
    {
        if (Input.GetKeyDown(KeyCode.V)) // Press V to debug vision
        {
            Debug.Log("=== ZOMBIE VISION DEBUG ===");
            Debug.Log($"Force Max Vision: {forceMaxVisionForAll}");
            Debug.Log($"Base Vision: {endlessModeBaseVision}");
            Debug.Log($"Enhanced Vision: {enhancedVisionRadius}");
            
            var allZombies = FindObjectsOfType<MonoBehaviour>().Where(mb => 
                mb is Zombie_1 || mb is Zombie_2 || mb is Zombie_3 || 
                mb is Zombie_4 || mb is ZombieMiniboss || mb is Boss).ToArray();
            
            foreach (var zombie in allZombies)
            {
                var type = zombie.GetType();
                var observationField = type.GetField("observationRadius", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (observationField != null)
                {
                    float currentVision = (float)observationField.GetValue(zombie);
                    Debug.Log($"{zombie.name}: Vision = {currentVision}");
                }
            }
            Debug.Log("=== END DEBUG ===");
        }
    }


    private void SetZombieVision(MonoBehaviour zombie, float radius)
    {
        var type = zombie.GetType();
        var observationField = type.GetField("observationRadius", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (observationField != null)
        {
            observationField.SetValue(zombie, radius);
        }
    }
    

    private IEnumerator InitialVisionEnhancement()
    {
        yield return new WaitForSeconds(0.5f); // Wait a bit for zombies to initialize
        
        Debug.Log("Enhancing vision for all existing zombies in Endless Mode...");
        
        // Find all existing zombies in the scene
        EnhanceAllZombiesVision();
        
        // Continue enhancing vision for newly spawned zombies
        yield return null;
    }

    private void EnhanceAllZombiesVision()
    {
        // Find all zombie types in scene
        var allZombie1 = FindObjectsOfType<Zombie_1>();
        var allZombie2 = FindObjectsOfType<Zombie_2>();
        var allZombie3 = FindObjectsOfType<Zombie_3>();
        var allZombie4 = FindObjectsOfType<Zombie_4>();
        var allMiniboss = FindObjectsOfType<ZombieMiniboss>();
        var allBoss = FindObjectsOfType<Boss>();
        
        // Set enhanced vision for all zombie types
        foreach (var zombie in allZombie1)
        {
            SetZombieVision(zombie, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        
        foreach (var zombie in allZombie2)
        {
            SetZombieVision(zombie, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        
        foreach (var zombie in allZombie3)
        {
            SetZombieVision(zombie, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        
        foreach (var zombie in allZombie4)
        {
            SetZombieVision(zombie, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        
        foreach (var zombie in allMiniboss)
        {
            SetZombieVision(zombie, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        
        foreach (var zombie in allBoss)
        {
            SetZombieVision(zombie, forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision);
        }
        
        Debug.Log($"Enhanced vision applied to all zombies. Vision radius: {(forceMaxVisionForAll ? enhancedVisionRadius : endlessModeBaseVision)}");
    }

    public float GetCurrentDifficultyMultiplier()
    {
        return currentDifficultyMultiplier;
    }

    public float GetSurvivalTime()
    {
        return Time.time - gameStartTime;
    }
    

    public int GetDifficultyLevel()
    {
        return currentDifficultyLevel;
    }
    


    public void StopEndlessMode()
    {
        isEndlessMode = false;
        
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        if (difficultyCoroutine != null)
        {
            StopCoroutine(difficultyCoroutine);
            difficultyCoroutine = null;
        }
        
        Debug.Log("Endless Mode stopped");
    }
    
    private void OnDestroy()
    {
        StopEndlessMode();
    }
}
