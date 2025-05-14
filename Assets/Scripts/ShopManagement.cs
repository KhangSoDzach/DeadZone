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
    public GameObject ak47Prefab;  // Add reference to AK-47 prefab
    public int pistolAmmoAmount = 10;
    public int rifleAmmoAmount = 30;  // Add rifle ammo amount

    // UI Panels
    [Header("Shop Panels")]
    public GameObject mainShopPanel;    // Reference to the main shop panel
    public GameObject gunShopPanel;     // Reference to the gun shop panel

    void Start()
    {
        // Initialize the shop items
        shopItemsList.Add(new ShopItem { id = 1, name = "First Aid", price = 300 });
        shopItemsList.Add(new ShopItem { id = 2, name = "Pistol Ammo", price = 200 });
        shopItemsList.Add(new ShopItem { id = 3, name = "Rifle Ammo", price = 300 });
        shopItemsList.Add(new ShopItem { id = 4, name = "Buy gun", price = 0 }); 
        shopItemsList.Add(new ShopItem { id = 5, name = "Upgrade", price = 0 });
        shopItemsList.Add(new ShopItem { id = 6, name = "AK-47", price = 50 });
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

        // Make sure gun shop panel is hidden at start
        if (gunShopPanel != null)
        {
            gunShopPanel.SetActive(false);
        }
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
    public void UpdateMoneyText()
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

    // Method to check if player has enough money for a purchase
    private bool HasEnoughMoney(int price)
    {
        if (playerMoney >= price)
        {
            return true;
        }
        else
        {
            ShowNotification("Not enough money. Required: " + price + " Cash");
            Debug.Log("Not enough money. Required: " + price + ", Current: " + playerMoney);
            return false;
        }
    }

    // Method to deduct money after successful purchase
    private void DeductMoney(int amount)
    {
        playerMoney -= amount;
        
        // Đồng bộ với ScoreManager (nếu có)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Score = playerMoney;
        }
        
        // Cập nhật giao diện hiển thị tiền
        UpdateMoneyText();
    }

    // Method to be called directly from UI buttons
    public void BuyItem(int itemID)
    {
        ShopItem item = shopItemsList.Find(i => i.id == itemID);
        if (item != null)
        {
            // Special case for "Buy gun" option
            if (itemID == 4)
            {
                // Show gun shop panel instead of purchasing
                ShowGunShopPanel();
                return;
            }
            
            // Special case for "Upgrade" option
            if (itemID == 5)
            {
                // Show upgrade panel instead of purchasing
                ShowWeaponUpgradePanel();
                return;
            }
            
            // Check if player has enough money
            if (HasEnoughMoney(item.price))
            {
                // Deduct cost from score
                DeductMoney(item.price);
                
                // Hiển thị thông báo mua hàng thành công
                ShowNotification("Has Bought: " + item.name);
                
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
                        AddRifleAmmo();  // Add method for rifle ammo
                        break;
                    case 6: // AK-47
                        BuyAK47();      // Add method to buy AK-47
                        break;
                    default:
                        Debug.Log("Bought: " + item.name);
                        break;
                }
            }
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
    }      // Adds a medkit to the player's inventory (adds to inventory or increases health)
    private void AddMedkit()
    {
        // Try to use the Devion Games Inventory System first
        bool addedToInventory = false;
        
        // Check if we have the medkit prefab and try to add it to inventory
        if (medkitPrefab != null)
        {
            // Get the ItemCollection component
            DevionGames.InventorySystem.ItemCollection itemCollection = medkitPrefab.GetComponent<DevionGames.InventorySystem.ItemCollection>();
            
            if (itemCollection != null && itemCollection.Count > 0)
            {
                // Get the first item from the collection
                DevionGames.InventorySystem.Item medkitItem = itemCollection[0];
                
                // Create an instance of the item to add to inventory
                DevionGames.InventorySystem.Item instance = DevionGames.InventorySystem.InventoryManager.CreateInstance(medkitItem);
                
                // Try to add it to the player's inventory
                if (DevionGames.InventorySystem.ItemContainer.AddItem("Inventory", instance))
                {
                    ShowNotification("Medkit added to inventory!");
                    Debug.Log("Added medkit to inventory");
                    addedToInventory = true;
                }
            }
        }
        
        // If we couldn't add to inventory, fallback to health restoration or spawning
        if (!addedToInventory)
        {
            // Try to find a health component on the player
            HealthManager playerHealth = FindObjectOfType<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.Heal(50); // Add 50 health or whatever amount is appropriate
                ShowNotification("Health restored!");
                Debug.Log("Added health to player: 50");
            }
            else if (medkitPrefab != null)
            {
                // Fallback to spawning if we have a prefab but couldn't add to inventory
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    // Spawn the medkit near the player
                    Vector3 spawnPosition = player.transform.position + player.transform.forward * .2f;
                    Instantiate(medkitPrefab, spawnPosition, Quaternion.identity);
                    ShowNotification("Medkit purchased!");
                    Debug.Log("Spawned medkit at: " + spawnPosition);
                }
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
    }    // Add rifle ammo to player's weapon
    private void AddRifleAmmo()
    {
        // Try to find all guns in the scene
        Gun[] allGuns = FindObjectsOfType<Gun>();
        Gun rifle = null;
        
        // Look specifically for active rifles (non-pistol and automatic)
        foreach (Gun gun in allGuns)
        {
            if (!gun.isPistol && gun.isAutomatic)
            {
                rifle = gun;
                Debug.Log("Found active rifle: " + gun.name);
                break;
            }
        }
        
        // If no active rifle is found, try by name for more active guns
        if (rifle == null)
        {
            Debug.Log("No rifle with isAutomatic=true found, trying to find by name...");
            foreach (Gun gun in allGuns)
            {
                string lowercaseName = gun.name.ToLower();
                if (!gun.isPistol && (lowercaseName.Contains("rifle") || 
                    lowercaseName.Contains("ak") || 
                    lowercaseName.Contains("m4") || 
                    lowercaseName.Contains("assault")))
                {
                    rifle = gun;
                    Debug.Log("Found rifle by name: " + gun.name);
                    break;
                }
            }
        }
        
        // If we found a rifle, add ammo to it
        if (rifle != null)
        {
            // Add ammo to the rifle
            rifle.AddAmmo(rifleAmmoAmount);
            ShowNotification("Rifle ammo purchased: +" + rifleAmmoAmount);
            Debug.Log("Added " + rifleAmmoAmount + " ammo to rifle: " + rifle.name);
        }
        else
        {
            // If looking in active guns failed, try to look in inactive guns (in weapon inventory)
            Gun[] inactiveGuns = FindObjectsOfType<Gun>(true); // true to include inactive objects
            foreach (Gun gun in inactiveGuns)
            {
                if (!gun.gameObject.activeInHierarchy && !gun.isPistol && gun.isAutomatic)
                {
                    rifle = gun;
                    Debug.Log("Found inactive rifle: " + gun.name);
                    break;
                }
            }
            
            // Try inactive guns by name as a last resort
            if (rifle == null)
            {
                foreach (Gun gun in inactiveGuns)
                {
                    if (!gun.gameObject.activeInHierarchy && !gun.isPistol)
                    {
                        string lowercaseName = gun.name.ToLower();
                        if (lowercaseName.Contains("rifle") || 
                            lowercaseName.Contains("ak") || 
                            lowercaseName.Contains("m4") || 
                            lowercaseName.Contains("assault"))
                        {
                            rifle = gun;
                            Debug.Log("Found inactive rifle by name: " + gun.name);
                            break;
                        }
                    }
                }
            }
            
            if (rifle != null)
            {
                // Add ammo to the inactive rifle
                rifle.AddAmmo(rifleAmmoAmount);
                ShowNotification("Rifle ammo purchased: +" + rifleAmmoAmount);
                Debug.Log("Added " + rifleAmmoAmount + " ammo to inactive rifle: " + rifle.name);
            }
            else
            {
                // No rifle found anywhere - return money
                Debug.LogWarning("No rifle found in the scene (active or inactive)! Returning money.");
                playerMoney += shopItemsList.Find(i => i.id == 3).price;
                
                // Cập nhật tiền trong ScoreManager
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Score = playerMoney;
                }
                
                UpdateMoneyText();
                ShowNotification("No rifle found to add ammo to!");
            }
        }
    }    // Buy AK-47 gun
    private void BuyAK47()
    {
        // Check if AK-47 prefab is assigned
        if (ak47Prefab == null)
        {
            Debug.LogError("AK-47 prefab is not assigned in ShopManagement!");
            
            // Refund money
            playerMoney += shopItemsList.Find(i => i.id == 6).price;
            
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Score = playerMoney;
            }
            
            UpdateMoneyText();
            return;
        }
        
        // Try to add directly to inventory using Devion Games Inventory System
        // Find the item (Item component or ItemCollection) in the prefab
        DevionGames.InventorySystem.ItemCollection itemCollection = ak47Prefab.GetComponent<DevionGames.InventorySystem.ItemCollection>();
        
        if (itemCollection != null && itemCollection.Count > 0)
        {
            // Get the first item from the collection
            DevionGames.InventorySystem.Item ak47Item = itemCollection[0];
            
            // Create an instance of the item
            DevionGames.InventorySystem.Item instance = DevionGames.InventorySystem.InventoryManager.CreateInstance(ak47Item);
            
            // Try to add it to the player's inventory
            if (DevionGames.InventorySystem.ItemContainer.AddItem("Inventory", instance))
            {
                ShowNotification("AK-47 added to inventory!");
                Debug.Log("Added AK-47 to inventory");
                return;
            }
        }
        
        // If we couldn't add to inventory, fallback to spawning
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Spawn position in front of the player
            Vector3 spawnPosition = player.transform.position + player.transform.forward * 1f;
            
            // Simply instantiate the AK-47 prefab - the prefab should already have all needed components
            GameObject newAK47 = Instantiate(ak47Prefab, spawnPosition, Quaternion.identity);
            
            // Make sure it's in the Pickups layer so the player can interact with it
            newAK47.layer = LayerMask.NameToLayer("Pickups");
            
            ShowNotification("AK-47 purchased! Pick up the weapon to use it.");
            Debug.Log("Spawned AK-47 at: " + spawnPosition);
        }
        else
        {
            ShowNotification("Lỗi: Không tìm thấy người chơi!");
            Debug.LogWarning("Player object not found when trying to spawn AK-47");
            
            // Refund money since we couldn't spawn the weapon
            playerMoney += shopItemsList.Find(i => i.id == 6).price;
            
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Score = playerMoney;
            }
            
            UpdateMoneyText();
        }
    }

    // Method to show the gun shop panel
    public void ShowGunShopPanel()
    {
        if (mainShopPanel != null && gunShopPanel != null)
        {
            mainShopPanel.SetActive(false);
            gunShopPanel.SetActive(true);
            Debug.Log("Showing Gun Shop Panel");
        }
        else
        {
            Debug.LogWarning("Shop panels not assigned in inspector");
            ShowNotification("Gun Shop not available");
        }
    }
    
    // Method to go back to the main shop panel
    public void BackToMainShop()
    {
        if (mainShopPanel != null && gunShopPanel != null)
        {
            mainShopPanel.SetActive(true);
            gunShopPanel.SetActive(false);
            Debug.Log("Returning to Main Shop Panel");
        }
    }
    
    // Method to show the weapon upgrade panel
    public void ShowWeaponUpgradePanel()
    {
        // Find the WeaponUpgradeManager in scene
        WeaponUpgradeManager upgradeManager = FindObjectOfType<WeaponUpgradeManager>();
        
        
        if (upgradeManager != null)
        {
            // Hide shop panels
            if (mainShopPanel != null)
                mainShopPanel.SetActive(false);
                
            if (gunShopPanel != null)
                gunShopPanel.SetActive(false);
                
            // Show the weapon type selection panel
            upgradeManager.ShowWeaponTypeSelection(playerMoney, this);
        }
        else
        {
            ShowNotification("Weapon Upgrade system not available!");   
            Debug.Log("WeaponUpgradeManager not found in scene!");
        }
    }
    
    // Method to be called from WeaponUpgradeManager to return to main shop
    public void ReturnFromUpgradePanel()
    {
        if (mainShopPanel != null)
        {
            mainShopPanel.SetActive(true);
        }
    }
}
