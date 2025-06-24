using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Scripts.API; // Add this using statement

public class WeaponManager : MonoBehaviour
{
    [Header("Cài đặt quản lý vũ khí")]
    public Transform weaponHolder; // Vị trí chứa vũ khí đang được trang bị
    public Transform dropPoint; // Vị trí vứt vũ khí
    public float dropForce = 5f; // Lực vứt vũ khí
    public float pickupRange = 10f; // Khoảng cách có thể nhặt vũ khí
    public LayerMask pickupLayer; // Layer của vật phẩm có thể nhặt
    
    [Header("Input")]
    public KeyCode dropKey = KeyCode.G; // Phím để vứt vũ khí
    public KeyCode pickupKey = KeyCode.E; // Phím để nhặt vũ khí
    
    [Header("Prefabs")]
    public List<GameObject> weaponPrefabs; // Prefabs của vũ khí hợp nhất
    
    [Header("Primary Weapon Transform")]
    public Vector3 primaryPosition = Vector3.zero;
    public Quaternion primaryRotation = Quaternion.identity;
    public Vector3 primaryScale = Vector3.one;

    // Dictionary để map giữa tên vũ khí và index trong mảng prefab
    private Dictionary<string, int> weaponNameToIndex = new Dictionary<string, int>();
    
    private Camera playerCamera;
    private Gun currentGun;
    private GameObject currentWeapon;
    private SwitchWeapon switchWeapon;
    
    // Properties for PlayerDataManager access
    public Gun CurrentGun { get { return currentGun; } }
    
    // Dictionary to store ammo counts by type
    private Dictionary<string, int> ammoByType = new Dictionary<string, int>() {
        { "pistol", 30 },
        { "rifle", 0 }
    };

    private void Start()
    {
        // Check if we're in a gameplay scene before initializing
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("login"))
        {
            Debug.LogWarning($"WeaponManager should not be active in scene: {sceneName}");
            this.enabled = false;
            return;
        }

        Debug.Log("Khởi tạo WeaponManager...");

        // Kiểm tra số lượng prefab
        if (weaponPrefabs.Count == 0)
        {
            Debug.LogError("Không có prefab vũ khí nào được đăng ký trong weaponPrefabs!");
        }
        else
        {
            Debug.Log($"Số lượng prefab vũ khí: {weaponPrefabs.Count}");
        }

        // Khởi tạo dictionary ánh xạ tên vũ khí với index
        for (int i = 0; i < weaponPrefabs.Count; i++)
        {
            if (weaponPrefabs[i] != null)
            {
                string weaponName = weaponPrefabs[i].name;
                weaponNameToIndex[weaponName] = i;
                Debug.Log($"Đăng ký vũ khí: {weaponName} -> index {i}");
            }
            else
            {
                Debug.LogError($"Prefab vũ khí tại index {i} là null!");
            }
        }
        
