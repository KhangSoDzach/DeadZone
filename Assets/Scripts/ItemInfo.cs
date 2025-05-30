using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static ShopManagement;

public class ItemInfo : MonoBehaviour
{
    public int itemID;
    public ShopManagement shopManager;
    
    // Các tham chiếu UI tùy chọn
    public Text priceText;
    public Text nameText;
    
    void Start()
    {
        // Tìm ShopManagement nếu không được gán
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManagement>();
        }
        
        // Cập nhật hiển thị giá và tên
        UpdateItemDisplay();
    }
    
    // Cập nhật thông tin hiển thị của mặt hàng
    private void UpdateItemDisplay()
    {
        if (shopManager != null)
        {
            ShopManagement.ShopItem item = shopManager.shopItemsList.Find(i => i.id == itemID);
            if (item != null)
            {
                if (priceText != null)
                    priceText.text = item.price.ToString() + "$";
                
                if (nameText != null)
                    nameText.text = item.name;
            }
        }
    }
    
    // Hàm này được gọi khi nút mua hàng được nhấn
    public void OnBuyButtonClick()
    {
        if (shopManager != null)
        {
            // Check if user is logged in and try to sync money with server
            if (GameAPI.Instance != null && GameAPI.Instance.IsLoggedIn)
            {
                StartCoroutine(BuyItemWithServerSync());
            }
            else
            {
                // Fallback to local purchase
                shopManager.BuyItem(itemID);
            }
        }
        else
        {
            Debug.LogError("Shop Manager không được tìm thấy!");
        }
    }
    
    private IEnumerator BuyItemWithServerSync()
    {
        // Get current server data first
        yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
            if (success && GameAPI.Instance.PlayerData != null)
            {
                // Update local money with server money
                // You'll need to implement this based on your money management system
                // For example: MoneyManager.Instance.SetMoney(GameAPI.Instance.PlayerData.money);
                
                // Proceed with purchase - BuyItem returns void, so we call it directly
                shopManager.BuyItem(itemID);
                
                // After purchase, update server with new money amount
                // Note: You'll need to get the updated money value from your money management system
                // GameAPI.Instance.PlayerData.money = MoneyManager.Instance.GetMoney();
                StartCoroutine(GameAPI.Instance.SavePlayerData((saveSuccess, saveError) => {
                    if (saveSuccess)
                    {
                        Debug.Log("Purchase saved to server successfully");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to save purchase to server: {saveError}");
                    }
                }));
            }
            else
            {
                // If server sync fails, still allow local purchase
                Debug.LogWarning("Server sync failed, proceeding with local purchase");
                shopManager.BuyItem(itemID);
            }
        }));
    }
}
