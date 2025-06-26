using System.Collections;
using System.Collections.Generic;
using Scripts.API;
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
    
    // Variable to track if shop is open
    private bool isShopOpen = false;
    
    // Prefabs for spawned items
    [Header("Item Prefabs")]
    public GameObject medkitPrefab;
    public GameObject m4a1Prefab;  // Đổi từ ak47Prefab sang m4a1Prefab
    public GameObject mp5Prefab;   // Thêm prefab cho MP5
    public GameObject uziPrefab;   // Thêm prefab cho UZI
    public int pistolAmmoAmount = 10;
    public int rifleAmmoAmount = 30;  // Add rifle ammo amount

    // UI Panels
    [Header("Shop Panels")]
    public GameObject mainShopPanel;    // Reference to the main shop panel
    public GameObject gunShopPanel;     // Reference to the gun shop panel

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        //Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        //foreach (Canvas canvas in canvases)
        //{
        //    Transform shopUITransform = canvas.transform.Find("ShopPanel");
        //    Transform gunShopPanelTransform = canvas.transform.Find("GunShopPanel");

        //    if (shopUITransform != null)
        //    {
        //        mainShopPanel = shopUITransform.gameObject;
        //        mainShopPanel.SetActive(false);
        //    }
        //    if (gunShopPanelTransform != null)
        //    {
        //        gunShopPanel = gunShopPanelTransform.gameObject;
        //        //gunShopPanel.SetActive(false);
        //        return;
        //    }
        //}

       
        // Initialize the shop items
        shopItemsList.Add(new ShopItem { id = 1, name = "First Aid", price = 100 });
        shopItemsList.Add(new ShopItem { id = 2, name = "Pistol Ammo", price = 50 });
        shopItemsList.Add(new ShopItem { id = 3, name = "Rifle Ammo", price = 70 });
        shopItemsList.Add(new ShopItem { id = 4, name = "Buy gun", price = 0 }); 
        shopItemsList.Add(new ShopItem { id = 5, name = "Weapon Upgrade", price = 0 });
        shopItemsList.Add(new ShopItem { id = 6, name = "M4A1", price = 50 }); // Đổi tên AK-47 thành M4A1
        shopItemsList.Add(new ShopItem { id = 7, name = "Character Upgrade", price = 0 });
        shopItemsList.Add(new ShopItem { id = 8, name = "MP5", price = 600 });  // Thêm MP5
        shopItemsList.Add(new ShopItem { id = 9, name = "UZI", price = 800 });  // Thêm UZI
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

        
        
        // Set initial shop state
        isShopOpen = mainShopPanel != null && mainShopPanel.activeSelf;
        
        // Set cursor state based on shop state
        if (isShopOpen) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Notify PauseMenu about shop state
        NotifyPauseMenuOfShopState();
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
              // Special case for "Weapon Upgrade" option
            if (itemID == 5)
            {
                // Show weapon upgrade panel instead of purchasing
                ShowWeaponUpgradePanel();
                return;
            }
            
            // Special case for "Character Upgrade" option
            if (itemID == 7)
            {
                // Show character upgrade panel instead of purchasing
                ShowCharacterUpgradePanel();
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
                    case 6: // M4A1
                        BuyM4A1();      // Đổi từ BuyAK47 sang BuyM4A1
                        break;
                    case 8: // MP5
                        BuyMP5();
                        break;
                    case 9: // UZI
                        BuyUZI();
                        break;
                    default:
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
                }
            }
            else
            {
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
                break;
            }
        }
        
        // If no gun with isPistol flag is found, try by name
        if (pistol == null)
        {
            foreach (Gun gun in allGuns)
            {
                string lowercaseName = gun.name.ToLower();
                if (lowercaseName.Contains("pistol") || 
                    lowercaseName.Contains("handgun") || 
                    lowercaseName.Contains("glock") || 
                    lowercaseName.Contains("revolver"))
                {
                    pistol = gun;
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
                    break;
                }
            }
            
            if (pistol != null)
            {
                // Add ammo to the inactive pistol
                pistol.AddAmmo(pistolAmmoAmount);
                ShowNotification("Ammo pistol purchased: +" + pistolAmmoAmount);
            }
            else
            {
                // No pistol found anywhere - return money
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
        // Check if player has a rifle in their weapon holder first
        if (!PlayerHasRifle())
        {
            // If no rifle is found, show notification and refund money
            playerMoney += shopItemsList.Find(i => i.id == 3).price;
            
            // Cập nhật tiền trong ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Score = playerMoney;
            }
            
            UpdateMoneyText();
            ShowNotification("You need to buy a rifle first!");
            return;
        }

        // Try to find all guns in the scene
        Gun[] allGuns = FindObjectsOfType<Gun>();
        Gun rifle = null;
        
        // Look specifically for active rifles (non-pistol and automatic)
        foreach (Gun gun in allGuns)
        {
            if (!gun.isPistol && gun.isAutomatic)
            {
                rifle = gun;
                break;
            }
        }
        
        // If no active rifle is found, try by name for more active guns
        if (rifle == null)
        {
            foreach (Gun gun in allGuns)
            {
                string lowercaseName = gun.name.ToLower();
                if (!gun.isPistol && (lowercaseName.Contains("rifle") || 
                    lowercaseName.Contains("ak") || 
                    lowercaseName.Contains("m4") || 
                    lowercaseName.Contains("assault")))
                {
                    rifle = gun;
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
            }
            else
            {
                // No rifle found anywhere - return money
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
    }    // Buy M4A1 gun
    private void BuyM4A1()
    {
        if (m4a1Prefab == null)
        {
            playerMoney += shopItemsList.Find(i => i.id == 6).price;
            if (ScoreManager.Instance != null)
                ScoreManager.Score = playerMoney;
            UpdateMoneyText();
            return;
        }
        DevionGames.InventorySystem.ItemCollection itemCollection = m4a1Prefab.GetComponent<DevionGames.InventorySystem.ItemCollection>();
        if (itemCollection != null && itemCollection.Count > 0)
        {
            DevionGames.InventorySystem.Item m4a1Item = itemCollection[0];
            DevionGames.InventorySystem.Item instance = DevionGames.InventorySystem.InventoryManager.CreateInstance(m4a1Item);
            if (DevionGames.InventorySystem.ItemContainer.AddItem("Inventory", instance))
            {
                ShowNotification("M4A1 added to inventory!");
                return;
            }
        }
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 spawnPosition = player.transform.position + player.transform.forward * 1f;
            GameObject newM4A1 = Instantiate(m4a1Prefab, spawnPosition, Quaternion.identity);
            newM4A1.layer = LayerMask.NameToLayer("Pickups");
            ShowNotification("M4A1 purchased! Pick up the weapon to use it.");
        }
        else
        {
            ShowNotification("Lỗi: Không tìm thấy người chơi!");
            playerMoney += shopItemsList.Find(i => i.id == 6).price;
            if (ScoreManager.Instance != null)
                ScoreManager.Score = playerMoney;
            UpdateMoneyText();
        }
    }
    // Buy MP5 gun
    private void BuyMP5()
    {
        if (mp5Prefab == null)
        {
            playerMoney += shopItemsList.Find(i => i.id == 8).price;
            if (ScoreManager.Instance != null)
                ScoreManager.Score = playerMoney;
            UpdateMoneyText();
            return;
        }
        DevionGames.InventorySystem.ItemCollection itemCollection = mp5Prefab.GetComponent<DevionGames.InventorySystem.ItemCollection>();
        if (itemCollection != null && itemCollection.Count > 0)
        {
            DevionGames.InventorySystem.Item mp5Item = itemCollection[0];
            DevionGames.InventorySystem.Item instance = DevionGames.InventorySystem.InventoryManager.CreateInstance(mp5Item);
            if (DevionGames.InventorySystem.ItemContainer.AddItem("Inventory", instance))
            {
                ShowNotification("MP5 added to inventory!");
                return;
            }
        }
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 spawnPosition = player.transform.position + player.transform.forward * 1f;
            GameObject newMP5 = Instantiate(mp5Prefab, spawnPosition, Quaternion.identity);
            newMP5.layer = LayerMask.NameToLayer("Pickups");
            ShowNotification("MP5 purchased! Pick up the weapon to use it.");
        }
        else
        {
            ShowNotification("Erro: Player not found!");
            playerMoney += shopItemsList.Find(i => i.id == 8).price;
            if (ScoreManager.Instance != null)
                ScoreManager.Score = playerMoney;
            UpdateMoneyText();
        }
    }
    // Buy UZI gun
    private void BuyUZI()
    {
        if (uziPrefab == null)
        {
            playerMoney += shopItemsList.Find(i => i.id == 9).price;
            if (ScoreManager.Instance != null)
                ScoreManager.Score = playerMoney;
            UpdateMoneyText();
            return;
        }
        DevionGames.InventorySystem.ItemCollection itemCollection = uziPrefab.GetComponent<DevionGames.InventorySystem.ItemCollection>();
        if (itemCollection != null && itemCollection.Count > 0)
        {
            DevionGames.InventorySystem.Item uziItem = itemCollection[0];
            DevionGames.InventorySystem.Item instance = DevionGames.InventorySystem.InventoryManager.CreateInstance(uziItem);
            if (DevionGames.InventorySystem.ItemContainer.AddItem("Inventory", instance))
            {
                ShowNotification("UZI added to inventory!");
                return;
            }
        }
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 spawnPosition = player.transform.position + player.transform.forward * 1f;
            GameObject newUZI = Instantiate(uziPrefab, spawnPosition, Quaternion.identity);
            newUZI.layer = LayerMask.NameToLayer("Pickups");
            ShowNotification("UZI purchased! Pick up the weapon to use it.");
        }
        else
        {
            ShowNotification("Lỗi: Không tìm thấy người chơi!");
            playerMoney += shopItemsList.Find(i => i.id == 9).price;
            if (ScoreManager.Instance != null)
                ScoreManager.Score = playerMoney;
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
            
            // Set shop as open to disable weapon firing
            isShopOpen = true;
            
            // Lock cursor to interact with UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
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
            
            // Shop is still open, just changed panels
            isShopOpen = true;
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
            
            // Set shop as open to disable weapon firing
            isShopOpen = true;
            
            // Lock cursor to interact with UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            ShowNotification("Weapon Upgrade system not available!");   
        }
    }    // Method to show character upgrade panel
    public void ShowCharacterUpgradePanel()
    {
        // Find the CharacterUpgradeManager in scene
        CharacterUpgradeManager upgradeManager = FindObjectOfType<CharacterUpgradeManager>();
        
        if (upgradeManager != null)
        {
            // Try to hide shop panels first (though the character manager will also do this)
            if (mainShopPanel != null)
            {
                mainShopPanel.SetActive(false);
            }
                
            if (gunShopPanel != null)
            {
                gunShopPanel.SetActive(false);
            }
            
            // Also hide any weapon upgrade panels that might be open
            WeaponUpgradeManager weaponManager = FindObjectOfType<WeaponUpgradeManager>();
            if (weaponManager != null)
            {
                if (weaponManager.weaponTypeSelectionPanel != null)
                {
                    weaponManager.weaponTypeSelectionPanel.SetActive(false);
                }
                
                if (weaponManager.weaponUpgradePanel != null)
                {
                    weaponManager.weaponUpgradePanel.SetActive(false);
                }
            }
            
            // Then show the character upgrade panel
            upgradeManager.ShowCharacterUpgradePanel(playerMoney, this);
            
            // Set shop as open to disable weapon firing
            isShopOpen = true;
            
            // Lock cursor to interact with UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            ShowNotification("Character Upgrade system not available!");   
        }
    }    // Method to be called from WeaponUpgradeManager or CharacterUpgradeManager to return to main shop
    public void ReturnFromUpgradePanel()
    {
        // First deactivate ALL panels to ensure clean state
        
        // Ensure any character upgrade panel is closed
        CharacterUpgradeManager characterManager = FindObjectOfType<CharacterUpgradeManager>();
        if (characterManager != null && characterManager.characterUpgradePanel != null)
        {
            characterManager.characterUpgradePanel.SetActive(false);
        }
        
        // Ensure any weapon upgrade panels are closed
        WeaponUpgradeManager weaponManager = FindObjectOfType<WeaponUpgradeManager>();
        if (weaponManager != null)
        {
            if (weaponManager.weaponTypeSelectionPanel != null)
            {
                weaponManager.weaponTypeSelectionPanel.SetActive(false);
            }
                
            if (weaponManager.weaponUpgradePanel != null)
            {
                weaponManager.weaponUpgradePanel.SetActive(false);
            }
        }
        else
        {
            // As a fallback, try to find the panels by name and hide them
            GameObject weaponTypePanel = GameObject.Find("WeaponTypeSelectionPanel");
            if (weaponTypePanel != null)
            {
                weaponTypePanel.SetActive(false);
            }
            
            GameObject weaponUpgradePanel = GameObject.Find("WeaponUpgradePanel");
            if (weaponUpgradePanel != null)
            {
                weaponUpgradePanel.SetActive(false);
            }
        }
        
        // Also close shop panels to ensure clean state
        if (gunShopPanel != null)
            gunShopPanel.SetActive(false);
        
        // Then show the main shop panel
        if (mainShopPanel != null)
        {
            mainShopPanel.SetActive(true);
            
            // Set shop as open to disable weapon firing
            isShopOpen = true;
        }
    }
    
    // Method to close all shop panels and return to gameplay
    public void CloseShop()
    {
        if (mainShopPanel != null)
            mainShopPanel.SetActive(false);
            
        if (gunShopPanel != null)
            gunShopPanel.SetActive(false);
        
        // Set shop as closed to enable weapon firing
        isShopOpen = false;
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    // Public method to check if shop is open (for other scripts to check)
    public bool IsShopOpen()
    {
        return isShopOpen;
    }

    // Method to open the main shop
    public void OpenShop()
    {
        if (mainShopPanel != null)
        {
            mainShopPanel.SetActive(true);
            
            if (gunShopPanel != null)
                gunShopPanel.SetActive(false);
            
            // Set shop as open to disable weapon firing
            isShopOpen = true;
            
            // Lock cursor to interact with UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            NotifyPauseMenuOfShopState();
        }
    }

    // New helper method to check if player has a rifle in their weapon holder
    private bool PlayerHasRifle()
    {
        // Find the weapon holder
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null && weaponManager.weaponHolder != null)
        {
            // Check all weapons in the holder
            foreach (Transform weapon in weaponManager.weaponHolder)
            {
                Gun gun = weapon.GetComponent<Gun>();
                if (gun != null && !gun.isPistol)
                {
                    // Found a rifle in the weapon holder
                    return true;
                }
            }
        }
        
        // No rifle found
        return false;
    }
    

    // Method to notify pause menu when shop state changes
    private void NotifyPauseMenuOfShopState()
    {
        PauseMenu pauseMenu = FindObjectOfType<PauseMenu>();
        if (pauseMenu != null && isShopOpen)
        {
            // Ensure pause menu is closed when shop opens
            if (pauseMenu.IsPaused)
            {
                pauseMenu.ContinueGame();
            }
        }
    }
}
