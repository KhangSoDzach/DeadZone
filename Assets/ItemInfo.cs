using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ShopManagement;

public class ItemInfo : MonoBehaviour
{
    public int itemID;
    public Text PriceTxt;
    public Text ItemNameTxt;
    public ShopManagement shopManager; 

    void Start()
    {
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManagement>();
        }

        ShopItem item = shopManager.shopItemsList.Find(i => i.id == itemID);
        if (item != null)
        {
            PriceTxt.text = "Price: $" + item.price;
            ItemNameTxt.text = item.name;
        }
        else
        {
            PriceTxt.text = "None";
            ItemNameTxt.text = "Unknown";
        }
    }
}
