using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopManagement : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public int id;
        public string name;
        public int price;
    }

    public List<ShopItem> shopItemsList = new List<ShopItem>();
    
    // Player's money/score
    public int playerMoney = 1000;  // Starting money
    public Text moneyText;          // UI Text to display money
    
    // References to Player's weapons and systems
    private Gun playerGun;
    
    // UI Elements for notification
    [Header("Notification")]
    public GameObject notificationPanel;
    public Text notificationText;
    private float notificationDuration = 2f;
    
    // Prefabs for spawned items
    [Header("Item Prefabs")]
    public GameObject medkitPrefab;
    public int pistolAmmoAmount = 10;

    void Start()
    {
        // Initialize the shop items
        shopItemsList.Add(new ShopItem { id = 1, name = "First Aid", price = 300 });
        shopItemsList.Add(new ShopItem { id = 2, name = "Pistol Ammo", price = 200 });
        shopItemsList.Add(new ShopItem { id = 3, name = "Rifle Ammo", price = 300 });
        
        // Find player's gun if present
        playerGun = FindObjectOfType<Gun>();
        
        // Make sure notification panel is hidden at start
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        
        // Lấy tiền từ ScoreManager (nếu đã được khởi tạo)
        if (ScoreManager.Instance != null)
        {
            playerMoney = ScoreManager.Score;
        }
        
        // Update the money text with current score
        UpdateMoneyText();
    }
    
    void Update()
    {
        // Đồng bộ tiền với ScoreManager
        if (ScoreManager.Instance != null && playerMoney != ScoreManager.Score)
        {
            playerMoney = ScoreManager.Score;
            UpdateMoneyText();
        }
    }
    
    // Update the money text with the current score
    private void UpdateMoneyText()
    {
        if (moneyText != null)
        {
            moneyText.text = "Coins: " + playerMoney.ToString();
        }
        
        // Đồng bộ với ScoreManager (nếu có)
        if (ScoreManager.Instance != null && playerMoney != ScoreManager.Score)
        {
            ScoreManager.Score = playerMoney;
        }
    }

    // Method to be called directly from UI buttons
    public void BuyItem(int itemID)
    {
        ShopItem item = shopItemsList.Find(i => i.id == itemID);
        if (item != null && playerMoney >= item.price)
        {
            // Deduct cost from score
            playerMoney -= item.price;
            
            // Cập nhật tiền trong ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Score = playerMoney;
            }
            
            // Cập nhật giao diện hiển thị tiền
            UpdateMoneyText();
            
            // Hiển thị thông báo mua hàng thành công
            ShowNotification("Đã mua: " + item.name);
            
            // Handle different items
            switch(itemID)
            {
                case 1: // First Aid (MedKit)
                    AddMedkit();
                    break;
                case 2: // Pistol Ammo
                    AddPistolAmmo();
                    break;
                case 3: // Rifle Ammo
                    // Implement rifle ammo similar to pistol ammo
                    Debug.Log("Bought: " + item.name);
                    break;
                default:
                    Debug.Log("Bought: " + item.name);
                    break;
            }
        }
        else
        {
            Debug.Log("Not enough coins");
            ShowNotification("Not enough money");
        }
    }
    
    // Legacy method - keeping for compatibility
    public void Buy()
    {
        GameObject buttonPoint = GameObject.FindGameObjectWithTag("Event").GetComponent<EventSystem>().currentSelectedGameObject;   
        if (buttonPoint != null && buttonPoint.GetComponent<ItemInfo>() != null)
        {
            int itemID = buttonPoint.GetComponent<ItemInfo>().itemID;
            BuyItem(itemID);
        }
        else
        {
            Debug.LogWarning("Button without ItemInfo component used with Buy method");
        }
    }
    
    // Method to show notification
    public void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            // Set message text
            notificationText.text = message;
            
            // Show the notification panel
            notificationPanel.SetActive(true);
            
            // Hide it after the duration
            StopAllCoroutines(); // Stop any existing notification coroutines
            StartCoroutine(HideNotificationAfterDelay());
        }
    }
    
    // Coroutine to hide notification after a delay
    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
    
    // Adds a medkit to the player's inventory (spawns medkit or increases health)
    private void AddMedkit()
    {
        // Option 1: Spawn a medkit in the scene
        if (medkitPrefab != null)
        {
            // Find the player object
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // Spawn the medkit near the player
                Vector3 spawnPosition = player.transform.position + player.transform.forward * .2f ;
                Instantiate(medkitPrefab, spawnPosition, Quaternion.identity);
                ShowNotification("Medkit purchased!");
                Debug.Log("Spawned medkit at: " + spawnPosition);
            }
        }
        else
        {
            // Try to find a health component on the player
            HealthManager playerHealth = FindObjectOfType<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.Heal(50); // Add 50 health or whatever amount is appropriate
                ShowNotification("Health restored!");
                Debug.Log("Added health to player: 50");
            }
            else
            {
                Debug.LogWarning("No medkit prefab assigned and no PlayerHealth found!");
                // Return the money since purchase failed
                playerMoney += shopItemsList.Find(i => i.id == 1).price;
                
                // Cập nhật tiền trong ScoreManager
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Score = playerMoney;
                }
                
                UpdateMoneyText();
            }
        }
    }
    
    // Adds pistol ammo to the player's weapon
    private void AddPistolAmmo()
    {
        // Try to find all guns in the scene
        Gun[] allGuns = FindObjectsOfType<Gun>();
        Gun pistol = null;
        
        // Look specifically for the pistol (gun with isPistol=true)
        foreach (Gun gun in allGuns)
        {
            if (gun.isPistol)
            {
                pistol = gun;
                Debug.Log("Found pistol: " + gun.name + " with isPistol=true");
                break;
            }
        }
        
        // If no gun with isPistol flag is found, try by name
        if (pistol == null)
        {
            Debug.Log("No gun with isPistol=true found, trying to find by name...");
            foreach (Gun gun in allGuns)
            {
                string lowercaseName = gun.name.ToLower();
                if (lowercaseName.Contains("pistol") || 
                    lowercaseName.Contains("handgun") || 
                    lowercaseName.Contains("glock") || 
                    lowercaseName.Contains("revolver"))
                {
                    pistol = gun;
                    Debug.Log("Found pistol by name: " + gun.name);
                    break;
                }
            }
        }
        
        // If we found a pistol, add ammo to it
        if (pistol != null)
        {
            // Add ammo to the pistol
            pistol.AddAmmo(pistolAmmoAmount);
            ShowNotification("Ammo pistol purchased: +" + pistolAmmoAmount);
            Debug.Log("Added " + pistolAmmoAmount + " ammo to pistol: " + pistol.name);
        }
        else
        {
            // If looking in active guns failed, try to look in inactive guns (in weapon inventory)
            Gun[] inactiveGuns = FindObjectsOfType<Gun>(true); // true to include inactive objects
            foreach (Gun gun in inactiveGuns)
            {
                if (!gun.gameObject.activeInHierarchy && gun.isPistol)
                {
                    pistol = gun;
                    Debug.Log("Found inactive pistol: " + gun.name);
                    break;
                }
            }
            
            if (pistol != null)
            {
                // Add ammo to the inactive pistol
                pistol.AddAmmo(pistolAmmoAmount);
                ShowNotification("Ammo pistol purchased: +" + pistolAmmoAmount);
                Debug.Log("Added " + pistolAmmoAmount + " ammo to inactive pistol: " + pistol.name);
            }
            else
            {
                // No pistol found anywhere - return money
                Debug.LogWarning("No pistol found in the scene (active or inactive)! Returning money.");
                playerMoney += shopItemsList.Find(i => i.id == 2).price;
                
                // Cập nhật tiền trong ScoreManager
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Score = playerMoney;
                }
                
                UpdateMoneyText();
                ShowNotification("No pistol found to add ammo to!");
            }
        }
    }
}
