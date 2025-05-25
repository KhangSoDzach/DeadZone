using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This component intercepts weapon input and prevents firing when shop is open
public class GunInputManager : MonoBehaviour
{
    private Gun[] allGuns;
    private ShopManagement shopManager;
    
    void Start()
    {
        // Find all guns in scene including inactive ones
        allGuns = FindObjectsOfType<Gun>(true);
        
        // Try to find the shop management script
        shopManager = FindObjectOfType<ShopManagement>();
        
        if (shopManager == null)
        {
            Debug.LogWarning("ShopManagement script not found! GunInputManager will not work properly.");
        }
    }
      // Static reference to the shop manager for better performance
    private static ShopManagement cachedShopManager;
    
    // Create a method to check if input should be blocked
    public static bool ShouldBlockWeaponInput()
    {
        // Use cached reference or find shop manager if not cached yet
        if (cachedShopManager == null)
        {
            cachedShopManager = FindObjectOfType<ShopManagement>();
        }
        
        // If shop manager exists and shop is open, block input
        if (cachedShopManager != null && cachedShopManager.IsShopOpen())
        {
            return true;
        }
        
        return false;
    }
}
