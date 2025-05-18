using UnityEngine;

/// <summary>
/// This component ensures that weapon firing is properly re-enabled after shop is closed
/// Add this to the player or a manager GameObject that never gets destroyed
/// </summary>
public class GunShopInputFix : MonoBehaviour
{
    private bool wasShopOpen = false;
    private float stuckDetectionTime = 1.0f;
    private float lastFireAttemptTime = 0f;
    private int fireAttemptCount = 0;
    
    void Update()
    {
        // Get shop state
        ShopManagement shopManager = FindObjectOfType<ShopManagement>();
        bool shopOpen = (shopManager != null && shopManager.IsShopOpen());
        
        // Check for shop state change
        if (wasShopOpen && !shopOpen)
        {
            // Shop was just closed, make sure everything is reset properly
            Debug.Log("GunShopInputFix: Shop was closed, ensuring weapon input is restored");
            ResetGunInputState();
        }
        
        // Keep track of previous state
        wasShopOpen = shopOpen;
        
        // If shop is closed and player is trying to fire but can't
        if (!shopOpen && Input.GetButtonDown("Fire1"))
        {
            // Store time of firing attempt
            lastFireAttemptTime = Time.time;
            fireAttemptCount++;
            
            // If player has tried to fire multiple times in a short period
            if (fireAttemptCount >= 3 && Time.time - lastFireAttemptTime < stuckDetectionTime)
            {
                Debug.Log("GunShopInputFix: Detected multiple fire attempts with no effect, resetting gun state");
                ResetGunInputState();
                fireAttemptCount = 0;
            }
        }
        
        // Reset counter after a delay
        if (Time.time - lastFireAttemptTime > stuckDetectionTime)
        {
            fireAttemptCount = 0;
        }
    }
    
    private void ResetGunInputState()
    {
        // Reset ShopWeaponBlocker state
        ShopWeaponBlocker.ResetShopState();
        
        // Force close shop just in case
        ShopManagement shopManager = FindObjectOfType<ShopManagement>();
        if (shopManager != null)
        {
            shopManager.CloseShop();
        }
        
        // Make sure cursor is locked for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Restore time scale
        Time.timeScale = 1f;
    }
}
