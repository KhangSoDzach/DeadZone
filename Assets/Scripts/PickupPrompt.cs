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
        // Kiểm tra scene context
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("login"))
        {
            Debug.LogWarning($"PickupPrompt should not be active in scene: {sceneName}");
            this.enabled = false;
            return;
        }

        // Kiểm tra nếu không có text UI
        if (promptText == null)
        {
            Debug.LogError("Chưa gán Text UI cho PickupPrompt");
            this.enabled = false;
            return;
        }
        
        // Tìm WeaponManager trong scene
        weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogWarning("Không tìm thấy WeaponManager trong scene");
            this.enabled = false;
            return;
        }
        
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
                // Kiểm tra nếu đang nhìn vào medkit
                MedkitPickup medkit = hit.collider.GetComponent<MedkitPickup>();
                if (medkit != null)
                {
                    // Hiển thị thông báo nhặt medkit
                    ShowMedkitPrompt(medkit);
                    return;
                }
                
                // Nếu không phải medkit, kiểm tra xem có phải vũ khí không
                WeaponPickup weaponPickup = hit.collider.GetComponent<WeaponPickup>();
                if (weaponPickup != null)
                {
                    // Hiển thị thông báo nhặt vũ khí
                    ShowPickupPrompt(weaponPickup.weaponName);
                    return;
                }
                
                // Kiểm tra trong các thành phần con
                if (medkit == null)
                {
                    medkit = hit.collider.GetComponentInChildren<MedkitPickup>();
                    if (medkit != null)
                    {
                        ShowMedkitPrompt(medkit);
                        return;
                    }
                }
                
                if (weaponPickup == null)
                {
                    weaponPickup = hit.collider.GetComponentInChildren<WeaponPickup>();
                    if (weaponPickup != null)
                    {
                        ShowPickupPrompt(weaponPickup.weaponName);
                        return;
                    }
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
    
    // Hiển thị thông báo nhặt medkit
    public void ShowMedkitPrompt(MedkitPickup medkit)
    {
        if (promptText != null)
        {
            // Tìm HealthManager của người chơi để kiểm tra nếu máu đã đầy
            HealthManager playerHealth = FindObjectOfType<HealthManager>();
            
            if (playerHealth != null && playerHealth.currentHealth >= playerHealth.maxHealth)
            {
                promptText.text = "Máu đã đầy, không cần sử dụng medkit!";
            }
            else
            {
                float healAmount = medkit.healPercent;
                promptText.text = $"Nhấn [{pickupKey}] để sử dụng medkit (Hồi {healAmount}% HP)";
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