        // Lấy reference đến camera
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogWarning("Không tìm thấy camera chính! WeaponManager sẽ tìm kiếm camera khi cần thiết.");
        }
        
        // Lấy reference đến SwitchWeapon nếu có
        switchWeapon = GetComponent<SwitchWeapon>();
        if (switchWeapon == null)
        {
            Debug.LogWarning("Không tìm thấy component SwitchWeapon trên đối tượng này. Chức năng chuyển đổi vũ khí có thể bị giới hạn.");
        }
        
        // Nếu không có dropPoint, tạo một điểm drop mặc định chỉ khi có camera
        if (dropPoint == null && playerCamera != null)
        {
            Debug.Log("Không có dropPoint được chỉ định, tạo dropPoint mặc định.");
            GameObject dropPointObj = new GameObject("DropPoint");
            dropPointObj.transform.parent = playerCamera.transform;
            dropPointObj.transform.localPosition = new Vector3(0, -0.5f, 1f);
            dropPoint = dropPointObj.transform;
        }
        
        // Đảm bảo vũ khí đã có trong inspector không bị tắt khi khởi động - chỉ khi có weaponHolder
        if (weaponHolder != null && weaponHolder.childCount > 0)
        {
            PreserveExistingWeapons();
        }
    }
    
    // Phương thức mới để bảo tồn vũ khí đã được set trong Inspector
    private void PreserveExistingWeapons()
    {
        bool foundActiveWeapon = false;
        
        // Kiểm tra xem có vũ khí nào đang active không
        for (int i = 0; i < weaponHolder.childCount; i++)
        {
            GameObject weapon = weaponHolder.GetChild(i).gameObject;
            if (weapon.activeSelf)
            {
                foundActiveWeapon = true;
                // Cập nhật tham chiếu đến vũ khí hiện tại
                currentWeapon = weapon;
                currentGun = weapon.GetComponent<Gun>();
                
                // Cập nhật selectedWeapon trong SwitchWeapon
                if (switchWeapon != null)
                {
                    switchWeapon.selectedWeapon = i;
                    Debug.Log($"Đã tìm thấy vũ khí active trong Inspector: {weapon.name}, index: {i}");
                }
                break;
            }
        }
        
        // Nếu không có vũ khí nào đang active, kích hoạt vũ khí đầu tiên
        if (!foundActiveWeapon && weaponHolder.childCount > 0)
        {
            int indexToActivate = 0; // Mặc định kích hoạt vũ khí đầu tiên
            
            // Tìm súng chính (non-pistol) để kích hoạt nếu có
            for (int i = 0; i < weaponHolder.childCount; i++)
            {
                Gun gunComponent = weaponHolder.GetChild(i).GetComponent<Gun>();
                if (gunComponent != null && !gunComponent.isPistol)
                {
                    indexToActivate = i;
                    break;
                }
            }
            
            // Kích hoạt vũ khí
            GameObject weaponToActivate = weaponHolder.GetChild(indexToActivate).gameObject;
            weaponToActivate.SetActive(true);
            currentWeapon = weaponToActivate;
            currentGun = weaponToActivate.GetComponent<Gun>();
            
            // Cập nhật selectedWeapon trong SwitchWeapon
            if (switchWeapon != null)
            {
                switchWeapon.selectedWeapon = indexToActivate;
                Debug.Log($"Không tìm thấy vũ khí active, đã kích hoạt vũ khí: {weaponToActivate.name}, index: {indexToActivate}");
            }
        }
    }
    
    // Phương thức đảm bảo rằng playerCamera luôn có sẵn
    private bool EnsurePlayerCamera()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("playerCamera là null, đang tìm kiếm Camera.main...");
            playerCamera = Camera.main;
            
            if (playerCamera == null)
            {
                // Nếu vẫn không tìm thấy, thử tìm kiếm camera trong scene
                Camera[] allCameras = FindObjectsOfType<Camera>();
                if (allCameras.Length > 0)
                {
                    playerCamera = allCameras[0];
                    //Debug.Log("Đã tìm thấy camera thay thế.");
                }
            }
        }
        
        return playerCamera != null;
    }
    
    // Phương thức cập nhật camera trước mỗi lần sử dụng
    private void UpdateReferences()
    {
        if (playerCamera == null)
        {
            EnsurePlayerCamera();
        }
        
        if (switchWeapon == null)
        {
            switchWeapon = GetComponent<SwitchWeapon>();
        }
    }
    
    private void Update()
    {
        // Don't process weapon management if game is paused
        if (PauseMenu.IsGamePaused)
        {
            return;
        }

        // Kiểm tra và cập nhật các tham chiếu trước khi sử dụng
        UpdateReferences();
        
        // Lấy vũ khí hiện tại
        GetCurrentWeapon();
        
        // Xử lý vứt vũ khí
        if (Input.GetKeyDown(dropKey) && currentWeapon != null && GetCurrentWeaponIsDroppable())
        {
            DropCurrentWeapon();
        }
        
        // Xử lý nhặt vũ khí
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupWeapon();
        }
    }
    
    // Lấy vũ khí hiện tại đang được sử dụng
    private void GetCurrentWeapon()
    {
        // Nếu có SwitchWeapon, lấy vũ khí đang active
        if (switchWeapon != null && weaponHolder != null)
        {
            foreach (Transform child in weaponHolder)
            {
                if (child.gameObject.activeSelf)
                {
                    currentWeapon = child.gameObject;
                    currentGun = child.GetComponent<Gun>();
                    return;
                }
            }
        }
        else
        {
            // Nếu không có SwitchWeapon, lấy vũ khí đầu tiên trong weaponHolder
            if (weaponHolder != null && weaponHolder.childCount > 0)
            {
                currentWeapon = weaponHolder.GetChild(0).gameObject;
                currentGun = currentWeapon.GetComponent<Gun>();
            }
        }
    }
    
    // Vứt vũ khí hiện tại xuống
    public void DropCurrentWeapon()
    {
        if (currentWeapon == null) 
        {
            Debug.LogWarning("Không thể vứt vũ khí vì currentWeapon là null!");
            return;
        }
        
        // Lưu WeaponComponentRestore component nếu có
        WeaponComponentRestore componentRestore = currentWeapon.GetComponent<WeaponComponentRestore>();
        if (componentRestore != null)
        {
            // Lưu transform hiện tại trước khi vứt vũ khí
            componentRestore.StoreCurrentTransformAsCorrect();
            componentRestore.PrepareForDrop();
           // Debug.Log($"Đã lưu transform hiện tại của vũ khí {currentWeapon.name} trước khi vứt");
        }
        
        // Kiểm tra và đảm bảo camera có sẵn
        if (!EnsurePlayerCamera())
        {
            Debug.LogError("Không thể tìm thấy camera, không thể vứt vũ khí!");
            return;
        }
        
        // Check if dropPoint is null
        if (dropPoint == null)
        {
            Debug.LogWarning("dropPoint là null! Đang tạo dropPoint mới...");
            GameObject dropPointObj = new GameObject("DropPoint");
            dropPointObj.transform.parent = playerCamera.transform;
            dropPointObj.transform.localPosition = new Vector3(0, -0.5f, 1f);
            dropPoint = dropPointObj.transform;
        }
        
        // Kiểm tra nếu đây là vũ khí cuối cùng thì không cho vứt
        if (weaponHolder == null || weaponHolder.childCount <= 1)
        {
           // Debug.Log("Không thể vứt vũ khí cuối cùng!");
            return;
        }
        
        // Kiểm tra xem vũ khí hiện tại có phải là súng lục không bằng Gun.isPistol
        Gun currentGunComponent = currentWeapon.GetComponent<Gun>();
        if (currentGunComponent != null && currentGunComponent.isPistol)
        {
          //  Debug.Log("Không thể vứt súng lục (vũ khí thứ cấp)!");
            return;
        }
        
        // Kiểm tra xác thực lần nữa để đảm bảo rằng vũ khí này có thể vứt
        if (!GetCurrentWeaponIsDroppable())
        {
           // Debug.Log("Vũ khí hiện tại không thể vứt!");
            return;
        }
        
        // Lấy tên của vũ khí để tìm prefab tương ứng
        string weaponName = currentWeapon.name;
        if (weaponName.Contains("(Clone)"))
        {
            weaponName = weaponName.Replace("(Clone)", "");
        }
        
     //   Debug.Log($"Đang vứt vũ khí: {weaponName}");
        
        // Tìm prefab index dựa trên tên vũ khí
        int prefabIndex = -1;
        
        // Kiểm tra chính xác tên vũ khí
        if (weaponNameToIndex.TryGetValue(weaponName, out prefabIndex))
        {
         //   Debug.Log($"Tìm thấy prefab index: {prefabIndex}");
        }
        // Thử với phương thức tìm kiếm mềm hơn
        else
        {
            foreach (var key in weaponNameToIndex.Keys)
            {
                if (key.ToLower().Contains(weaponName.ToLower()) || 
                    weaponName.ToLower().Contains(key.ToLower()))
                {
                    prefabIndex = weaponNameToIndex[key];
                 //   Debug.Log($"Tìm thấy prefab tương tự: {key} với index {prefabIndex}");
                    break;
                }
            }
        }
        
        // Nếu vẫn không tìm thấy, thử với phương pháp khác
        if (prefabIndex == -1)
        {
            Debug.LogWarning($"Không tìm thấy prefab cho vũ khí: {weaponName} - Đang thử phương pháp khác");
            
            // Thử dùng index đầu tiên nếu có
            if (weaponPrefabs.Count > 0)
            {
                prefabIndex = 0;  // Dùng vũ khí đầu tiên trong danh sách
                Debug.LogWarning($"Sử dụng vũ khí đầu tiên trong danh sách với index {prefabIndex}");
            }
            else
            {
                Debug.LogError("Không có vũ khí nào trong weaponPrefabs!");
                return;
            }
        }
        
        // Kiểm tra index có hợp lệ không
        if (prefabIndex < 0 || prefabIndex >= weaponPrefabs.Count)
        {
            Debug.LogError($"Index không hợp lệ: {prefabIndex}. Số lượng prefab: {weaponPrefabs.Count}");
            return;
        }
        
        // Lưu tham chiếu đến GameObject hiện tại trước khi xóa
        GameObject weaponToDestroy = currentWeapon;
        
        // Lưu thông tin về số lượng vũ khí còn lại trước khi xóa
        int remainingWeapons = weaponHolder.childCount - 1;
        
        // Lưu tham chiếu đến switchWeapon trước khi có thể bị thay đổi
        SwitchWeapon switchWeaponRef = this.switchWeapon;
        
        // Tìm súng lục TRƯỚC khi xóa vũ khí hiện tại
        int pistolIndex = FindPistolIndex();
        bool hasPistol = pistolIndex != -1;
        
        // Store a local reference to currentGunComponent before nulling references
        Gun gunComponentRef = currentGunComponent;
        
        // Đặt currentWeapon và currentGun thành null để tránh tham chiếu đến đối tượng đã bị hủy
        currentWeapon = null;
        currentGun = null;
        
        // Tạo instance của prefab vũ khí hợp nhất ở vị trí drop point
        GameObject droppedWeapon = Instantiate(
            weaponPrefabs[prefabIndex],
            dropPoint.position,
            dropPoint.rotation
        );
        
        // Đảm bảo súng vừa tạo nằm đúng layer để có thể nhặt được
        droppedWeapon.layer = LayerMask.NameToLayer("Pickups");
        
        // Đảm bảo có WeaponPickup component
        WeaponPickup pickup = droppedWeapon.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            pickup = droppedWeapon.AddComponent<WeaponPickup>();
        }
        
        // Đảm bảo có Rigidbody với cấu hình phù hợp
        Rigidbody rb = droppedWeapon.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = droppedWeapon.AddComponent<Rigidbody>();
        }
        
        // Cấu hình Rigidbody để vật lý hoạt động đúng
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = 5f; // Điều chỉnh khối lượng phù hợp
        rb.drag = 0.5f; // Thêm lực cản không khí
        rb.angularDrag = 0.2f; // Cản quay
        
        // Đảm bảo có Collider phù hợp
        Collider collider = droppedWeapon.GetComponent<Collider>();
        if (collider == null)
        {
            // Thử tìm collider trong con trước
            Collider childCollider = droppedWeapon.GetComponentInChildren<Collider>(true);
            
            if (childCollider != null)
            {
                // Sử dụng collider đã có trong con
                collider = childCollider;
            }
            else
            {
                // Tạo mới box collider nếu cần
                BoxCollider boxCollider = droppedWeapon.AddComponent<BoxCollider>();
                // Đảm bảo kích thước collider phù hợp
                boxCollider.size = new Vector3(0.3f, 0.15f, 0.5f);
                boxCollider.center = Vector3.zero;
            }
        }
        
        // Sao chép thuộc tính từ vũ khí hiện tại sang pickup
        pickup.weaponIndex = prefabIndex;
        pickup.weaponName = weaponName;
        
        // Sao chép thông tin chi tiết từ Gun component - using our saved reference
        if (gunComponentRef != null)
        {
            pickup.CopyPropertiesFromGun(gunComponentRef);
        }
        
        // Đặt chế độ pickup và áp dụng lực
        pickup.SetPickupMode(true);
        pickup.ApplyDropForce(playerCamera.transform.forward, dropForce);
        
        // Xóa vũ khí hiện tại sau khi đã tạo bản sao
        Destroy(weaponToDestroy);
        
        // Đợi một frame để Unity cập nhật hierarchy
        StartCoroutine(SwitchToWeaponAfterDrop(hasPistol, pistolIndex, switchWeaponRef));
    }
    
    // Coroutine để chờ một frame trước khi chuyển đổi vũ khí
    private IEnumerator SwitchToWeaponAfterDrop(bool hadPistol, int pistolIndex, SwitchWeapon switchWeaponRef)
    {
        // Chờ một frame để Unity cập nhật hierarchy
        yield return null;
        
        // Kiểm tra lại tham chiếu switchWeapon
        if (this.switchWeapon == null)
        {
            Debug.LogWarning("switchWeapon đã trở thành null sau khi vứt vũ khí. Đang thử lấy lại tham chiếu...");
            this.switchWeapon = GetComponent<SwitchWeapon>();
        }
        
        // Sử dụng tham chiếu cục bộ nếu tham chiếu chính vẫn là null
        SwitchWeapon switchToUse = this.switchWeapon ?? switchWeaponRef;
        
        // Kiểm tra nếu switchWeapon không null
        if (switchToUse != null)
        {
            // Đảm bảo số vũ khí còn lại hợp lệ
            if (weaponHolder.childCount > 0)
            {
                Debug.Log($"Số lượng vũ khí còn lại: {weaponHolder.childCount}");
                
                // Chọn vũ khí mới dựa trên index
                if (weaponHolder.childCount > 1 && hadPistol && pistolIndex != -1)
                {
                    // Tìm lại súng lục sau khi xóa vũ khí cũ
                    int newPistolIndex = FindPistolIndex();
                    if (newPistolIndex != -1)
                    {
                        Debug.Log($"Đã tìm thấy súng lục ở vị trí {newPistolIndex} sau khi vứt vũ khí, đang chuyển sang súng lục");
                        switchToUse.selectedWeapon = newPistolIndex;
                    }
                    else
                    {
                        Debug.LogWarning("Không tìm thấy súng lục sau khi vứt vũ khí!");
                        switchToUse.selectedWeapon = 0;
                    }
                }
                else
                {
                    // Nếu không có súng lục hoặc chỉ còn 1 vũ khí, chuyển sang vũ khí đầu tiên
                    Debug.Log("Chuyển sang vũ khí đầu tiên (index 0)");
                    switchToUse.selectedWeapon = 0;
                }
                
                // Kích hoạt thủ công vũ khí mới thay vì gọi SelectWeapon
                ManuallyEnableWeapon(switchToUse.selectedWeapon);
                
                // Debug để theo dõi
                StartCoroutine(DebugWeaponSwitchStatus());
            }
            else
            {
                Debug.LogWarning("Không còn vũ khí nào trong kho đồ sau khi vứt vũ khí cuối cùng!");
            }
        }
        else
        {
            Debug.LogWarning("Cả hai tham chiếu switchWeapon đều là null, thử phương pháp khác để kích hoạt vũ khí!");
            
            // Phương pháp dự phòng để kích hoạt vũ khí nếu không có SwitchWeapon
            ManuallyEnableNextWeapon();
        }
    }
    
    // Thêm phương thức debug để theo dõi việc chuyển đổi vũ khí
    private IEnumerator DebugWeaponSwitchStatus()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (switchWeapon == null || weaponHolder == null) yield break;
        
        Debug.Log($"Sau khi chuyển vũ khí: selectedWeapon = {switchWeapon.selectedWeapon}, Số lượng vũ khí: {weaponHolder.childCount}");
        
        for (int i = 0; i < weaponHolder.childCount; i++)
        {
            Transform child = weaponHolder.GetChild(i);
            Debug.Log($"- Vũ khí {i}: {child.name}, Active: {child.gameObject.activeSelf}");
            
            Gun gun = child.GetComponent<Gun>();
            if (gun != null)
            {
                Debug.Log($"  + Là súng lục: {gun.isPistol}");
            }
        }
    }
    
    // Cải tiến phương thức tìm súng lục
    private int FindPistolIndex()
    {
        // Kiểm tra null trước khi truy cập
        if (weaponHolder == null)
        {
            Debug.LogError("weaponHolder là null!");
            return -1;
        }
        
        // Tìm qua tất cả vũ khí trong weaponHolder
        for (int i = 0; i < weaponHolder.childCount; i++)
        {
            Transform weapon = weaponHolder.GetChild(i);
            if (weapon == null) continue;
            
            // Trường hợp 1: Kiểm tra qua Gun component và thuộc tính isPistol
            Gun gunComponent = weapon.GetComponent<Gun>();
            if (gunComponent != null && gunComponent.isPistol)
            {
                Debug.Log($"Tìm thấy súng lục ở vị trí {i} dựa trên thuộc tính isPistol");
                return i;
            }
            
            // Trường hợp 2: Tìm theo tên (phương pháp dự phòng)
            string weaponName = weapon.name.ToLower();
            if (weaponName.Contains("pistol") || weaponName.Contains("handgun") || 
                weaponName.Contains("revolver") || weaponName.Contains("glock"))
            {
                Debug.Log($"Tìm thấy súng lục ở vị trí {i} dựa trên tên vũ khí: {weapon.name}");
                return i;
            }
        }
        
        Debug.LogWarning("Không tìm thấy súng lục trong weaponHolder!");
        return -1;
    }
    
    // Thử nhặt vũ khí hoặc đạn trước mặt
    private void TryPickupWeapon()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Không có camera được gán!");
            return;
        }
        
        RaycastHit hit;
        // Raycast để kiểm tra vật phẩm phía trước
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, pickupRange, pickupLayer))
        {
            // Kiểm tra xem đối tượng có phải là đạn không
            AmmoPickup ammoPickup = hit.collider.GetComponent<AmmoPickup>();
            if (ammoPickup != null)
            {
                bool ammoAdded = false;
                
                // Thử tìm súng phù hợp trong kho để thêm đạn
                for (int i = 0; i < weaponHolder.childCount; i++)
                {
                    Gun gun = weaponHolder.GetChild(i).GetComponent<Gun>();
                    if (gun != null)
                    {
                        // Thử thêm đạn vào súng này
                        if (ammoPickup.AddAmmoToGun(gun))
                        {
                            ammoAdded = true;
                            break;
                        }
                    }
                }
                
                if (ammoAdded)
                {
                    // Xóa đạn sau khi nhặt
                    Destroy(hit.collider.gameObject);
                    return;
                }
                else
                {
                    Debug.Log("Không có súng phù hợp để thêm đạn này!");
                }
            }
            
            // Xử lý nhặt vũ khí như bình thường
            WeaponPickup pickup = hit.collider.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                Debug.Log($"Tìm thấy WeaponPickup: {pickup.weaponName}");
                
                // Nhặt và trang bị vũ khí
                AddWeaponToInventory(pickup.gameObject);
                
                // Xóa vật phẩm sau khi nhặt
                Destroy(hit.collider.gameObject);
            }
            else
            {
                Debug.LogWarning($"Đối tượng {hit.collider.gameObject.name} không có component WeaponPickup!");
                
                // Thử tìm trong các đối tượng con
                WeaponPickup childPickup = hit.collider.GetComponentInChildren<WeaponPickup>();
                if (childPickup != null)
                {
                    Debug.Log($"Tìm thấy WeaponPickup trong đối tượng con: {childPickup.weaponName}");
                    
                    // Nhặt và trang bị vũ khí
                    AddWeaponToInventory(childPickup.gameObject);
                    
                    // Xóa vật phẩm sau khi nhặt
                    Destroy(childPickup.transform.root.gameObject);
                }
                
                // Thử tìm Medkit trong các đối tượng con
                MedkitPickup childMedkit = hit.collider.GetComponentInChildren<MedkitPickup>();
                if (childMedkit != null)
                {
                    Debug.Log("Tìm thấy Medkit trong đối tượng con, đang sử dụng...");
                    // Tìm HealthManager của người chơi
                    HealthManager playerHealth = GetComponent<HealthManager>();
                    if (playerHealth == null)
                    {
                        // Nếu không tìm thấy trên GameObjet hiện tại, tìm trên Player
                        playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<HealthManager>();
                    }
                    
                    if (playerHealth != null)
                    {
                        // Sử dụng medkit
                        childMedkit.Use(playerHealth);
                    }
                    else
                    {
                        Debug.LogError("Không tìm thấy HealthManager trên người chơi!");
                    }
                }
            }
        }
        else
        {
        }
    }
    
    // Thêm vũ khí từ GameObject đã tồn tại vào kho đồ
    public void AddWeaponToInventory(GameObject weaponObject)
    {
        if (weaponObject == null)
        {
            Debug.LogError("weaponObject là null!");
            return;
        }
        
        // Lấy hoặc thêm WeaponPickup component
        WeaponPickup pickup = weaponObject.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            Debug.LogError("Không tìm thấy WeaponPickup component!");
            return;
        }
        
        Debug.Log($"Đang nhặt vũ khí: {pickup.weaponName} với index: {pickup.weaponIndex}");
        
        // Lưu camera chính trước khi tạo vũ khí mới
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Không tìm thấy Camera.main, đang tìm camera khác...");
            Camera[] cameras = FindObjectsOfType<Camera>();
            if (cameras.Length > 0)
            {
                mainCamera = cameras[0];
                Debug.Log($"Sử dụng camera thay thế: {mainCamera.name}");
            }
        }
        
        // Tắt tất cả các vũ khí hiện tại trước khi thêm vũ khí mới
        if (weaponHolder != null)
        {
            for (int i = 0; i < weaponHolder.childCount; i++)
            {
                weaponHolder.GetChild(i).gameObject.SetActive(false);
            }
        }
        
        // Clone vũ khí vào weaponHolder
        GameObject newWeapon = Instantiate(
            weaponObject,
            weaponHolder.position,
            weaponHolder.rotation,
            weaponHolder
        );
        
        // Sửa tên vũ khí để loại bỏ "(Clone)" hoặc "(Clone)(Clone)"
        string cleanName = pickup.weaponName;
        if (cleanName.Contains("(Clone)"))
        {
            cleanName = cleanName.Replace("(Clone)", "").Trim();
        }
        newWeapon.name = cleanName; // Đặt tên sạch cho vũ khí
        
        // Lưu transform gốc từ prefab nếu có
        Vector3 originalPosition = pickup.transform.localPosition;
        Quaternion originalRotation = pickup.transform.localRotation;
        Vector3 originalScale = pickup.transform.localScale;
        
        Debug.Log($"Transform gốc từ pickup: pos={originalPosition}, rot={originalRotation.eulerAngles}, scale={originalScale}");

        // KHÔNG đặt vị trí về Vector3.zero nữa, để giữ nguyên transform từ prefab
        // Chỉ đặt scale về mặc định nếu scale quá nhỏ hoặc không hợp lệ
        if (originalScale.magnitude < 0.1f)
        {
            newWeapon.transform.localScale = Vector3.one;
        }
        
        // Đảm bảo có WeaponComponentRestore component
        WeaponComponentRestore componentRestore = newWeapon.GetComponent<WeaponComponentRestore>();
        if (componentRestore == null)
        {
            componentRestore = newWeapon.AddComponent<WeaponComponentRestore>();
        }
        
        // Lấy Gun component
        Gun gunComponent = newWeapon.GetComponent<Gun>();
        if (gunComponent != null)
        {
            // Đảm bảo camera được gán đúng
            if (mainCamera != null)
            {
                gunComponent.playerCamera = mainCamera;
                Debug.Log($"Đã gán camera {mainCamera.name} cho vũ khí {newWeapon.name}");
            }
            else
            {
                Debug.LogError("Không thể tìm thấy camera nào để gán cho vũ khí!");
            }
            
            // Khôi phục các component bị thiếu
            componentRestore.RestoreGunComponents(gunComponent);
        }
        
        // Chuyển sang chế độ trang bị và áp dụng thuộc tính vào Gun component
        WeaponPickup newPickup = newWeapon.GetComponent<WeaponPickup>();
        if (newPickup != null)
        {
            newPickup.weaponName = cleanName;
            newPickup.SetPickupMode(false);
            newPickup.ApplyPropertiesToGun();
        }
        
        // Store transform nếu nó có giá trị hợp lệ (không phải zero)
        if (originalPosition != Vector3.zero || originalRotation != Quaternion.identity || originalScale != Vector3.one)
        {
            componentRestore.StoreCurrentTransformAsCorrect();
        }
        
        // QUAN TRỌNG: Gọi ResetPosition để khôi phục vị trí ban đầu của vũ khí
        componentRestore.ResetPosition();
        Debug.Log($"Đã gọi ResetPosition cho vũ khí {newWeapon.name}");

        // Đảm bảo vũ khí mới luôn hiển thị sau khi được thêm vào
        newWeapon.SetActive(true);
        Debug.Log($"Đã kích hoạt vũ khí {newWeapon.name} sau khi thêm vào kho đồ");
        
        // Cập nhật vũ khí hiện tại
        currentWeapon = newWeapon;
        currentGun = gunComponent;

        // Xác định vị trí của vũ khí mới trong hierarchy
        bool isPistol = gunComponent != null && gunComponent.isPistol;
        int newSelectedWeapon;
        
        if (isPistol)
        {
            // Súng lục luôn nằm ở cuối danh sách
            newWeapon.transform.SetSiblingIndex(weaponHolder.childCount - 1);
            newSelectedWeapon = weaponHolder.childCount - 1;
            Debug.Log("Thêm súng lục vào vị trí cuối cùng");
        }
        else
        {
            // Vũ khí chính (primary) luôn nằm ở vị trí đầu tiên (index 0)
            newWeapon.transform.SetSiblingIndex(0);
            newSelectedWeapon = 0;
            Debug.Log("Thêm vũ khí chính vào vị trí đầu tiên");
        }
        
        // Nếu có SwitchWeapon, cập nhật selectedWeapon nhưng KHÔNG gọi SelectWeapon()
        if (switchWeapon != null)
        {
            switchWeapon.selectedWeapon = newSelectedWeapon;
            Debug.Log($"Đã cập nhật selectedWeapon trong switchWeapon thành: {newSelectedWeapon} (không gọi SelectWeapon)");
        }

        // Add this code after successfully adding a weapon to inventory
        if (PickupDisplayManager.Instance != null)
        {
            // Get Gun component to extract required information
            if (gunComponent != null)
            {
                PickupDisplayManager.Instance.ShowWeaponPickup(
                    cleanName,  // weapon name
                    gunComponent.currentAmmo,  // current ammo
                    gunComponent.damage,  // damage value
                    gunComponent.isAutomatic  // is automatic
                );
            }
        }
    }
    
    // Phương thức để tìm Text component từ đường dẫn đã lưu
    private Text FindUITextFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        
        GameObject obj = GameObject.Find(path);
        if (obj != null)
        {
            return obj.GetComponent<Text>();
        }
        
        // Nếu không tìm thấy, tìm kiếm tất cả Text components
        Text[] texts = Object.FindObjectsOfType<Text>();
        foreach (Text text in texts)
        {
            if (text.name.ToLower().Contains("ammo"))
            {
                return text;
            }
        }
        
        return null;
    }
    
    // Hiển thị phạm vi nhặt vũ khí trong Editor
    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * pickupRange);
        }
    }
    
    // Kiểm tra xem vũ khí hiện tại có phải là vũ khí có thể vứt
    public bool GetCurrentWeaponIsDroppable()
    {
        // Lấy vũ khí hiện tại
        GetCurrentWeapon();
        
        // Nếu không có vũ khí nào hoặc là vũ khí duy nhất thì không thể vứt
        if (currentWeapon == null || weaponHolder.childCount <= 1)
        {
            return false;
        }
        
        // Kiểm tra nếu là súng lục (dựa vào Gun component)
        Gun gunComponent = currentWeapon.GetComponent<Gun>();
        if (gunComponent != null && gunComponent.isPistol)
        {
            return false;
        }
        
        // Có thể vứt trong các trường hợp còn lại
        return true;
    }
    
    // Phương pháp dự phòng để kích hoạt vũ khí nếu không có SwitchWeapon
    private void ManuallyEnableNextWeapon()
    {
        if (weaponHolder == null || weaponHolder.childCount == 0)
        {
            Debug.LogError("Không có vũ khí nào để kích hoạt!");
            return;
        }
        
        // Tìm súng lục trước
        int pistolIndex = FindPistolIndex();
        
        // Kích hoạt súng lục nếu có, nếu không thì kích hoạt vũ khí đầu tiên
        int weaponToEnable = (pistolIndex != -1) ? pistolIndex : 0;
        Debug.Log($"Đang kích hoạt vũ khí ở vị trí {weaponToEnable} thủ công");
        
        // Gọi phương thức mới để kích hoạt vũ khí
        ManuallyEnableWeapon(weaponToEnable);
    }
    
    // Mỗi khi Awake hoặc Start, đảm bảo chúng ta có reference đến SwitchWeapon
    private void OnEnable()
    {
        // Bảo đảm khi component này được kích hoạt, chúng ta luôn có reference đến SwitchWeapon
        if (switchWeapon == null)
        {
            switchWeapon = GetComponent<SwitchWeapon>();
            Debug.Log($"OnEnable: switchWeapon reference {(switchWeapon != null ? "found" : "not found")}");
        }
    }

    public void ApplyPrimaryTransform(WeaponComponentRestore componentRestore)
    {
        if (componentRestore != null)
        {
            // Luôn áp dụng giá trị cố định từ WeaponManager
            componentRestore.SetStoredPosition(primaryPosition);
            componentRestore.SetStoredRotation(primaryRotation);
            componentRestore.SetStoredScale(primaryScale);
            
            // Áp dụng ngay lập tức
            componentRestore.ResetPosition();
            
            Debug.Log($"WeaponManager: Đã áp dụng transform chính ({primaryPosition}, {primaryRotation.eulerAngles}, {primaryScale}) cho vũ khí {componentRestore.gameObject.name}");
        }
    }

    private void UpdatePrimaryTransformForAllWeapons()
    {
        if (weaponHolder == null) return;

        foreach (Transform weapon in weaponHolder)
        {
            WeaponComponentRestore componentRestore = weapon.GetComponent<WeaponComponentRestore>();
            if (componentRestore != null)
            {
                componentRestore.SetStoredPosition(primaryPosition);
                componentRestore.SetStoredRotation(primaryRotation);
                componentRestore.SetStoredScale(primaryScale);
                componentRestore.ResetPosition();
                Debug.Log($"WeaponManager: Đã cập nhật transform primary cho vũ khí {weapon.name}");
            }
        }
    }

    private void OnValidate()
    {
        // Gọi khi giá trị trong Inspector thay đổi
        UpdatePrimaryTransformForAllWeapons();
    }

    // Phương thức mới để kích hoạt thủ công vũ khí tại vị trí chỉ định
    private void ManuallyEnableWeapon(int index)
    {
        if (weaponHolder == null || index < 0 || index >= weaponHolder.childCount)
        {
            Debug.LogError($"Không thể kích hoạt vũ khí ở vị trí {index}: index không hợp lệ hoặc weaponHolder là null");
            return;
        }
        
        // Kích hoạt vũ khí được chỉ định và vô hiệu hóa tất cả các vũ khí khác
        for (int i = 0; i < weaponHolder.childCount; i++)
        {
            GameObject weapon = weaponHolder.GetChild(i).gameObject;
            bool shouldBeActive = (i == index);
            
            // Chỉ thay đổi trạng thái nếu cần thiết để tránh các sự kiện OnEnable/OnDisable không cần thiết
            if (weapon.activeSelf != shouldBeActive)
            {
                weapon.SetActive(shouldBeActive);
                Debug.Log($"Thay đổi trạng thái của vũ khí {weapon.name} thành {shouldBeActive}");
            }
        }
        
        // Cập nhật tham chiếu đến vũ khí hiện tại
        if (index < weaponHolder.childCount)
        {
            currentWeapon = weaponHolder.GetChild(index).gameObject;
            currentGun = currentWeapon.GetComponent<Gun>();
            Debug.Log($"Đã cập nhật tham chiếu currentWeapon thành {currentWeapon.name}");
        }
    }

    // Method to get a Gun component by name
    public Gun GetGunByName(string weaponName)
    {
        GameObject weapon = GetWeaponPrefabByName(weaponName);
        if (weapon != null)
        {
            return weapon.GetComponent<Gun>();
        }
        return null;
    }

    // Method to get a weapon prefab by name
    public GameObject GetWeaponPrefabByName(string weaponName)
    {
        if (weaponNameToIndex.TryGetValue(weaponName, out int index))
        {
            if (index >= 0 && index < weaponPrefabs.Count)
            {
                return weaponPrefabs[index];
            }
        }
        Debug.LogWarning($"Không tìm thấy prefab vũ khí có tên: {weaponName}");
        return null;
    }

    // Method to unlock a weapon
    public void UnlockWeapon(string weaponName)
    {
        // In this implementation, we assume a weapon is "unlocked" when it's added to the player's inventory
        // This would typically happen in your game's weapon unlock logic
        Debug.Log($"Weapon unlocked: {weaponName}");
        
        // Find weapon prefab by name
        GameObject weaponPrefab = GetWeaponPrefabByName(weaponName);
        if (weaponPrefab != null)
        {
            // Equip the weapon if it's not already equipped
            bool weaponFound = false;
            for (int i = 0; i < weaponHolder.childCount; i++)
            {
                if (weaponHolder.GetChild(i).name.Contains(weaponName))
                {
                    weaponFound = true;
                    break;
                }
            }
            
            if (!weaponFound)
            {
                // Add weapon to player's inventory
                GameObject weapon = Instantiate(weaponPrefab, weaponHolder);
                weapon.name = weaponName;
                weapon.SetActive(false); // Don't activate it yet
                Debug.Log($"Added weapon {weaponName} to player's inventory");
            }
        }
    }

    // Method to equip a specific weapon by name
    public void EquipWeapon(string weaponName)
    {
        // Find weapon in holder by name
        for (int i = 0; i < weaponHolder.childCount; i++)
        {
            if (weaponHolder.GetChild(i).name.Contains(weaponName))
            {
                if (switchWeapon != null)
                {
                    switchWeapon.selectedWeapon = i;
                    switchWeapon.SelectWeapon();
                }
                else
                {
                    // Fallback if switchWeapon is not available
                    for (int j = 0; j < weaponHolder.childCount; j++)
                    {
                        weaponHolder.GetChild(j).gameObject.SetActive(j == i);
                    }
                }
                
                currentWeapon = weaponHolder.GetChild(i).gameObject;
                currentGun = currentWeapon.GetComponent<Gun>();
                
                Debug.Log($"Equipped weapon: {weaponName}");
                return;
            }
        }
        
        Debug.LogWarning($"Could not equip weapon {weaponName}: not found in weapon holder");
    }

    // Methods to get/set ammo count for different weapon types
    public int GetAmmoCount(string ammoType)
    {
        if (ammoByType.TryGetValue(ammoType.ToLower(), out int count))
        {
            return count;
        }
        return 0;
    }

    public void SetAmmoCount(string ammoType, int count)
    {
        ammoType = ammoType.ToLower();
        if (ammoByType.ContainsKey(ammoType))
        {
            ammoByType[ammoType] = Mathf.Max(0, count);
            
            // Update the ammo count on all guns of this type
            foreach (Transform weaponTransform in weaponHolder)
            {
                Gun gun = weaponTransform.GetComponent<Gun>();
                if (gun != null)
                {
                    // Determine gun type based on isPistol property
                    string gunType = gun.isPistol ? "pistol" : "rifle";
                    if (gunType == ammoType)
                    {
                        gun.totalAmmo = count;
                        gun.UpdateAmmoUI();
                    }
                }
            }
        }
    }
}