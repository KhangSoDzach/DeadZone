using UnityEngine;
using System.Reflection;

/// <summary>
/// This script adds functionality to block weapon firing when shop is open
/// Attach this to a GameObject in the scene to ensure it works correctly
/// </summary>
public class ShopWeaponBlocker : MonoBehaviour
{
    // Singleton instance for easy access
    public static ShopWeaponBlocker Instance { get; private set; }
    
    // Reference to shop manager
    private ShopManagement shopManager;
    
    // Cache for performance
    private bool isShopOpen = false;
    
    // Force correction interval
    private float checkInterval = 0.5f;
    private float lastCheckTime = 0f;
    
    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Find shop manager
        shopManager = FindObjectOfType<ShopManagement>();
        
        if (shopManager == null)
        {
            Debug.LogWarning("ShopWeaponBlocker: ShopManagement not found in scene!");
        }
    }
    
    void Update()
    {
        // Update the shop status every frame for accurate checking
        if (shopManager != null)
        {
            // Read the current shop state
            isShopOpen = shopManager.IsShopOpen();
        }
        else
        {
            // Try to find shop manager again if it was null
            shopManager = FindObjectOfType<ShopManagement>();
            
            // Reset shop state to false if we can't find the shop manager
            if (shopManager == null && isShopOpen)
            {
                Debug.Log("ShopWeaponBlocker: Reset shop state to false because shop manager not found");
                isShopOpen = false;
            }
        }
        
        // Extra safety checks at regular intervals for performance
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            
            // If cursor is locked but shop is still marked as open, fix the inconsistency
            if (isShopOpen && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0.1f)
            {
                Debug.Log("ShopWeaponBlocker: Detected cursor locked while shop open, resetting shop state");
                isShopOpen = false;
                
                // Also reset the shop state in ShopManagement
                if (shopManager != null)
                {
                    // Try to call CloseShop directly
                    shopManager.CloseShop();
                    
                    // Double check that it worked
                    if (shopManager.IsShopOpen())
                    {
                        // Use reflection as a last resort to force state change
                        ForceResetShopState(shopManager);
                    }
                }
            }
            
            // If cursor is unlocked but shop is marked as closed, check if UI panels are active
            if (!isShopOpen && Cursor.lockState == CursorLockMode.None && shopManager != null)
            {
                // Check if any shop panel is actually visible
                bool anyPanelActive = false;
                
                // Use reflection to check main panel state (safer than accessing directly)
                anyPanelActive = IsAnyShopPanelActive(shopManager);
                
                if (anyPanelActive)
                {
                    Debug.Log("ShopWeaponBlocker: Detected shop panel active but isShopOpen=false, fixing...");
                    isShopOpen = true;
                }
            }
        }
    }
    
    // Check if any shop panel is active via reflection
    private bool IsAnyShopPanelActive(ShopManagement shop)
    {
        // Try to get the mainShopPanel field
        FieldInfo mainPanelField = shop.GetType().GetField("mainShopPanel", 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
        if (mainPanelField != null)
        {
            GameObject mainPanel = mainPanelField.GetValue(shop) as GameObject;
            if (mainPanel != null && mainPanel.activeSelf)
            {
                return true;
            }
        }
        
        // Try to get the gunShopPanel field
        FieldInfo gunPanelField = shop.GetType().GetField("gunShopPanel", 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
        if (gunPanelField != null)
        {
            GameObject gunPanel = gunPanelField.GetValue(shop) as GameObject;
            if (gunPanel != null && gunPanel.activeSelf)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Force reset the shop state via reflection
    private void ForceResetShopState(ShopManagement shop)
    {
        FieldInfo shopOpenField = shop.GetType().GetField("isShopOpen", 
            BindingFlags.NonPublic | BindingFlags.Instance);
            
        if (shopOpenField != null)
        {
            shopOpenField.SetValue(shop, false);
            Debug.Log("ShopWeaponBlocker: Force reset ShopManagement isShopOpen state using reflection");
        }
    }
    
    /// <summary>
    /// Check if weapon firing should be blocked
    /// </summary>
    /// <returns>True if shop is open and firing should be blocked</returns>
    public static bool ShouldBlockWeaponFiring()
    {
        // If instance exists and shop is open, block firing
        if (Instance != null)
        {
            return Instance.isShopOpen;
        }
        
        // Safety fallback - if we can't access the instance, check the cursor state
        // If cursor is visible and unlocked, UI is probably open so block firing
        return Cursor.visible && Cursor.lockState == CursorLockMode.None;
    }
    
    /// <summary>
    /// Force reset the shop state to closed
    /// </summary>
    public static void ResetShopState()
    {
        if (Instance != null)
        {
            Instance.isShopOpen = false;
            Debug.Log("ShopWeaponBlocker: Forced reset of shop state");
            
            // Also make sure ShopManagement is updated
            if (Instance.shopManager != null)
            {
                // Try to call CloseShop if possible
                Instance.shopManager.CloseShop();
                
                // Double check that it worked
                if (Instance.shopManager.IsShopOpen())
                {
                    Instance.ForceResetShopState(Instance.shopManager);
                }
            }
        }
    }
}
