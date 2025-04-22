using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchWeapon : MonoBehaviour
{
    public int selectedWeapon = 0; // Index of the currently selected weapon

    void Start()
    {
        SelectWeapon(); // Initialize the selected weapon
    }

    void Update()
    {
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
        
        // Kích hoạt vũ khí được chọn và vô hiệu hóa các vũ khí khác
        int i = 0;
        foreach (Transform weapon in transform)
        {
            bool shouldBeActive = (i == selectedWeapon);
            
            // Chỉ thay đổi trạng thái nếu cần thiết
            if (weapon.gameObject.activeSelf != shouldBeActive)
            {
                weapon.gameObject.SetActive(shouldBeActive);
                if (shouldBeActive)
                {
                    Debug.Log($"Đã kích hoạt vũ khí: {weapon.name} tại vị trí {i}");
                }
            }
            i++;
        }
    }
}
