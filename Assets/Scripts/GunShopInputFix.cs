using UnityEngine;


public class GunShopInputFix : MonoBehaviour
{
    private bool wasShopOpen = false;
    private float stuckDetectionTime = 1.0f;
    private float lastFireAttemptTime = 0f;
    private int fireAttemptCount = 0;
    
    void Update()
    {

        ShopManagement shopManager = FindObjectOfType<ShopManagement>();
        bool shopOpen = (shopManager != null && shopManager.IsShopOpen());
        

        if (wasShopOpen && !shopOpen)
        {

            Debug.Log("GunShopInputFix: Shop was closed, ensuring weapon input is restored");
            ResetGunInputState();
        }
        

        wasShopOpen = shopOpen;
        

        if (!shopOpen && Input.GetButtonDown("Fire1"))
        {

            lastFireAttemptTime = Time.time;
            fireAttemptCount++;
            
            if (fireAttemptCount >= 3 && Time.time - lastFireAttemptTime < stuckDetectionTime)
            {
                Debug.Log("GunShopInputFix: Detected multiple fire attempts with no effect, resetting gun state");
                ResetGunInputState();
                fireAttemptCount = 0;
            }
        }
        
        if (Time.time - lastFireAttemptTime > stuckDetectionTime)
        {
            fireAttemptCount = 0;
        }
    }
    
    private void ResetGunInputState()
    {

        ShopWeaponBlocker.ResetShopState();
        

        ShopManagement shopManager = FindObjectOfType<ShopManagement>();
        if (shopManager != null)
        {
            shopManager.CloseShop();
        }
        

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        

        Time.timeScale = 1f;
    }
}
