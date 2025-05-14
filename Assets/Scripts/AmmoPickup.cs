using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public enum AmmoType
    {
        Pistol,
        Rifle
    }
    
    [Header("Cài đặt đạn")]
    public AmmoType ammoType = AmmoType.Pistol;
    public int ammoAmount = 30;
    
    [Header("Hiệu ứng")]
    public float rotationSpeed = 50f;
    public float floatHeight = 0.1f;
    public float floatSpeed = 1f;
    public GameObject pickupEffect; // Hiệu ứng khi nhặt đạn
    
    private Vector3 startPosition;
    
    private void Start()
    {
        startPosition = transform.position;
    }
    
    private void Update()
    {
        // Xoay hộp đạn
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Di chuyển lên xuống nhẹ
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    // Được gọi từ WeaponManager hoặc Player khi nhặt đạn
    public bool AddAmmoToGun(Gun gun)
    {
        if (gun == null) return false;
        
        // Kiểm tra loại súng để thêm đạn phù hợp
        if ((ammoType == AmmoType.Pistol && gun.isPistol) || 
            (ammoType == AmmoType.Rifle && !gun.isPistol))
        {
            // Thêm đạn vào súng
            gun.AddAmmo(ammoAmount);
            
            // Hiển thị thông báo nhặt đạn
            if (PickupDisplayManager.Instance != null)
            {
                string ammoTypeName = ammoType == AmmoType.Pistol ? "Đạn Súng Lục" : "Đạn Súng Trường";
                //PickupDisplayManager.Instance.ShowAmmoPickup(ammoTypeName, ammoAmount);
            }
            
            // Hiệu ứng khi nhặt đạn
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }
            
            return true;
        }
        
        return false;
    }
}
