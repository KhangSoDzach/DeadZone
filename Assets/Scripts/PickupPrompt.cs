using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickupPrompt : MonoBehaviour
{
    public Text promptText; // Text UI để hiển thị thông báo
    public KeyCode pickupKey = KeyCode.E; // Phím để nhặt vũ khí
    public KeyCode dropKey = KeyCode.G; // Phím để vứt vũ khí
    
    private WeaponManager weaponManager;
    
    private void Start()
    {
        // Kiểm tra nếu không có text UI
        if (promptText == null)
        {
            Debug.LogError("Chưa gán Text UI cho PickupPrompt");
        }
        
        // Tìm WeaponManager trong scene
        weaponManager = FindObjectOfType<WeaponManager>();
        
        // Ban đầu ẩn thông báo
        HidePrompt();
    }
    
    private void Update()
    {
        // Kiểm tra xem người chơi có đang nhìn vào vũ khí có thể nhặt không
        if (weaponManager != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, weaponManager.pickupRange, weaponManager.pickupLayer))
            {
                WeaponPickup weaponPickup = hit.collider.GetComponent<WeaponPickup>();
                if (weaponPickup != null)
                {
                    // Hiển thị thông báo nhặt vũ khí
                    ShowPickupPrompt(weaponPickup.weaponName);
                    return;
                }
            }
            
            // Nếu có vũ khí trong tay và không phải vũ khí lục, hiển thị gợi ý vứt vũ khí
            if (weaponManager.GetCurrentWeaponIsDroppable())
            {
                ShowDropPrompt();
                return;
            }
            
            // Nếu không nhìn vào vũ khí có thể nhặt, ẩn thông báo
            HidePrompt();
        }
    }
    
    // Hiển thị thông báo nhặt vũ khí
    public void ShowPickupPrompt(string weaponName)
    {
        if (promptText != null)
        {
            WeaponPickup pickup = FindPickupInFront();
            if (pickup != null)
            {
                string ammoInfo = pickup.remainingAmmo > 0 ? $" | Đạn: {pickup.remainingAmmo}" : "";
                string damageInfo = pickup.damage > 0 ? $" | Sát thương: {pickup.damage}" : "";
                string typeInfo = pickup.isAutomatic ? " | Tự động" : " | Bán tự động";
                
                promptText.text = $"Nhấn [{pickupKey}] để nhặt {weaponName}{ammoInfo}{damageInfo}{typeInfo}";
            }
            else
            {
                promptText.text = $"Nhấn [{pickupKey}] để nhặt {weaponName}";
            }
            promptText.gameObject.SetActive(true);
        }
    }
    
    // Phương thức helper để tìm WeaponPickup đang được nhìn
    private WeaponPickup FindPickupInFront()
    {
        if (weaponManager != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, 
                                out hit, weaponManager.pickupRange, weaponManager.pickupLayer))
            {
                return hit.collider.GetComponent<WeaponPickup>();
            }
        }
        return null;
    }
    
    // Hiển thị thông báo vứt vũ khí
    public void ShowDropPrompt()
    {
        if (promptText != null)
        {
            promptText.text = $"Nhấn [{dropKey}] để vứt vũ khí hiện tại";
            promptText.gameObject.SetActive(true);
        }
    }
    
    // Ẩn thông báo
    public void HidePrompt()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }
}