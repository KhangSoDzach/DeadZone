using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryForKey : MonoBehaviour
{
    public static InventoryForKey Instance;
    public bool hasKey = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PickUpKey()
    {
        hasKey = true;
        // Đồng bộ với PlayerData nếu có
        if (GameAPI.Instance != null && GameAPI.Instance.PlayerData != null)
        {
            GameAPI.Instance.PlayerData.hasKey = true;
        }
    }

    // Hàm để đồng bộ trạng thái từ PlayerData sang InventoryForKey
    public void SyncFromPlayerData()
    {
        if (GameAPI.Instance != null && GameAPI.Instance.PlayerData != null)
        {
            hasKey = GameAPI.Instance.PlayerData.hasKey;
        }
    }
}
