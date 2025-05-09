using System.Collections;
using System.Collections.Generic;
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
    public float money;
    public Text moneyTxt;

    void Start()
    {
        moneyTxt.text = "Coins: " + money.ToString();

        shopItemsList.Add(new ShopItem { id = 1, name = "First Aid", price = 300 });
        shopItemsList.Add(new ShopItem { id = 2, name = "Pistol Ammo", price = 200 });
        shopItemsList.Add(new ShopItem { id = 3, name = "Rifle Ammo", price = 300 });
    }

    public void Buy()
    {
        GameObject buttonPoint = GameObject.FindGameObjectWithTag("Event").GetComponent<EventSystem>().currentSelectedGameObject;   
        int itemID = buttonPoint.GetComponent<ItemInfo>().itemID;

        ShopItem item = shopItemsList.Find(i => i.id == itemID);
        if (item != null && money >= item.price)
        {
            money -= item.price;
            moneyTxt.text = "Coins: " + money.ToString();
            Debug.Log("Bought: " + item.name);
        }
        else
        {
            Debug.Log("Not enough coins");
        }
    }
}
