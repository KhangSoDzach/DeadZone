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
                    priceText.text = item.price.ToString() + "đ";
                
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
            shopManager.BuyItem(itemID);
        }
        else
        {
            Debug.LogError("Shop Manager không được tìm thấy!");
        }
    }
}
