using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scripts.API; // Add this using statement

public class SwitchWeapon : MonoBehaviour
{
    public int selectedWeapon = 0; // Index of the currently selected weapon

    void Start()
    {
        SelectWeapon(); // Initialize the selected weapon
    }

    void Update()
    {
        // Don't process weapon switching if game is paused
        if (PauseMenu.IsGamePaused)
        {
            return;
        }

        int previousSelectedWeapon = selectedWeapon;

        // Switch weapon using scroll wheel
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            selectedWeapon = (selectedWeapon + 1) % transform.childCount;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            selectedWeapon = (selectedWeapon - 1 + transform.childCount) % transform.childCount;
        }

        // Switch weapon using number keys
        if (Input.GetKeyDown(KeyCode.Alpha1) && transform.childCount >= 1)
        {
            selectedWeapon = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && transform.childCount >= 2)
        {
            selectedWeapon = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && transform.childCount >= 3)
        {
            selectedWeapon = 2;
        }

        // If the selected weapon has changed, update the active weapon
        if (previousSelectedWeapon != selectedWeapon)
        {
            SelectWeapon();
        }
        
        // Kiểm tra trong Shop đóng lại có cần reload lại súng hay không
        ShopManagement shopManager = FindObjectOfType<ShopManagement>();
        if (shopManager != null && !shopManager.IsShopOpen())
        {
            // Check if there's a flag indicating we just closed the shop
            if (shopManager.GetComponent<ShopStateTracker>() == null)
            {
                // Add a tracker component
                ShopStateTracker tracker = shopManager.gameObject.AddComponent<ShopStateTracker>();
                tracker.wasOpen = true;
            }
            else if (shopManager.GetComponent<ShopStateTracker>().wasOpen)
            {
                // Reset the flag
                shopManager.GetComponent<ShopStateTracker>().wasOpen = false;
                // Force reselect the weapon to enable it properly
                ForceSelectCurrentWeapon();
            }
        }
    }

    public void SelectWeapon()
    {
        // Safety check: ensure we have weapons to select
        if (transform.childCount == 0)
        {
            Debug.LogWarning("Không có vũ khí nào trong weaponHolder để chọn!");
            return;
        }
        
        // Đảm bảo selectedWeapon nằm trong phạm vi hợp lệ
        if (selectedWeapon >= transform.childCount)
        {
            Debug.LogWarning($"selectedWeapon ({selectedWeapon}) vượt quá số lượng vũ khí hiện có ({transform.childCount}). Điều chỉnh về 0.");
            selectedWeapon = 0;
        }
        else if (selectedWeapon < 0)
        {
            Debug.LogWarning($"selectedWeapon ({selectedWeapon}) nhỏ hơn 0. Điều chỉnh về 0.");
            selectedWeapon = 0;
        }
        
        // Lưu camera chính để gán lại nếu cần
        Camera mainCamera = Camera.main;
        
        // Kích hoạt vũ khí được chọn và vô hiệu hóa các vũ khí khác
        int i = 0;
        foreach (Transform weapon in transform)
        {
            bool shouldBeActive = (i == selectedWeapon);
            
            // Chuẩn bị vũ khí trước khi thay đổi trạng thái
            WeaponComponentRestore componentRestore = weapon.GetComponent<WeaponComponentRestore>();
            
            if (!shouldBeActive && weapon.gameObject.activeSelf)
            {
                // Chuẩn bị vũ khí trước khi vô hiệu hóa
                if (componentRestore != null)
                {
                    componentRestore.PrepareForDrop();
                    Debug.Log($"Đã chuẩn bị vũ khí {weapon.name} trước khi vô hiệu hóa.");
                }
            }
            
            // Chỉ thay đổi trạng thái nếu cần thiết
            if (weapon.gameObject.activeSelf != shouldBeActive)
            {
                weapon.gameObject.SetActive(shouldBeActive);
                
                if (shouldBeActive)
                {
                    // Đảm bảo vũ khí sẽ có vị trí chuẩn khi được kích hoạt
                    if (componentRestore == null)                       
                    {
                        // Nếu không có WeaponComponentRestore, tạo mới và thiết lập vị trí mặc định
                        componentRestore = weapon.gameObject.AddComponent<WeaponComponentRestore>();
                        Debug.Log($"Đã thêm WeaponComponentRestore cho {weapon.name}");
                        
                        // Chỉ đặt vị trí mặc định nếu vũ khí đang ở vị trí 0,0,0 (có thể là vũ khí mới)
                        if (weapon.localPosition == Vector3.zero && weapon.localEulerAngles == Vector3.zero)
                        {
                            Debug.Log($"Vũ khí {weapon.name} có vị trí 0, thiết lập các thông số mặc định");
                            weapon.localPosition = Vector3.zero;
                            weapon.localRotation = Quaternion.identity;
                            weapon.localScale = Vector3.one;
                        }
                        
                        // Lưu vị trí hiện tại làm vị trí chuẩn
                        componentRestore.StoreCurrentTransformAsCorrect();
                    }
                    else
                    {
                        // Đảm bảo vị trí được reset về vị trí đã lưu trước đó
                        componentRestore.ResetPosition();
                    }
                    
                    // Thông báo cho vũ khí rằng nó đã được kích hoạt
                    Gun gunComponent = weapon.GetComponent<Gun>();
                    if (gunComponent != null)
                    {
                        // Đảm bảo camera được gán chính xác
                        if (gunComponent.playerCamera == null && mainCamera != null)
                        {
                            gunComponent.playerCamera = mainCamera;
                        }
                        
                        gunComponent.OnWeaponEnabled();
                        
                        // Force refresh animator sau khi weapon được kích hoạt
                        if (gunComponent.animator != null)
                        {
                            gunComponent.animator.enabled = false;
                            gunComponent.animator.enabled = true;
                            gunComponent.animator.Rebind();
                            gunComponent.animator.Update(0);
                            
                            // Reset về trạng thái idle
                            gunComponent.animator.SetBool("Reloading", false);
                        }
                        
                        // Đảm bảo WeaponComponentRestore cập nhật lại tất cả các thành phần
                        if (componentRestore != null)
                        {
                            componentRestore.RestoreGunComponents(gunComponent);
                        }
                    }
                    
                    Debug.Log($"Đã kích hoạt vũ khí: {weapon.name} tại vị trí {i} với tọa độ: {weapon.localPosition}");
                }
            }
            i++;
        }
    }

    // Phương thức để bắt buộc chọn lại vũ khí hiện tại (sau khi đóng cửa hàng)
    public void ForceSelectCurrentWeapon()
    {
        Debug.Log("Áp dụng Force Select sau khi rời cửa hàng");
        SelectWeapon(); // Chỉ gọi lại phương thức SelectWeapon để refreshq
    }
}

// Class nhỏ để theo dõi trạng thái cửa hàng
public class ShopStateTracker : MonoBehaviour
{
    public bool wasOpen = false;
}
