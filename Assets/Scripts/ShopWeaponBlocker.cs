using UnityEngine;
using System.Reflection;


public class ShopWeaponBlocker : MonoBehaviour
{

    public static ShopWeaponBlocker Instance { get; private set; }
    

    private ShopManagement shopManager;
    

    private bool isShopOpen = false;
    

    private float checkInterval = 0.5f;
    private float lastCheckTime = 0f;
    
    void Awake()
    {

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

        shopManager = FindObjectOfType<ShopManagement>();
        
        if (shopManager == null)
        {
            Debug.LogWarning("ShopWeaponBlocker: ShopManagement not found in scene!");
        }
    }
    
    void Update()
    {

        if (shopManager != null)
        {

            isShopOpen = shopManager.IsShopOpen();
        }
        else
        {

            shopManager = FindObjectOfType<ShopManagement>();
            

            if (shopManager == null && isShopOpen)
            {
                Debug.Log("ShopWeaponBlocker: Reset shop state to false because shop manager not found");
                isShopOpen = false;
            }
        }
        

        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            

            if (isShopOpen && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0.1f)
            {
                Debug.Log("ShopWeaponBlocker: Detected cursor locked while shop open, resetting shop state");
                isShopOpen = false;
                

                if (shopManager != null)
                {

                    shopManager.CloseShop();
                    

                    if (shopManager.IsShopOpen())
                    {

                        ForceResetShopState(shopManager);
                    }
                }
            }
            

            if (!isShopOpen && Cursor.lockState == CursorLockMode.None && shopManager != null)
            {

                bool anyPanelActive = false;
                

                anyPanelActive = IsAnyShopPanelActive(shopManager);
                
                if (anyPanelActive)
                {
                    Debug.Log("ShopWeaponBlocker: Detected shop panel active but isShopOpen=false, fixing...");
                    isShopOpen = true;
                }
            }
        }
    }
    

    private bool IsAnyShopPanelActive(ShopManagement shop)
    {

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
    
    public static bool ShouldBlockWeaponFiring()
    {
        // If instance exists and shop is open, block firing
        if (Instance != null)
        {
            return Instance.isShopOpen;
        }
        

        return Cursor.visible && Cursor.lockState == CursorLockMode.None;
    }
    public static void ResetShopState()
    {
        if (Instance != null)
        {
            Instance.isShopOpen = false;
            Debug.Log("ShopWeaponBlocker: Forced reset of shop state");
            

            if (Instance.shopManager != null)
            {

                Instance.shopManager.CloseShop();
                

                if (Instance.shopManager.IsShopOpen())
                {
                    Instance.ForceResetShopState(Instance.shopManager);
                }
            }
        }
    }
}
