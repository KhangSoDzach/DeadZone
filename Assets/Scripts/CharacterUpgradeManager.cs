using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterUpgradeManager : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject characterUpgradePanel;
    
    [Header("Character Upgrade UI")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI currentMoneyText;
    
    [Header("Health Upgrade")]
    public Button healthButton;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI healthCostText;
    public float healthUpgradeAmount = 10f;
    public int healthBaseCost = 300;
    public float healthCostMultiplier = 1.5f;
    
    [Header("Sprint Time Upgrade")]
    public Button sprintTimeButton;
    public TextMeshProUGUI sprintTimeText;
    public TextMeshProUGUI sprintTimeCostText;
    public float sprintTimeUpgradeAmount = 5f;
    public int sprintTimeBaseCost = 250;
    public float sprintTimeCostMultiplier = 1.5f;
    
    [Header("Walk Speed Upgrade")]
    public Button walkSpeedButton;
    public TextMeshProUGUI walkSpeedText;
    public TextMeshProUGUI walkSpeedCostText;
    public float walkSpeedUpgradeAmount = 0.5f;
    public int walkSpeedBaseCost = 400;
    public float walkSpeedCostMultiplier = 1.75f;
    
    [Header("Run Speed Upgrade")]
    public Button runSpeedButton;
    public TextMeshProUGUI runSpeedText;
    public TextMeshProUGUI runSpeedCostText;
    public float runSpeedUpgradeAmount = 0.5f;
    public int runSpeedBaseCost = 500;
    public float runSpeedCostMultiplier = 2f;
    
    [Header("Navigation Buttons")]
    public Button backToShopButton;
    
    // Reference to the shop management for returning to shop
    private ShopManagement shopManager;
    
    // References to player components
    private HealthManager playerHealthManager;
    private PlayerMovement playerMovement;
    
    // Current player money
    private int currentMoney;
      private void Start()
    {
        // Check if the panel is assigned
        if (characterUpgradePanel == null)
        {
            Debug.LogError("CharacterUpgradeManager: characterUpgradePanel is not assigned in inspector!");
            
            // Try to find it by name as fallback
            characterUpgradePanel = GameObject.Find("CharacterUpgradePanel");
            if (characterUpgradePanel != null)
                Debug.Log("CharacterUpgradeManager: Found panel by name: " + characterUpgradePanel.name);
        }
        
        // Initially hide the panel
        if (characterUpgradePanel)
        {
            characterUpgradePanel.SetActive(false);
            Debug.Log("CharacterUpgradeManager: Panel hidden at start");
        }
        else
        {
            Debug.LogError("CharacterUpgradeManager: Failed to find characterUpgradePanel!");
        }
            
        // Add listeners to upgrade buttons
        if (healthButton)
            healthButton.onClick.AddListener(() => UpgradeHealth());
        else
            Debug.LogWarning("CharacterUpgradeManager: healthButton not assigned");
            
        if (sprintTimeButton)
            sprintTimeButton.onClick.AddListener(() => UpgradeSprintTime());
        else
            Debug.LogWarning("CharacterUpgradeManager: sprintTimeButton not assigned");
            
        if (walkSpeedButton)
            walkSpeedButton.onClick.AddListener(() => UpgradeWalkSpeed());
        else
            Debug.LogWarning("CharacterUpgradeManager: walkSpeedButton not assigned");
            
        if (runSpeedButton)
            runSpeedButton.onClick.AddListener(() => UpgradeRunSpeed());
        else
            Debug.LogWarning("CharacterUpgradeManager: runSpeedButton not assigned");
            
        if (backToShopButton)
            backToShopButton.onClick.AddListener(BackToShop);
        else
            Debug.LogWarning("CharacterUpgradeManager: backToShopButton not assigned");
    }    // Show the character upgrade panel
    public void ShowCharacterUpgradePanel(int playerMoney, ShopManagement shop)
    {
        Debug.Log("CharacterUpgradeManager: ShowCharacterUpgradePanel called");
        currentMoney = playerMoney;
        shopManager = shop;
        
        // Find player references
        FindPlayerReferences();
        
        if (playerHealthManager == null || playerMovement == null)
        {
            Debug.LogWarning("Player components not found. Cannot show upgrade panel.");
            if (shopManager != null)
            {
                shopManager.ShowNotification("Character upgrade unavailable!");
            }
            return;
        }
          if (characterUpgradePanel)
        {
            // Force shop panels to deactivate first - this is critical
            if (shopManager != null)
            {
                // Force deactivate any panels from the ShopManagement
                GameObject mainShopPanel = GameObject.Find("MainShopPanel");
                if (mainShopPanel != null)
                {
                    mainShopPanel.SetActive(false);
                    Debug.Log("CharacterUpgradeManager: Force deactivated MainShopPanel");
                }
                
                GameObject gunShopPanel = GameObject.Find("GunShopPanel");
                if (gunShopPanel != null)
                {
                    gunShopPanel.SetActive(false);
                    Debug.Log("CharacterUpgradeManager: Force deactivated GunShopPanel");
                }
            }
            
            // Make sure to hide the weapon type selection panel as well
            WeaponUpgradeManager weaponManager = FindObjectOfType<WeaponUpgradeManager>();
            if (weaponManager != null)
            {
                if (weaponManager.weaponTypeSelectionPanel != null)
                {
                    weaponManager.weaponTypeSelectionPanel.SetActive(false);
                    Debug.Log("CharacterUpgradeManager: Force deactivated Weapon Type Selection Panel");
                }
                
                if (weaponManager.weaponUpgradePanel != null)
                {
                    weaponManager.weaponUpgradePanel.SetActive(false);
                    Debug.Log("CharacterUpgradeManager: Force deactivated Weapon Upgrade Panel");
                }
            }
            
            // Ensure character upgrade panel is active
            characterUpgradePanel.SetActive(true);
            Debug.Log("CharacterUpgradeManager: Panel activated: " + characterUpgradePanel.name);
            
            // Update character name and player money
            if (characterNameText)
                characterNameText.text = "Character Upgrades";
                
            UpdateMoneyDisplay();
            
            // Update all upgrade displays
            UpdateHealthUI();
            UpdateSprintTimeUI();
            UpdateWalkSpeedUI();
            UpdateRunSpeedUI();
        }
        else
        {
            Debug.LogError("CharacterUpgradeManager: characterUpgradePanel is null!");
        }
    }
      // Find required player components
    private void FindPlayerReferences()
    {
        Debug.Log("CharacterUpgradeManager: Finding player references");
        
        // Find player components in scene
        GameObject player = GameObject.FindWithTag("Player");
        
        if (player != null)
        {
            Debug.Log("CharacterUpgradeManager: Player found with tag: " + player.name);
            
            playerHealthManager = player.GetComponent<HealthManager>();
            if (playerHealthManager == null)
            {
                playerHealthManager = player.GetComponentInChildren<HealthManager>();
            }
            
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = player.GetComponentInChildren<PlayerMovement>();
            }
            
            // Additional check for PlayerMovementScript
            if (playerMovement == null)
            {
                PlayerMovementScript moveScript = player.GetComponent<PlayerMovementScript>();
                if (moveScript != null)
                {
                    Debug.Log("CharacterUpgradeManager: Found PlayerMovementScript instead of PlayerMovement");
                    // Create adapter for PlayerMovementScript if needed
                    // For now just use a dummy PlayerMovement component
                    playerMovement = player.AddComponent<PlayerMovement>();
                    playerMovement.playerSpeed = moveScript.maxSpeed;
                    playerMovement.sprintSpeedMultiplier = 1.5f;
                }
            }
        }
        else
        {
            Debug.LogWarning("CharacterUpgradeManager: No player found with 'Player' tag");
        }
        
        // If we still haven't found the components, look in the entire scene
        if (playerHealthManager == null)
        {
            playerHealthManager = FindObjectOfType<HealthManager>();
            if (playerHealthManager != null)
                Debug.Log("CharacterUpgradeManager: Found HealthManager via FindObjectOfType");
        }
        
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
                Debug.Log("CharacterUpgradeManager: Found PlayerMovement via FindObjectOfType");
            else
            {
                // Last resort - create temporary components for upgrading
                Debug.LogWarning("CharacterUpgradeManager: Creating temporary player components");
                GameObject tempObject = new GameObject("TempPlayerComponents");
                playerMovement = tempObject.AddComponent<PlayerMovement>();
                playerMovement.playerSpeed = 5.0f;
                playerMovement.sprintSpeedMultiplier = 1.5f;
                
                if (playerHealthManager == null)
                    playerHealthManager = tempObject.AddComponent<HealthManager>();
            }
        }
        
        Debug.Log("CharacterUpgradeManager: HealthManager found: " + (playerHealthManager != null));
        Debug.Log("CharacterUpgradeManager: PlayerMovement found: " + (playerMovement != null));
    }
    
    // Update the current money display
    private void UpdateMoneyDisplay()
    {
        if (currentMoneyText)
            currentMoneyText.text = "Your Money: " + currentMoney;
    }
    
    // Update health upgrade UI
    private void UpdateHealthUI()
    {
        if (healthText && playerHealthManager != null)
        {
            healthText.text = "Max Health: " + playerHealthManager.maxHealth.ToString("F0");
            
            int cost = CalculateUpgradeCost(playerHealthManager.maxHealth, healthBaseCost, healthCostMultiplier);
            healthCostText.text = cost + " coins";
            
            // Disable button if not enough money
            if (healthButton)
                healthButton.interactable = currentMoney >= cost;
        }
    }
    
    // Update sprint time upgrade UI
    private void UpdateSprintTimeUI()
    {
        if (sprintTimeText && playerHealthManager != null)
        {
            sprintTimeText.text = "Max Stamina: " + playerHealthManager.maxStamina.ToString("F0");
            
            int cost = CalculateUpgradeCost(playerHealthManager.maxStamina, sprintTimeBaseCost, sprintTimeCostMultiplier);
            sprintTimeCostText.text = cost + " coins";
            
            // Disable button if not enough money
            if (sprintTimeButton)
                sprintTimeButton.interactable = currentMoney >= cost;
        }
    }
    
    // Update walk speed upgrade UI
    private void UpdateWalkSpeedUI()
    {
        if (walkSpeedText && playerMovement != null)
        {
            walkSpeedText.text = "Walk Speed: " + playerMovement.playerSpeed.ToString("F1");
            
            int cost = CalculateUpgradeCost(playerMovement.playerSpeed, walkSpeedBaseCost, walkSpeedCostMultiplier);
            walkSpeedCostText.text = cost + " coins";
            
            // Disable button if not enough money or approaching max speed
            if (walkSpeedButton)
                walkSpeedButton.interactable = currentMoney >= cost && playerMovement.playerSpeed < 10f;
        }
    }
    
    // Update run speed upgrade UI
    private void UpdateRunSpeedUI()
    {
        if (runSpeedText && playerMovement != null)
        {
            runSpeedText.text = "Sprint Speed: " + playerMovement.sprintSpeedMultiplier.ToString("F1");
            
            int cost = CalculateUpgradeCost(playerMovement.sprintSpeedMultiplier, runSpeedBaseCost, runSpeedCostMultiplier);
            runSpeedCostText.text = cost + " coins";
            
            // Disable button if not enough money or approaching max speed
            if (runSpeedButton)
                runSpeedButton.interactable = currentMoney >= cost && playerMovement.sprintSpeedMultiplier < 20f;
        }
    }
    
    // Calculate the cost of an upgrade based on current level
    private int CalculateUpgradeCost(float currentValue, int baseCost, float multiplier)
    {
        int level = Mathf.FloorToInt(currentValue / 10f);
        return Mathf.RoundToInt(baseCost * Mathf.Pow(multiplier, level));
    }
    
    // Upgrade health
    private void UpgradeHealth()
    {
        if (playerHealthManager == null) return;
        
        int cost = CalculateUpgradeCost(playerHealthManager.maxHealth, healthBaseCost, healthCostMultiplier);
        
        if (currentMoney >= cost)
        {
            // Apply upgrade
            playerHealthManager.maxHealth += healthUpgradeAmount;
            // Also heal the player to reflect new max health
            playerHealthManager.Heal(healthUpgradeAmount);
            
            // Deduct cost
            currentMoney -= cost;
            if (shopManager != null)
            {
                shopManager.playerMoney = currentMoney;
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Score = currentMoney;
                }
                shopManager.UpdateMoneyText();
            }
            
            // Show notification
            if (shopManager != null)
            {
                shopManager.ShowNotification("Max Health Upgraded!");
            }
            
            // Update UI
            UpdateMoneyDisplay();
            UpdateHealthUI();
        }
    }
    
    // Upgrade sprint time (stamina)
    private void UpgradeSprintTime()
    {
        if (playerHealthManager == null) return;
        
        int cost = CalculateUpgradeCost(playerHealthManager.maxStamina, sprintTimeBaseCost, sprintTimeCostMultiplier);
        
        if (currentMoney >= cost)
        {
            // Apply upgrade
            playerHealthManager.maxStamina += sprintTimeUpgradeAmount;
            // Also replenish stamina to new max
            playerHealthManager.currentStamina = playerHealthManager.maxStamina;
            
            // Deduct cost
            currentMoney -= cost;
            if (shopManager != null)
            {
                shopManager.playerMoney = currentMoney;
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Score = currentMoney;
                }
                shopManager.UpdateMoneyText();
            }
            
            // Show notification
            if (shopManager != null)
            {
                shopManager.ShowNotification("Max Stamina Upgraded!");
            }
            
            // Update UI
            UpdateMoneyDisplay();
            UpdateSprintTimeUI();
        }
    }
    
    // Upgrade walk speed
    private void UpgradeWalkSpeed()
    {
        if (playerMovement == null) return;
        
        int cost = CalculateUpgradeCost(playerMovement.playerSpeed, walkSpeedBaseCost, walkSpeedCostMultiplier);
        
        if (currentMoney >= cost && playerMovement.playerSpeed < 10f)
        {
            // Apply upgrade
            playerMovement.playerSpeed += walkSpeedUpgradeAmount;
            // Cap at max speed
            playerMovement.playerSpeed = Mathf.Min(10f, playerMovement.playerSpeed);
            
            // Deduct cost
            currentMoney -= cost;
            if (shopManager != null)
            {
                shopManager.playerMoney = currentMoney;
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Score = currentMoney;
                }
                shopManager.UpdateMoneyText();
            }
            
            // Show notification
            if (shopManager != null)
            {
                shopManager.ShowNotification("Walk Speed Upgraded!");
            }
            
            // Update UI
            UpdateMoneyDisplay();
            UpdateWalkSpeedUI();
        }
    }
    
    // Upgrade run speed
    private void UpgradeRunSpeed()
    {
        if (playerMovement == null) return;
        
        int cost = CalculateUpgradeCost(playerMovement.sprintSpeedMultiplier, runSpeedBaseCost, runSpeedCostMultiplier);
        
        if (currentMoney >= cost && playerMovement.sprintSpeedMultiplier < 20f)
        {
            // Apply upgrade
            playerMovement.sprintSpeedMultiplier += runSpeedUpgradeAmount;
            // Cap at max multiplier
            playerMovement.sprintSpeedMultiplier = Mathf.Min(20f, playerMovement.sprintSpeedMultiplier);
            
            // Deduct cost
            currentMoney -= cost;
            if (shopManager != null)
            {
                shopManager.playerMoney = currentMoney;
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Score = currentMoney;
                }
                shopManager.UpdateMoneyText();
            }
            
            // Show notification
            if (shopManager != null)
            {
                shopManager.ShowNotification("Sprint Speed Upgraded!");
            }
            
            // Update UI
            UpdateMoneyDisplay();
            UpdateRunSpeedUI();
        }
    }      // Return to shop
    private void BackToShop()
    {
        Debug.Log("CharacterUpgradeManager: BackToShop called");
        
        // Always hide the upgrade panel first
        if (characterUpgradePanel)
        {
            characterUpgradePanel.SetActive(false);
            Debug.Log("CharacterUpgradeManager: Panel deactivated");
        }
        
        // Then return to shop if possible
        if (shopManager != null)
        {
            // Return to shop
            shopManager.ReturnFromUpgradePanel();
            Debug.Log("CharacterUpgradeManager: Returned to shop");
        }
        else
        {
            Debug.LogWarning("CharacterUpgradeManager: shopManager is null");
        }
    }
}