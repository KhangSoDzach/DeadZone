using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwitchWeapon : MonoBehaviour
{
    public int selectedWeapon = 0; // Index của vũ khí đang được chọn
    
    // Vị trí lưu trữ cho các loại vũ khí
    private GameObject primaryWeapon; // Vũ khí lớn (slot 0)
    private GameObject secondaryWeapon; // Vũ khí nhỏ (slot 1)
    
    // Prefab mặc định cho vũ khí (nếu không có vũ khí nào)
    public GameObject defaultPrimaryWeapon;
    public GameObject defaultSecondaryWeapon;

    void Start()
    {
        // Khởi tạo vũ khí ban đầu
        InitializeWeapons();
        SelectWeapon(); // Kích hoạt vũ khí được chọn
    }
    
    // Khởi tạo vũ khí ban đầu từ các con của đối tượng này
    void InitializeWeapons()
    {
        bool hasPrimary = false;
        bool hasSecondary = false;
        
        // Kiểm tra các vũ khí hiện có
        foreach (Transform weapon in transform)
        {
            Gun gunScript = weapon.GetComponent<Gun>();
            if (gunScript != null)
            {
                if (gunScript.isLargeWeapon && !hasPrimary)
                {
                    primaryWeapon = weapon.gameObject;
                    hasPrimary = true;
                }
                else if (!gunScript.isLargeWeapon && !hasSecondary)
                {
                    secondaryWeapon = weapon.gameObject;
                    hasSecondary = true;
                }
                else
                {
                    // Vô hiệu hóa vũ khí dư thừa
                    weapon.gameObject.SetActive(false);
                }
            }
        }
        
        // Tạo vũ khí mặc định nếu cần
        if (!hasPrimary && defaultPrimaryWeapon != null)
        {
            GameObject newWeapon = Instantiate(defaultPrimaryWeapon, transform);
            primaryWeapon = newWeapon;
        }
        
        if (!hasSecondary && defaultSecondaryWeapon != null)
        {
            GameObject newWeapon = Instantiate(defaultSecondaryWeapon, transform);
            secondaryWeapon = newWeapon;
        }
        
        // Đảm bảo rằng tất cả các vũ khí đều bị tắt ban đầu
        if (primaryWeapon != null)
            primaryWeapon.SetActive(false);
        if (secondaryWeapon != null)
            secondaryWeapon.SetActive(false);
    }

    void Update()
    {
        // Nếu không có vũ khí nào, không thực hiện chuyển đổi vũ khí
        if (primaryWeapon == null && secondaryWeapon == null)
            return;
            
        int previousSelectedWeapon = selectedWeapon;

        // Chỉ xử lý cuộn chuột nếu có ít nhất 2 vũ khí
        if (primaryWeapon != null && secondaryWeapon != null)
        {
            // Chuyển đổi vũ khí bằng con lăn chuột
            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
            {
                selectedWeapon = (selectedWeapon + 1) % 2; // Chỉ có 2 slot vũ khí
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
            {
                selectedWeapon = (selectedWeapon - 1 + 2) % 2; // Chỉ có 2 slot vũ khí
            }
        }

        // Chuyển đổi vũ khí bằng các phím số - chỉ khi vũ khí tương ứng tồn tại
        if (Input.GetKeyDown(KeyCode.Alpha1) && primaryWeapon != null)
        {
            selectedWeapon = 0; // Vũ khí lớn
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && secondaryWeapon != null)
        {
            selectedWeapon = 1; // Vũ khí nhỏ
        }

        // Nếu vũ khí đã thay đổi, cập nhật vũ khí hiện tại
        if (previousSelectedWeapon != selectedWeapon)
        {
            SelectWeapon();
        }

        // Phím G để thả vũ khí hiện tại
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropCurrentWeapon();
        }
    }
    
    // Kích hoạt vũ khí được chọn và vô hiệu hóa các vũ khí khác
    void SelectWeapon()
    {
        // Xử lý trường hợp không có vũ khí nào
        if (primaryWeapon == null && secondaryWeapon == null)
            return;
            
        // Chuyển đổi sang vũ khí có sẵn nếu vũ khí hiện tại không tồn tại
        if (selectedWeapon == 0 && primaryWeapon == null && secondaryWeapon != null)
        {
            selectedWeapon = 1;
        }
        else if (selectedWeapon == 1 && secondaryWeapon == null && primaryWeapon != null)
        {
            selectedWeapon = 0;
        }
            
        // Chỉ kích hoạt vũ khí đang được chọn
        if (selectedWeapon == 0) // Vũ khí lớn
        {
            if (primaryWeapon != null)
                primaryWeapon.SetActive(true);
            if (secondaryWeapon != null)
                secondaryWeapon.SetActive(false);
        }
        else // Vũ khí nhỏ
        {
            if (primaryWeapon != null)
                primaryWeapon.SetActive(false);
            if (secondaryWeapon != null)
                secondaryWeapon.SetActive(true);
        }
    }
    
    // Xử lý khi nhặt một vũ khí mới (nhận Gun script thay vì WeaponPickup)
    public void PickupWeapon(Gun pickupGun)
    {
        if (string.IsNullOrEmpty(pickupGun.weaponName))
        {
            Debug.LogError("Weapon name is empty, cannot load prefab");
            return;
        }
        
        Debug.Log("Attempting to load prefab: " + pickupGun.weaponName);
        
        // Tạo một GameObject mới cho vũ khí dựa trên tên vũ khí
        GameObject weaponPrefab = null;
        
        // Thử tìm prefab bằng tên chính xác
        weaponPrefab = Resources.Load<GameObject>(pickupGun.weaponName);
        
        // Nếu không tìm thấy, thử tìm bằng cách khác
        if (weaponPrefab == null)
        {
            // Thử một số tên phổ biến của súng
            string[] commonNames = {"AK - 47", "Pistol", "AK-47", "AK47", "Gun", "Rifle"};
            foreach (string name in commonNames)
            {
                weaponPrefab = Resources.Load<GameObject>(name);
                if (weaponPrefab != null)
                {
                    Debug.Log("Found weapon prefab with alternative name: " + name);
                    break;
                }
            }
        }
        
        // Nếu vẫn không tìm thấy, hiển thị danh sách tất cả các prefab trong Resources để debug
        if (weaponPrefab == null)
        {
            Debug.LogError("Cannot find weapon prefab in Resources: " + pickupGun.weaponName);
            // Hiển thị danh sách tất cả các prefab trong Resources để debug
            Object[] allResources = Resources.LoadAll("");
            Debug.Log("Available resources in Resources folder:");
            foreach (Object obj in allResources)
            {
                Debug.Log("- " + obj.name);
            }
            return;
        }
        
        GameObject newWeapon = Instantiate(weaponPrefab, transform);
        Gun gunScript = newWeapon.GetComponent<Gun>();
        
        if (gunScript != null)
        {
            // Sao chép các thuộc tính từ vũ khí được nhặt (pickup)
            gunScript.currentAmmo = pickupGun.currentAmmo;
            gunScript.maxAmmo = pickupGun.maxAmmo;
            gunScript.damage = pickupGun.damage;
            gunScript.isLargeWeapon = pickupGun.isLargeWeapon;
            gunScript.weaponName = pickupGun.weaponName;
            gunScript.range = pickupGun.range;
            gunScript.isAutomatic = pickupGun.isAutomatic;
            gunScript.reloadTime = pickupGun.reloadTime;
            gunScript.impactForce = pickupGun.impactForce;
            gunScript.fireRate = pickupGun.fireRate;
            
            // QUAN TRỌNG: Sao chép tham chiếu đến prefab vũ khí khi rớt xuống đất
            // Đây là lý do tại sao không thể quăng lại vũ khí đã quăng
            gunScript.weaponDropPrefab = pickupGun.weaponDropPrefab;
            
            // ĐẶC BIỆT QUAN TRỌNG: Sao chép tham chiếu đến animator
            // Giữ lại animator từ vũ khí vừa tạo
            Animator newAnimator = newWeapon.GetComponent<Animator>();
            
            // Nếu vũ khí nhặt có animator, sao chép trạng thái animator
            if (pickupGun.animator != null && newAnimator != null)
            {
                // Lưu trữ tham chiếu đến animator trong gunScript
                gunScript.animator = newAnimator;
            }
            
            // Đảm bảo có tham chiếu đến camera để bắn
            if (gunScript.playerCamera == null)
            {
                gunScript.playerCamera = Camera.main;
            }
            
            // Đảm bảo có tham chiếu đến các UI elements
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                // Tìm Text components cho ammo và score
                Text[] allTexts = mainCanvas.GetComponentsInChildren<Text>(true);
                foreach (Text text in allTexts)
                {
                    if (text.name.Contains("Ammo") || text.name.ToLower().Contains("ammo"))
                    {
                        gunScript.ammoText = text;
                    }
                    else if (text.name.Contains("Score") || text.name.ToLower().Contains("score"))
                    {
                        gunScript.scoreText = text;
                    }
                }
            }
            
            // Đảm bảo có các hiệu ứng cần thiết
            if (gunScript.muzzleFlash == null)
            {
                // Tìm muzzle flash từ các con của vũ khí
                gunScript.muzzleFlash = newWeapon.GetComponentInChildren<ParticleSystem>();
            }
            
            if (gunScript.gunshotSound == null)
            {
                // Tìm audio source từ vũ khí
                gunScript.gunshotSound = newWeapon.GetComponent<AudioSource>();
                if (gunScript.gunshotSound == null)
                {
                    // Nếu không có, tạo mới
                    gunScript.gunshotSound = newWeapon.AddComponent<AudioSource>();
                    // Sao chép AudioClip từ vũ khí pickup nếu có
                    if (pickupGun.gunshotSound != null && pickupGun.gunshotSound.clip != null)
                    {
                        gunScript.gunshotSound.clip = pickupGun.gunshotSound.clip;
                        gunScript.gunshotSound.volume = pickupGun.gunshotSound.volume;
                        gunScript.gunshotSound.pitch = pickupGun.gunshotSound.pitch;
                    }
                }
            }
            
            // Xử lý dựa trên loại vũ khí
            if (pickupGun.isLargeWeapon)
            {
                // Tắt vũ khí lớn cũ nếu có
                if (primaryWeapon != null && primaryWeapon != newWeapon)
                {
                    Destroy(primaryWeapon);
                }
                
                // Lưu vũ khí lớn mới
                primaryWeapon = newWeapon;
                
                // Tự động chuyển sang vũ khí lớn mới
                selectedWeapon = 0;
            }
            else // Vũ khí nhỏ
            {
                // Tắt vũ khí nhỏ cũ nếu có
                if (secondaryWeapon != null && secondaryWeapon != newWeapon)
                {
                    Destroy(secondaryWeapon);
                }
                
                // Lưu vũ khí nhỏ mới
                secondaryWeapon = newWeapon;
                
                // Tự động chuyển sang vũ khí nhỏ mới
                selectedWeapon = 1;
            }
            
            // Kích hoạt vũ khí mới
            SelectWeapon();
            
            // Thông báo nhặt vũ khí thành công
            Debug.Log("Picked up " + pickupGun.weaponName + " with weaponDropPrefab: " + (gunScript.weaponDropPrefab != null ? gunScript.weaponDropPrefab.name : "null"));
        }
        else
        {
            Debug.LogError("Weapon prefab doesn't have Gun component: " + pickupGun.weaponName);
            Destroy(newWeapon); // Destroy the newly spawned object if it doesn't have Gun component
        }
    }
    
    // Xử lý khi thả một vũ khí
    public void DropCurrentWeapon()
    {
        GameObject weaponToDrop = null;
        
        // Xác định vũ khí đang được sử dụng
        if (selectedWeapon == 0 && primaryWeapon != null)
        {
            weaponToDrop = primaryWeapon;
            primaryWeapon = null; // Quan trọng: Gán null trước khi thả
        }
        else if (selectedWeapon == 1 && secondaryWeapon != null)
        {
            weaponToDrop = secondaryWeapon;
            secondaryWeapon = null; // Quan trọng: Gán null trước khi thả
        }
        else
        {
            return; // Không có vũ khí nào để thả
        }
        
        if (weaponToDrop != null)
        {
            // Lấy component Gun từ vũ khí
            Gun gunScript = weaponToDrop.GetComponent<Gun>();
            if (gunScript != null)
            {
                // Gọi phương thức DropWeapon của Gun
                gunScript.DropWeapon();
                
                Debug.Log("Dropped weapon: " + weaponToDrop.name);
            }
            
            // Chuyển sang vũ khí còn lại nếu có
            if (primaryWeapon != null)
            {
                selectedWeapon = 0;
            }
            else if (secondaryWeapon != null)
            {
                selectedWeapon = 1;
            }
            
            // Cập nhật vũ khí hiện tại
            SelectWeapon();
        }
    }
    
    // Phương thức để loại bỏ vũ khí theo GameObject
    public void RemoveCurrentWeapon(GameObject weaponToRemove)
    {
        // Kiểm tra xem weaponToRemove có phải là vũ khí lớn hiện tại không
        if (primaryWeapon == weaponToRemove)
        {
            primaryWeapon = null; // Quan trọng: Đặt tham chiếu thành null
            
            // Chuyển sang vũ khí nhỏ nếu có
            if (secondaryWeapon != null)
            {
                selectedWeapon = 1;
            }
        }
        // Kiểm tra xem weaponToRemove có phải là vũ khí nhỏ hiện tại không
        else if (secondaryWeapon == weaponToRemove)
        {
            secondaryWeapon = null; // Quan trọng: Đặt tham chiếu thành null
            
            // Chuyển sang vũ khí lớn nếu có
            if (primaryWeapon != null)
            {
                selectedWeapon = 0;
            }
        }
        
        // Cập nhật vũ khí hiện tại
        SelectWeapon();
    }
}
