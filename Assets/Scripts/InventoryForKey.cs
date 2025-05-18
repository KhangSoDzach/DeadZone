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
        Debug.Log("Đã nhặt chìa khóa!");
    }
}
