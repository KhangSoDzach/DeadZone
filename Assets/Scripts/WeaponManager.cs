using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WeaponManager : MonoBehaviour
{
    [Header("Cài đặt quản lý vũ khí")]
    public Transform weaponHolder; // Vị trí chứa vũ khí đang được trang bị
    public Transform dropPoint; // Vị trí vứt vũ khí
    public float dropForce = 5f; // Lực vứt vũ khí
    public float pickupRange = 3f; // Khoảng cách có thể nhặt vũ khí
    public LayerMask pickupLayer; // Layer của vật phẩm có thể nhặt
    
    [Header("Input")]
    public InputManager inputManager; // Quản lý input
    public KeyCode dropKey = KeyCode.G; // Phím để vứt vũ khí
    public KeyCode pickupKey = KeyCode.E; // Phím để nhặt vũ khí
    
    [Header("Prefabs")]
    public List<GameObject> weaponPrefabs; // Prefabs của vũ khí không có script (để vứt xuống)
    public List<GameObject> weaponGameplayPrefabs; // Prefabs của vũ khí có đầy đủ script
    
    // Remove the cached score text references - we now use ScoreManager
    // [Header("UI References")]
    // public Text globalScoreText; 
    // private Text cachedScoreText;
    
    // Dictionary để map giữa tên vũ khí và index trong mảng prefab
    private Dictionary<string, int> weaponNameToIndex = new Dictionary<string, int>();
    
    private Camera playerCamera;
    private Gun currentGun;
    private GameObject currentWeapon;
    private SwitchWeapon switchWeapon;
    
    private void Start()
    {
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

        // Kiểm tra số lượng prefab gameplay
        if (weaponGameplayPrefabs.Count == 0)
        {
            Debug.LogError("Không có prefab gameplay vũ khí nào được đăng ký trong weaponGameplayPrefabs!");
        }
        else
        {
            Debug.Log($"Số lượng prefab gameplay vũ khí: {weaponGameplayPrefabs.Count}");
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
            Debug.LogError("Không tìm thấy camera chính!");
        }
        
        // Lấy reference đến SwitchWeapon nếu có
        switchWeapon = GetComponent<SwitchWeapon>();
        if (switchWeapon == null)
        {
            Debug.LogWarning("Không tìm thấy component SwitchWeapon trên đối tượng này. Chức năng chuyển đổi vũ khí có thể bị giới hạn.");
        }
        
        // Nếu không có dropPoint, tạo một điểm drop mặc định
        if (dropPoint == null)
        {
            Debug.Log("Không có dropPoint được chỉ định, tạo dropPoint mặc định.");
            GameObject dropPointObj = new GameObject("DropPoint");
            dropPointObj.transform.parent = playerCamera.transform;
            dropPointObj.transform.localPosition = new Vector3(0, -0.5f, 1f);
            dropPoint = dropPointObj.transform;
        }
        
        // Remove score text caching code
        // if (globalScoreText != null) {
        //    cachedScoreText = globalScoreText;
        //    Debug.Log("Using inspector-assigned score text: " + cachedScoreText.name);
        // }
        // Try to find score text if it wasn't assigned
        // if (cachedScoreText == null) {
        //    Text[] texts = FindObjectsOfType<Text>();
        //    foreach (Text text in texts) {
        //        if (text.name.ToLower().Contains("score")) {
        //            cachedScoreText = text;
        //            Debug.Log("Found and cached scoreText: " + text.name);
        //            break;
        //        }
        //    }
        // }
    }
    
    private void Update()
    {
        // Lấy vũ khí hiện tại
        GetCurrentWeapon();
        
        // Xử lý vứt vũ khí
        if (Input.GetKeyDown(dropKey) && currentWeapon != null)
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
        
        // Kiểm tra nếu đây là vũ khí cuối cùng thì không cho vứt
        if (weaponHolder.childCount <= 1)
        {
            Debug.Log("Không thể vứt vũ khí cuối cùng!");
            return;
        }
        
        // Kiểm tra xem vũ khí hiện tại có phải là súng lục không bằng Gun.isPistol
        Gun currentGunComponent = currentWeapon.GetComponent<Gun>();
        if (currentGunComponent != null && currentGunComponent.isPistol)
        {
            Debug.Log("Không thể vứt súng lục (vũ khí thứ cấp)!");
            return;
        }
        
        // Lấy tên của vũ khí để tìm prefab tương ứng
        string weaponName = currentWeapon.name;
        if (weaponName.Contains("(Clone)"))
        {
            weaponName = weaponName.Replace("(Clone)", "");
        }
        
        Debug.Log($"Đang tìm prefab cho vũ khí: {weaponName}");
        
        // Debug toàn bộ dictionary để kiểm tra
        Debug.Log("Danh sách vũ khí có trong dictionary:");
        foreach (var key in weaponNameToIndex.Keys)
        {
            Debug.Log($"- {key} : {weaponNameToIndex[key]}");
        }
        
        // Tìm index của prefab vũ khí trong mảng
        int prefabIndex = -1;
        
        // Kiểm tra chính xác tên vũ khí
        if (weaponNameToIndex.TryGetValue(weaponName, out prefabIndex))
        {
            Debug.Log($"Tìm thấy prefab index: {prefabIndex}");
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
                    Debug.Log($"Tìm thấy prefab tương tự: {key} với index {prefabIndex}");
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
        
        // Kiểm tra prefab có tồn tại không
        if (weaponPrefabs[prefabIndex] == null)
        {
            Debug.LogError($"Prefab ở index {prefabIndex} là null!");
            return;
        }
        
        // Tạo instance của prefab vũ khí không có script
        GameObject droppedWeapon = Instantiate(
            weaponPrefabs[prefabIndex],
            dropPoint.position,
            dropPoint.rotation
        );
        
        // Thêm Rigidbody nếu chưa có
        Rigidbody rb = droppedWeapon.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = droppedWeapon.AddComponent<Rigidbody>();
        }
        
        // Thêm BoxCollider nếu chưa có
        Collider collider = droppedWeapon.GetComponent<Collider>();
        if (collider == null)
        {
            droppedWeapon.AddComponent<BoxCollider>();
        }
        
        // Thêm WeaponPickup component và lưu lại thông tin chi tiết về vũ khí
        WeaponPickup pickup = droppedWeapon.AddComponent<WeaponPickup>();
        pickup.weaponIndex = prefabIndex;
        pickup.weaponName = weaponName;
        
        // Lưu lại prefab gốc và chỉ số
        pickup.originalWeaponPrefab = weaponGameplayPrefabs[prefabIndex];
        pickup.originalWeaponIndex = prefabIndex;
        
        // Lưu lại thông tin chi tiết từ vũ khí hiện tại
        if (currentGunComponent != null)
        {
            pickup.CopyPropertiesFromGun(currentGunComponent);
            
            // Thêm mã sau để lưu thông tin animator
            if (currentGunComponent.animator != null && currentGunComponent.animator.runtimeAnimatorController != null)
            {
                pickup.animatorController = currentGunComponent.animator.runtimeAnimatorController;
                Debug.Log($"Đã sao chép animator controller từ vũ khí {currentGunComponent.name} cho pickup");
            }
            
            Debug.Log($"Đã sao chép thuộc tính từ vũ khí {currentGunComponent.name} cho pickup");
        }
        
        // Áp dụng lực để vứt vũ khí ra xa
        rb.AddForce(playerCamera.transform.forward * dropForce, ForceMode.Impulse);
        
        // Lưu thông tin về số lượng vũ khí còn lại trước khi xóa
        int remainingWeapons = weaponHolder.childCount - 1;
        
        // Lưu tham chiếu đến switchWeapon trước khi có thể bị thay đổi
        SwitchWeapon switchWeaponRef = this.switchWeapon;
        
        // Tìm súng lục TRƯỚC khi xóa vũ khí hiện tại
        int pistolIndex = FindPistolIndex();
        bool hasPistol = pistolIndex != -1;
        
        Debug.Log($"Trước khi xóa: Có súng lục: {hasPistol}, Ở vị trí: {pistolIndex}, Số vũ khí còn lại: {remainingWeapons}, switchWeapon: {(switchWeaponRef != null)}");
        
        // Xóa vũ khí hiện tại
        Destroy(currentWeapon);
        currentWeapon = null;
        currentGun = null;
        
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
                
                // Gọi SelectWeapon để kích hoạt vũ khí mới
                switchToUse.SelectWeapon();
                
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
            if (weaponName.Contains("Pistol") || weaponName.Contains("handgun") || 
                weaponName.Contains("revolver") || weaponName.Contains("glock"))
            {
                Debug.Log($"Tìm thấy súng lục ở vị trí {i} dựa trên tên vũ khí: {weapon.name}");
                return i;
            }
        }
        
        Debug.LogWarning("Không tìm thấy súng lục trong weaponHolder!");
        return -1;
    }
    
    // Thử nhặt vũ khí trước mặt
    private void TryPickupWeapon()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, pickupRange, pickupLayer))
        {
            // Kiểm tra xem có phải là vũ khí có thể nhặt không
            WeaponPickup pickup = hit.collider.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                // Sử dụng phương thức mới để nhặt vũ khí với đúng tham chiếu gốc
                AddOriginalWeaponToInventory(pickup);
                Destroy(hit.collider.gameObject);
            }
        }
    }
    
    // Thêm vũ khí vào kho đồ với số đạn được chỉ định và các thuộc tính từ pickup
    public void AddWeaponToInventory(int weaponIndex, int ammoCount = -1)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponGameplayPrefabs.Count)
        {
            Debug.LogError($"weaponIndex không hợp lệ: {weaponIndex}");
            return;
        }
        
        // Lưu lại vị trí và góc xoay của prefab gốc
        GameObject originalPrefab = weaponGameplayPrefabs[weaponIndex];
        Transform originalTransform = originalPrefab.transform;
        
        Debug.Log($"Bắt đầu khởi tạo vũ khí từ prefab: {originalPrefab.name}");
        
        // Tạo bản sao của vũ khí gameplay và thêm vào weaponHolder
        GameObject newWeapon = Instantiate(
            weaponGameplayPrefabs[weaponIndex],
            weaponHolder.position,
            weaponHolder.rotation,
            weaponHolder
        );
        
        // Đặt lại vị trí, rotation và scale chính xác như prefab gốc
        newWeapon.transform.localPosition = originalTransform.localPosition;
        newWeapon.transform.localRotation = originalTransform.localRotation;
        newWeapon.transform.localScale = originalTransform.localScale;
        
        // Kiểm tra và xử lý các component thiết yếu
        EnsureWeaponComponentsLoaded(newWeapon, originalPrefab);
        
        // Xác định vị trí của vũ khí mới trong hierarchy
        Gun gunComponent = newWeapon.GetComponent<Gun>();
        bool isPistol = gunComponent != null && gunComponent.isPistol;
        int targetIndex;
        int newSelectedWeapon;
        
        // Tìm WeaponPickup đang ngắm để áp dụng thuộc tính
        WeaponPickup pickupInfo = FindPickupInFront();
        
        if (isPistol)
        {
            // Súng lục (secondary) luôn nằm ở cuối danh sách
            targetIndex = weaponHolder.childCount - 1;
            newSelectedWeapon = targetIndex;
            Debug.Log("Thêm súng lục vào vị trí cuối cùng");
        }
        else
        {
            // Vũ khí chính (primary) luôn nằm ở vị trí đầu tiên (index 0)
            targetIndex = 0;
            newSelectedWeapon = 0;
            Debug.Log("Thêm vũ khí chính vào vị trí đầu tiên");
            
            // Di chuyển vũ khí mới lên vị trí đầu tiên trong hierarchy
            newWeapon.transform.SetSiblingIndex(0);
        }
        
        // Áp dụng thuộc tính đã lưu từ WeaponPickup nếu có
        if (gunComponent != null)
        {
            if (pickupInfo != null)
            {
                // Áp dụng thuộc tính từ pickup
                ApplyWeaponPropertiesFromPickup(gunComponent, pickupInfo);
                Debug.Log("Đã áp dụng thuộc tính từ weapon pickup vào súng mới");
            }
            else if (ammoCount >= 0)
            {
                // Nếu không có pickup nhưng có ammoCount, chỉ thiết lập đạn
                gunComponent.currentAmmo = ammoCount;
            }
            else
            {
                // Nếu không có gì, sử dụng giá trị mặc định của prefab
                Gun originalGun = originalPrefab.GetComponent<Gun>();
                if (originalGun != null)
                {
                    gunComponent.currentAmmo = originalGun.maxAmmo;
                }
            }
            
            // Cập nhật UI ngay lập tức
            gunComponent.UpdateAmmoUI();
        }
        
        // Nếu có SwitchWeapon, chuyển sang vũ khí mới nhặt
        if (switchWeapon != null)
        {
            switchWeapon.selectedWeapon = newSelectedWeapon;
            switchWeapon.SelectWeapon();
            Debug.Log($"Đã chọn vũ khí tại index: {newSelectedWeapon}");
        }
    }
    
    // Phương thức mới để thêm vũ khí gốc vào inventory (không tạo clone mới)
    private void AddOriginalWeaponToInventory(WeaponPickup pickup)
    {
        if (pickup == null) return;
        
        int weaponIndex = pickup.weaponIndex;
        if (weaponIndex < 0 || weaponIndex >= weaponGameplayPrefabs.Count)
        {
            Debug.LogError($"weaponIndex không hợp lệ: {weaponIndex}");
            return;
        }
        
        Debug.Log($"Đang nhặt vũ khí với index: {weaponIndex}, tên: {pickup.weaponName}");
        
        // Tạo bản sao của vũ khí gameplay và thêm vào weaponHolder
        GameObject newWeapon = Instantiate(
            weaponGameplayPrefabs[weaponIndex],
            weaponHolder.position,
            weaponHolder.rotation,
            weaponHolder
        );
        
        // Đặt lại vị trí, rotation và scale dựa trên prefab gốc
        Transform originalTransform = weaponGameplayPrefabs[weaponIndex].transform;
        newWeapon.transform.localPosition = originalTransform.localPosition;
        newWeapon.transform.localRotation = originalTransform.localRotation;
        newWeapon.transform.localScale = originalTransform.localScale;
        
        // Khôi phục tên gốc của vũ khí (nếu không có "(Clone)")
        if (pickup.weaponName != null && !pickup.weaponName.Contains("(Clone)"))
        {
            newWeapon.name = pickup.weaponName;
        }
        
        // Kiểm tra và xử lý các component thiết yếu
        EnsureWeaponComponentsLoaded(newWeapon, weaponGameplayPrefabs[weaponIndex]);
        
        // Áp dụng các thuộc tính từ WeaponPickup vào Gun component
        Gun gunComponent = newWeapon.GetComponent<Gun>();
        if (gunComponent != null)
        {
            ApplyWeaponPropertiesFromPickup(gunComponent, pickup);
            
            // Đảm bảo animator được khởi tạo
            if (pickup.animatorController != null)
            {
                if (gunComponent.animator == null)
                    gunComponent.animator = newWeapon.GetComponent<Animator>() ?? 
                                           newWeapon.AddComponent<Animator>();
                                           
                gunComponent.animator.runtimeAnimatorController = pickup.animatorController;
                Debug.Log("Đã áp dụng animator controller cho súng vừa nhặt");
            }
            
            // Xác định vị trí của vũ khí mới trong hierarchy
            bool isPistol = gunComponent.isPistol;
            int targetIndex;
            int newSelectedWeapon;
            
            if (isPistol)
            {
                // Súng lục luôn nằm ở cuối danh sách
                targetIndex = weaponHolder.childCount - 1;
                newSelectedWeapon = targetIndex;
                Debug.Log("Thêm súng lục vào vị trí cuối cùng");
            }
            else
            {
                // Vũ khí chính luôn nằm ở vị trí đầu tiên
                targetIndex = 0;
                newSelectedWeapon = 0;
                Debug.Log("Thêm vũ khí chính vào vị trí đầu tiên");
                
                // Di chuyển vũ khí mới lên vị trí đầu tiên trong hierarchy
                newWeapon.transform.SetSiblingIndex(0);
            }
            
            // Nếu có SwitchWeapon, chuyển sang vũ khí mới nhặt
            if (switchWeapon != null)
            {
                switchWeapon.selectedWeapon = newSelectedWeapon;
                switchWeapon.SelectWeapon();
                
                // Gọi phương thức OnWeaponEnabled để vũ khí khởi tạo đúng
                if (gunComponent != null)
                {
                    gunComponent.OnWeaponEnabled();
                }
                
                Debug.Log($"Đã chọn vũ khí gốc tại index: {newSelectedWeapon}");
            }
        }
        else
        {
            Debug.LogError("Không tìm thấy Gun component trên vũ khí mới nhặt!");
        }
    }
    
    // Phương thức mới để tìm WeaponPickup đang được nhìn
    private WeaponPickup FindPickupInFront()
    {
        if (playerCamera == null) return null;
        
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, pickupRange, pickupLayer))
        {
            return hit.collider.GetComponent<WeaponPickup>();
        }
        return null;
    }
    
    // Phương thức mới để áp dụng thuộc tính từ WeaponPickup vào Gun
    private void ApplyWeaponPropertiesFromPickup(Gun targetGun, WeaponPickup pickup)
    {
        if (targetGun == null || pickup == null) return;
        
        // Áp dụng các thuộc tính cơ bản
        targetGun.currentAmmo = pickup.remainingAmmo;
        targetGun.isPistol = pickup.isPistol;
        targetGun.isAutomatic = pickup.isAutomatic;
        targetGun.damage = pickup.damage;
        targetGun.recoilAmount = pickup.recoilAmount;
        targetGun.baseSpread = pickup.baseSpread;
        targetGun.impactEffect = pickup.impactEffect;
        
        // Khôi phục animator controller
        if (pickup.animatorController != null)
        {
            // Tìm Animator trong parent GameObject (Weapon holder) trước
            Animator parentAnimator = targetGun.transform.parent ? targetGun.transform.parent.GetComponent<Animator>() : null;
            
            if (parentAnimator != null)
            {
                targetGun.animator = parentAnimator;
                targetGun.animator.runtimeAnimatorController = pickup.animatorController;
                Debug.Log("Đã áp dụng animator controller cho Weapon holder (parent)");
            }
            else
            {
                // Nếu không có trong parent, kiểm tra trong weapon GameObject
                if (targetGun.animator == null)
                    targetGun.animator = targetGun.gameObject.GetComponent<Animator>() ?? 
                                         targetGun.gameObject.GetComponentInChildren<Animator>();
                
                if (targetGun.animator == null)
                    targetGun.animator = targetGun.gameObject.AddComponent<Animator>();
                    
                targetGun.animator.runtimeAnimatorController = pickup.animatorController;
                Debug.Log("Đã áp dụng animator controller cho weapon GameObject");
            }
        }
        
        // Áp dụng thiết lập âm thanh nếu có
        if (pickup.gunshotClip != null)
        {
            if (targetGun.gunshotSound == null)
                targetGun.gunshotSound = targetGun.gameObject.GetComponent<AudioSource>() ?? 
                                        targetGun.gameObject.AddComponent<AudioSource>();
                
            targetGun.gunshotSound.clip = pickup.gunshotClip;
            targetGun.gunshotSound.volume = pickup.gunVolume;
            targetGun.gunshotSound.playOnAwake = false;
        }
        
        // Tìm các UI components từ đường dẫn đã lưu
        if (!string.IsNullOrEmpty(pickup.ammoTextPath))
            targetGun.ammoText = FindUITextFromPath(pickup.ammoTextPath);
            
        // We no longer need to deal with scoreText
        
        Debug.Log($"Đã áp dụng thuộc tính từ pickup vào vũ khí {targetGun.name}: " +
                 $"Đạn còn: {targetGun.currentAmmo}, " +
                 $"Súng lục: {targetGun.isPistol}, " +
                 $"Tự động: {targetGun.isAutomatic}, " +
                 $"Sát thương: {targetGun.damage}");
    }
    
    // Phương thức để tìm Text component từ đường dẫn đã lưu
    private Text FindUITextFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        
        GameObject obj = GameObject.Find(path);
        if (obj != null)
            return obj.GetComponent<Text>();
            
        // Nếu không tìm thấy bằng đường dẫn đầy đủ, thử tìm bằng tên
        string[] parts = path.Split('/');
        if (parts.Length > 0)
        {
            string name = parts[parts.Length - 1];
            Text[] allTexts = FindObjectsOfType<Text>();
            foreach (Text text in allTexts)
            {
                if (text.name == name)
                    return text;
            }
        }
        
        return null;
    }
    
    // Phương thức mới để đảm bảo tất cả component của vũ khí được tải đúng
    private void EnsureWeaponComponentsLoaded(GameObject newWeapon, GameObject originalPrefab)
    {
        Debug.Log("Kiểm tra và đảm bảo tất cả component được tải đúng...");
        
        // 1. Kiểm tra Gun component
        Gun gunComponent = newWeapon.GetComponent<Gun>();
        if (gunComponent != null)
        {
            Debug.Log("Đã tìm thấy Gun component");
            
            // 2. Kiểm tra Animator - kiểm tra trong parent (Weapon holder) trước
            if (gunComponent.animator == null)
            {
                Debug.Log("Tìm kiếm Animator component...");
                
                // Tìm trong parent (Weapon holder) trước
                Animator parentAnimator = newWeapon.transform.parent ? newWeapon.transform.parent.GetComponent<Animator>() : null;
                
                if (parentAnimator != null)
                {
                    gunComponent.animator = parentAnimator;
                    Debug.Log("Đã tìm thấy Animator trong parent (Weapon holder)");
                }
                else
                {
                    // Tìm trong vũ khí mới nếu không có trong parent
                    gunComponent.animator = newWeapon.GetComponent<Animator>();
                    
                    // Nếu không có, tìm trong các thành phần con
                    if (gunComponent.animator == null)
                    {
                        gunComponent.animator = newWeapon.GetComponentInChildren<Animator>();
                    }
                    
                    // Nếu vẫn không thấy, kiểm tra prefab gốc và xem nó có parent Animator không
                    if (gunComponent.animator == null)
                    {
                        Animator originalParentAnimator = originalPrefab.transform.parent ? 
                                                         originalPrefab.transform.parent.GetComponent<Animator>() : null;
                        
                        if (originalParentAnimator != null && originalParentAnimator.runtimeAnimatorController != null)
                        {
                            // Thêm vào parent của weapon hiện tại nếu có
                            if (newWeapon.transform.parent)
                            {
                                Debug.Log("Tạo Animator trong parent từ prefab gốc");
                                gunComponent.animator = newWeapon.transform.parent.gameObject.AddComponent<Animator>();
                                gunComponent.animator.runtimeAnimatorController = originalParentAnimator.runtimeAnimatorController;
                                gunComponent.animator.avatar = originalParentAnimator.avatar;
                            }
                            else
                            {
                                // Nếu không có parent, thêm vào weapon
                                Debug.Log("Không có parent, tạo Animator trong weapon");
                                gunComponent.animator = newWeapon.AddComponent<Animator>();
                                gunComponent.animator.runtimeAnimatorController = originalParentAnimator.runtimeAnimatorController;
                                gunComponent.animator.avatar = originalParentAnimator.avatar;
                            }
                        }
                        else
                        {
                            // Kiểm tra Animator trong weapon prefab gốc
                            Animator originalAnimator = originalPrefab.GetComponent<Animator>() ?? 
                                                      originalPrefab.GetComponentInChildren<Animator>();
                            
                            if (originalAnimator != null && originalAnimator.runtimeAnimatorController != null)
                            {
                                Debug.Log("Tạo Animator từ prefab gốc");
                                gunComponent.animator = newWeapon.AddComponent<Animator>();
                                gunComponent.animator.runtimeAnimatorController = originalAnimator.runtimeAnimatorController;
                                gunComponent.animator.avatar = originalAnimator.avatar;
                            }
                            else
                            {
                                Debug.LogWarning("Không tìm thấy Animator trong prefab. Tạo mới Animator.");
                                gunComponent.animator = newWeapon.AddComponent<Animator>();
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"Animator đã được tìm thấy trong weapon: {gunComponent.animator.name}");
                    }
                }
            }
            else
            {
                Debug.Log($"Animator đã được gán trước đó: {gunComponent.animator.name}");
            }
            
            // 3. Kiểm tra AudioSource
            if (gunComponent.gunshotSound == null)
            {
                Debug.Log("Tìm kiếm AudioSource cho âm thanh bắn...");
                
                // Tìm AudioSource trong vũ khí mới
                AudioSource audioSource = newWeapon.GetComponent<AudioSource>();
                
                // Nếu không có, tìm trong các thành phần con
                if (audioSource == null)
                {
                    audioSource = newWeapon.GetComponentInChildren<AudioSource>();
                }
                
                // Nếu vẫn không thấy, sao chép từ prefab gốc hoặc tạo mới
                if (audioSource == null)
                {
                    AudioSource originalAudioSource = originalPrefab.GetComponent<AudioSource>() ?? 
                                                 originalPrefab.GetComponentInChildren<AudioSource>();
                    
                    if (originalAudioSource != null)
                    {
                        Debug.Log("Tạo AudioSource từ prefab gốc");
                        audioSource = newWeapon.AddComponent<AudioSource>();
                        audioSource.clip = originalAudioSource.clip;
                        audioSource.volume = originalAudioSource.volume;
                        audioSource.pitch = originalAudioSource.pitch;
                        audioSource.spatialBlend = originalAudioSource.spatialBlend;
                        audioSource.playOnAwake = false;
                    }
                    else
                    {
                        Debug.LogWarning("Không tìm thấy AudioSource trong prefab. Tạo mới AudioSource.");
                        audioSource = newWeapon.AddComponent<AudioSource>();
                        audioSource.playOnAwake = false;
                    }
                }
                
                // Gán AudioSource cho gunshotSound
                if (audioSource != null)
                {
                    gunComponent.gunshotSound = audioSource;
                    Debug.Log("Đã gán AudioSource cho gunshotSound");
                }
            }
            
            // 4. Kiểm tra ParticleSystem (muzzleFlash)
            if (gunComponent.muzzleFlash == null)
            {
                Debug.Log("Tìm kiếm ParticleSystem cho hiệu ứng đạn...");
                
                // Tìm trong các thành phần con trước
                ParticleSystem[] particleSystems = newWeapon.GetComponentsInChildren<ParticleSystem>();
                
                if (particleSystems.Length > 0)
                {
                    // Tìm particle system có tên liên quan đến muzzle hoặc flash
                    foreach (ParticleSystem ps in particleSystems)
                    {
                        if (ps.name.ToLower().Contains("muzzle") || ps.name.ToLower().Contains("flash"))
                        {
                            gunComponent.muzzleFlash = ps;
                            Debug.Log($"Đã tìm thấy muzzleFlash: {ps.name}");
                            break;
                        }
                    }
                    
                    // Nếu không tìm thấy, dùng cái đầu tiên
                    if (gunComponent.muzzleFlash == null)
                    {
                        gunComponent.muzzleFlash = particleSystems[0];
                        Debug.Log($"Sử dụng ParticleSystem đầu tiên làm muzzleFlash: {particleSystems[0].name}");
                    }
                }
            }
            
            // 5. Kiểm tra impactEffect
            if (gunComponent.impactEffect == null)
            {
                Debug.LogWarning("impactEffect is missing, trying to find a default one...");
                
                // Tìm từ prefab gốc
                Gun originalGun = originalPrefab.GetComponent<Gun>();
                if (originalGun != null && originalGun.impactEffect != null)
                {
                    gunComponent.impactEffect = originalGun.impactEffect;
                }
                else
                {
                    // Tìm một impact effect mặc định trong Resources nếu có
                    GameObject defaultImpact = Resources.Load<GameObject>("DefaultImpact");
                    if (defaultImpact != null)
                    {
                        gunComponent.impactEffect = defaultImpact;
                    }
                    else
                    {
                        Debug.LogError("Không thể tìm thấy impact effect. Hãy gán trong Inspector.");
                    }
                }
            }
            
            // 6. Tìm UI references - only find ammoText now
            if (gunComponent.ammoText == null)
            {
                Text[] texts = FindObjectsOfType<Text>();
                foreach (Text text in texts)
                {
                    if (text.name.ToLower().Contains("ammo"))
                    {
                        gunComponent.ammoText = text;
                        Debug.Log("Đã tìm thấy ammoText: " + text.name);
                        break;
                    }
                }
            }
            
            // Remove all scoreText related code
            
            // 7. Đảm bảo camera reference
            if (gunComponent.playerCamera == null)
            {
                gunComponent.playerCamera = Camera.main;
                Debug.Log("Đã gán Camera.main cho playerCamera");
            }
        }
        else
        {
            Debug.LogError("Không tìm thấy Gun component trên vũ khí mới!");
        }
        
        Debug.Log("Hoàn tất kiểm tra và thiết lập component");
    }
    
    // Thêm phương thức mới để sao chép transform chính xác
    private void CopyTransform(Transform source, Transform target)
    {
        if (source == null || target == null) return;
        
        // Sao chép các giá trị transform đầy đủ
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
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
    
    // Phương thức này giúp phân tích sự khác biệt giữa tên vũ khí thực tế và tên prefab
    public void DebugWeaponNames()
    {
        // Liệt kê tất cả prefab trong mảng
        Debug.Log("====== DANH SÁCH PREFAB VŨ KHÍ ======");
        for (int i = 0; i < weaponPrefabs.Count; i++)
        {
            if (weaponPrefabs[i] != null)
            {
                Debug.Log($"{i}: {weaponPrefabs[i].name}");
            }
            else
            {
                Debug.Log($"{i}: NULL");
            }
        }
        
        // Liệt kê tất cả vũ khí đang có trong weaponHolder
        Debug.Log("====== DANH SÁCH VŨ KHÍ HIỆN TẠI ======");
        if (weaponHolder != null)
        {
            int childCount = 0;
            foreach (Transform child in weaponHolder)
            {
                string status = child.gameObject.activeSelf ? "[ACTIVE]" : "[INACTIVE]";
                Debug.Log($"{childCount}: {child.gameObject.name} {status}");
                childCount++;
            }
        }
        else
        {
            Debug.LogError("weaponHolder chưa được thiết lập!");
        }
        
        // Kiểm tra ánh xạ giữa vũ khí hiện tại và prefab
        Debug.Log("====== KIỂM TRA MAPPING TÊN VŨ KHÍ ======");
        if (weaponHolder != null)
        {
            foreach (Transform child in weaponHolder)
            {
                string weaponName = child.gameObject.name;
                if (weaponName.Contains("(Clone)"))
                {
                    weaponName = weaponName.Replace("(Clone)", "");
                }
                
                bool found = false;
                foreach (var key in weaponNameToIndex.Keys)
                {
                    // Tìm kiếm chính xác
                    if (key == weaponName)
                    {
                        Debug.Log($"+ Vũ khí '{weaponName}' khớp chính xác với prefab '{key}'");
                        found = true;
                        break;
                    }
                    
                    // Tìm kiếm một phần
                    if (key.ToLower().Contains(weaponName.ToLower()) || 
                        weaponName.ToLower().Contains(key.ToLower()))
                    {
                        Debug.Log($"+ Vũ khí '{weaponName}' khớp một phần với prefab '{key}'");
                        found = true;
                    }
                }
                
                if (!found)
                {
                    Debug.LogWarning($"- Vũ khí '{weaponName}' không khớp với bất kỳ prefab nào!");
                }
            }
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
        
        // Nếu đây là vũ khí lục (vũ khí ở vị trí 0), không thể vứt
        if (switchWeapon != null && switchWeapon.selectedWeapon == 0)
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
        
        // Vô hiệu hóa tất cả vũ khí
        for (int i = 0; i < weaponHolder.childCount; i++)
        {
            weaponHolder.GetChild(i).gameObject.SetActive(false);
        }
        
        // Kích hoạt vũ khí đã chọn
        if (weaponToEnable < weaponHolder.childCount)
        {
            weaponHolder.GetChild(weaponToEnable).gameObject.SetActive(true);
            Debug.Log($"Đã kích hoạt vũ khí: {weaponHolder.GetChild(weaponToEnable).name}");
        }
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
}