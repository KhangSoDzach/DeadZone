using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUpgradeManager : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject weaponTypeSelectionPanel;
    public GameObject weaponUpgradePanel;
    
    [Header("Weapon Type Selection UI")]
    public Button pistolButton;
    public Button rifleButton;
    public TextMeshProUGUI noWeaponText;
    
    [Header("Weapon Upgrade UI")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI currentMoneyText;
    
    [Header("Ammo Capacity Upgrade")]
    public Button ammoCapacityButton;
    public TextMeshProUGUI ammoCapacityText;
    public TextMeshProUGUI ammoCapacityCostText;
    public int ammoCapacityUpgradeAmount = 5;
    public int ammoCapacityBaseCost = 200;
    public float ammoCapacityCostMultiplier = 1.5f;
    
    [Header("Magazine Size Upgrade")]
    public Button magazineSizeButton;
    public TextMeshProUGUI magazineSizeText;
    public TextMeshProUGUI magazineSizeCostText;
    public int magazineSizeUpgradeAmount = 2;
    public int magazineSizeBaseCost = 300;
    public float magazineSizeCostMultiplier = 1.5f;
    
    [Header("Damage Upgrade")]
    public Button damageButton;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI damageCostText;
    public float damageUpgradeAmount = 2f;
    public int damageBaseCost = 500;
    public float damageCostMultiplier = 2f;
    
    [Header("Reload Time Upgrade")]
    public Button reloadTimeButton;
    public TextMeshProUGUI reloadTimeText;
    public TextMeshProUGUI reloadTimeCostText;
    public float reloadTimeUpgradeAmount = 0.1f;
    public int reloadTimeBaseCost = 400;
    public float reloadTimeCostMultiplier = 1.75f;
    
    [Header("Navigation Buttons")]
    public Button backToShopButton;
    public Button backToWeaponTypeButton;
    
    // Reference to the shop management for returning to shop
    private ShopManagement shopManager;
    
    // Current selected weapon and player money
    private Gun currentWeapon;
    private int currentMoney;
    private bool isPistol;
    
    private void Start()
    {
        // Initially hide all panels
        if (weaponTypeSelectionPanel)
            weaponTypeSelectionPanel.SetActive(false);
            
        if (weaponUpgradePanel)
            weaponUpgradePanel.SetActive(false);
            
        // Add listeners to buttons
        if (pistolButton)
            pistolButton.onClick.AddListener(() => SelectWeaponType(true));
            
        if (rifleButton)
            rifleButton.onClick.AddListener(() => SelectWeaponType(false));
            
        if (backToShopButton)
            backToShopButton.onClick.AddListener(BackToShop);
            
        if (backToWeaponTypeButton)
            backToWeaponTypeButton.onClick.AddListener(BackToWeaponTypeSelection);
            
        // Add listeners to upgrade buttons
        if (ammoCapacityButton)
            ammoCapacityButton.onClick.AddListener(() => UpgradeAmmoCapacity());
            
        if (magazineSizeButton)
            magazineSizeButton.onClick.AddListener(() => UpgradeMagazineSize());
            
        if (damageButton)
            damageButton.onClick.AddListener(() => UpgradeDamage());
            
        if (reloadTimeButton)
            reloadTimeButton.onClick.AddListener(() => UpgradeReloadTime());
    }
    // Show the weapon type selection panel
    public void ShowWeaponTypeSelection(int playerMoney, ShopManagement shop)
    {
        currentMoney = playerMoney;
        shopManager = shop;
        
        // Check if player has weapons in the WeaponHolder
        bool hasPistol = false;
        bool hasRifle = false;
        bool hasActiveRifle = false;
        
        // Find the WeaponManager to access the WeaponHolder
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null && weaponManager.weaponHolder != null)
        {
            // Check only guns in the WeaponHolder
            foreach (Transform child in weaponManager.weaponHolder)
            {
                Gun gun = child.GetComponent<Gun>();
                if (gun != null)
                {
                    if (gun.isPistol)
                        hasPistol = true;
                    else if (gun.isAutomatic) {
                        hasRifle = true;
                        
                        // Kiểm tra xem súng trường có đang được trang bị (active) không
                        if (child.gameObject.activeInHierarchy) {
                            hasActiveRifle = true;
                            Debug.Log("Đã tìm thấy súng trường đang active trong WeaponHolder: " + gun.name);
                        }
                    }
                }
            }
        }
        
        // Enable/disable buttons based on weapon availability
        if (pistolButton)
            pistolButton.interactable = hasPistol;
            
        if (rifleButton)
            // Chỉ cho phép nâng cấp súng trường khi có súng trường ĐANG ACTIVE
            rifleButton.interactable = hasActiveRifle;
            
        if (noWeaponText)
            noWeaponText.gameObject.SetActive(!hasPistol && !hasRifle);
        
        // Show the panel
        if (weaponTypeSelectionPanel)
            weaponTypeSelectionPanel.SetActive(true);
            
        if (weaponUpgradePanel)
            weaponUpgradePanel.SetActive(false);
    }
    // Select weapon type (pistol or rifle)
    private void SelectWeaponType(bool isPistol)
    {
        this.isPistol = isPistol;
        
        // Find the appropriate weapon from the WeaponHolder
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null && weaponManager.weaponHolder != null)
        {
            // Find guns that are children of WeaponHolder
            foreach (Transform child in weaponManager.weaponHolder)
            {
                Gun gun = child.GetComponent<Gun>();
                if (gun != null)
                {
                    if (isPistol && gun.isPistol) {
                        currentWeapon = gun;
                        break;
                    } else if (!isPistol && gun.isAutomatic && !gun.isPistol) {
                        // Đối với súng trường, chỉ chọn súng đang active
                        if (child.gameObject.activeInHierarchy) {
                            currentWeapon = gun;
                            break;
                        }
                    }
                }
            }
        }
        
        if (currentWeapon != null)
        {
            ShowWeaponUpgradePanel();
        }
        else
        {
            // This shouldn't happen if buttons are disabled properly
            Debug.LogWarning("No weapon found of selected type in WeaponHolder!");
        }
    }
    
    // Show the weapon upgrade panel with current weapon stats
    private void ShowWeaponUpgradePanel()
    {
        if (weaponUpgradePanel && currentWeapon != null)
        {
            // Show upgrade panel and hide selection panel
            weaponTypeSelectionPanel.SetActive(false);
            weaponUpgradePanel.SetActive(true);
            
            // Update weapon name and player money
            if (weaponNameText)
                weaponNameText.text = isPistol ? "Pistol Upgrades" : "Rifle Upgrades";
                
            UpdateMoneyDisplay();
            
            // Update all upgrade displays
            UpdateAmmoCapacityUI();
            UpdateMagazineSizeUI();
            UpdateDamageUI();
            UpdateReloadTimeUI();
        }
    }
    
    // Update the current money display
    private void UpdateMoneyDisplay()
    {
        if (currentMoneyText)
            currentMoneyText.text = "Your Money: " + currentMoney;
    }
    
    // Update ammo capacity upgrade UI
    private void UpdateAmmoCapacityUI()
    {
        if (ammoCapacityText && currentWeapon != null)
        {
            ammoCapacityText.text = "Total Ammo: " + currentWeapon.totalAmmo;
            
            int cost = CalculateUpgradeCost(currentWeapon.totalAmmo, ammoCapacityBaseCost, ammoCapacityCostMultiplier);
            ammoCapacityCostText.text = cost + " coins";
            
            // Disable button if not enough money
            if (ammoCapacityButton)
                ammoCapacityButton.interactable = currentMoney >= cost;
        }
    }
    
    // Update magazine size upgrade UI
    private void UpdateMagazineSizeUI()
    {
        if (magazineSizeText && currentWeapon != null)
        {
            magazineSizeText.text = "Magazine Size: " + currentWeapon.maxAmmo;
            
            int cost = CalculateUpgradeCost(currentWeapon.maxAmmo, magazineSizeBaseCost, magazineSizeCostMultiplier);
            magazineSizeCostText.text = cost + " coins";
            
            // Disable button if not enough money
            if (magazineSizeButton)
                magazineSizeButton.interactable = currentMoney >= cost;
        }
    }
    
    // Update damage upgrade UI
    private void UpdateDamageUI()
    {
        if (damageText && currentWeapon != null)
        {
            damageText.text = "Damage: " + currentWeapon.damage.ToString("F1");
            
            int upgradeLevel = Mathf.FloorToInt((currentWeapon.damage - 10f) / damageUpgradeAmount);
            int cost = CalculateUpgradeCost(upgradeLevel, damageBaseCost, damageCostMultiplier);
            damageCostText.text = cost + " coins";
            
            // Disable button if not enough money
            if (damageButton)
                damageButton.interactable = currentMoney >= cost;
        }
    }
    
    // Update reload time upgrade UI
    private void UpdateReloadTimeUI()
    {
        if (reloadTimeText && currentWeapon != null)
        {
            reloadTimeText.text = "Reload Time: " + currentWeapon.reloadTime.ToString("F1") + "s";
            
            // Calculate upgrade level (inverted because lower is better)
            int upgradeLevel = Mathf.FloorToInt((2f - currentWeapon.reloadTime) / reloadTimeUpgradeAmount);
            int cost = CalculateUpgradeCost(upgradeLevel, reloadTimeBaseCost, reloadTimeCostMultiplier);
            reloadTimeCostText.text = cost + " coins";
            
            // Disable button if not enough money or at minimum reload time
            if (reloadTimeButton)
                reloadTimeButton.interactable = currentMoney >= cost && currentWeapon.reloadTime > 0.5f;
        }
    }
    
    // Calculate the cost of an upgrade based on current level
    private int CalculateUpgradeCost(int currentLevel, int baseCost, float multiplier)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(multiplier, currentLevel / 5f));
    }
    
    // Calculate the cost of an upgrade based on current value
    private int CalculateUpgradeCost(float currentValue, int baseCost, float multiplier)
    {
        int level = Mathf.FloorToInt(currentValue / 5f);
        return Mathf.RoundToInt(baseCost * Mathf.Pow(multiplier, level));
    }
    
    // Upgrade ammo capacity
    private void UpgradeAmmoCapacity()
    {
        if (currentWeapon == null) return;
        
        int cost = CalculateUpgradeCost(currentWeapon.totalAmmo, ammoCapacityBaseCost, ammoCapacityCostMultiplier);
        
        if (currentMoney >= cost)
        {
            // Apply upgrade
            currentWeapon.totalAmmo += ammoCapacityUpgradeAmount;
            
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
                shopManager.ShowNotification("Ammo Capacity Upgraded!");
            }
            
            // Update UI
            UpdateMoneyDisplay();
            UpdateAmmoCapacityUI();
            
            // Also update the ammo UI
            currentWeapon.UpdateAmmoUI();
        }
    }
    
    // Upgrade magazine size
    private void UpgradeMagazineSize()
    {
        if (currentWeapon == null) return;
        
        int cost = CalculateUpgradeCost(currentWeapon.maxAmmo, magazineSizeBaseCost, magazineSizeCostMultiplier);
        
        if (currentMoney >= cost)
        {
            // Apply upgrade
            currentWeapon.maxAmmo += magazineSizeUpgradeAmount;
            
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
                shopManager.ShowNotification("Magazine Size Upgraded!");
            }
            
            // Update UI
            UpdateMoneyDisplay();
            UpdateMagazineSizeUI();
            
            // Also update the ammo UI
            currentWeapon.UpdateAmmoUI();
        }
    }
    
    // Upgrade damage
    private void UpgradeDamage()
    {
        if (currentWeapon == null) return;
        
        int upgradeLevel = Mathf.FloorToInt((currentWeapon.damage - 10f) / damageUpgradeAmount);
        int cost = CalculateUpgradeCost(upgradeLevel, damageBaseCost, damageCostMultiplier);
        
        if (currentMoney >= cost)
        {
            // Apply upgrade
            currentWeapon.damage += damageUpgradeAmount;
            
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
                shopManager.ShowNotification("Weapon Damage Upgraded!");
            }
            
            // Update UI
            UpdateMoneyDisplay();
            UpdateDamageUI();
        }
    }
    
    // Upgrade reload time (decrease it)
    private void UpgradeReloadTime()
    {
        if (currentWeapon == null) return;
        
        int upgradeLevel = Mathf.FloorToInt((2f - currentWeapon.reloadTime) / reloadTimeUpgradeAmount);
        int cost = CalculateUpgradeCost(upgradeLevel, reloadTimeBaseCost, reloadTimeCostMultiplier);
        
        if (currentMoney >= cost && currentWeapon.reloadTime > 0.5f)
        {
            // Apply upgrade (reduce reload time)
            currentWeapon.reloadTime -= reloadTimeUpgradeAmount;
            // Make sure it doesn't go below minimum
            currentWeapon.reloadTime = Mathf.Max(0.5f, currentWeapon.reloadTime);
            
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
                shopManager.ShowNotification("Reload Time Improved!");
            }
            
            // Update UI
            UpdateMoneyDisplay();
            UpdateReloadTimeUI();
        }
    }
    
    // Return to weapon type selection panel
    private void BackToWeaponTypeSelection()
    {
        if (weaponTypeSelectionPanel && weaponUpgradePanel)
        {
            weaponTypeSelectionPanel.SetActive(true);
            weaponUpgradePanel.SetActive(false);
        }
    }
    
    // Return to shop
    private void BackToShop()
    {
        if (shopManager != null)
        {
            // Hide upgrade panels
            if (weaponTypeSelectionPanel)
                weaponTypeSelectionPanel.SetActive(false);
                
            if (weaponUpgradePanel)
                weaponUpgradePanel.SetActive(false);
                
            // Return to shop
            shopManager.ReturnFromUpgradePanel();
        }
    }
}
