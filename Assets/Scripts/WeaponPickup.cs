using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public int weaponIndex;          // Index trong weaponPrefabs array
    public string weaponName;        // Tên của vũ khí
    public int remainingAmmo;        // Lưu số đạn còn lại khi vứt xuống
    public bool isPistol;            // Có phải súng lục không (vũ khí chính không thể vứt)
    
    // Các thuộc tính chi tiết về vũ khí
    public bool isAutomatic;         // Loại súng (tự động/bán tự động)
    public float damage;             // Sát thương
    public float recoilAmount;       // Lượng giật
    public float baseSpread;         // Độ chính xác cơ bản
    public GameObject impactEffect;  // Prefab hiệu ứng va chạm
    
    // Tham chiếu đến các components
    [HideInInspector] public RuntimeAnimatorController animatorController;
    [HideInInspector] public AudioClip gunshotClip;
    [HideInInspector] public float gunVolume;
    
    // Thông tin UI
    [HideInInspector] public string ammoTextPath;
    
    // Tham chiếu đến vũ khí gameplay prefab gốc
    [HideInInspector] public GameObject originalWeaponPrefab;
    [HideInInspector] public int originalWeaponIndex = -1;
    
    // Các thành phần cho hiệu ứng nhặt súng
    [Header("Hiệu ứng nhặt vũ khí")]
    public float rotationSpeed = 50f;
    public float floatHeight = 0.1f;
    public float floatSpeed = 1f;
    
    // Components cần thiết khi súng được vứt xuống
    private Rigidbody rb;
    private Collider pickupCollider;
    
    // Components cần thiết khi súng được trang bị
    private Gun gunComponent;
    
    // Lưu vị trí gốc để làm hiệu ứng nổi
    private Vector3 originalPosition;
    private bool isPickupMode = false; // Thay đổi mặc định thành false
    private bool isInitialized = false;

    // Lưu vị trí của các vũ khí đã vứt xuống để có thể tái sử dụng khi nhặt lại
    private static Dictionary<string, Vector3> lastDroppedPositions = new Dictionary<string, Vector3>();
    
    void Awake()
    {
        // Kiểm tra các components cần thiết
        rb = GetComponent<Rigidbody>();
        pickupCollider = GetComponent<Collider>();
        gunComponent = GetComponent<Gun>();
        
        // Lưu vị trí ban đầu để làm hiệu ứng nổi
        originalPosition = transform.position;
    }
    
    void Start()
    {
        // Kiểm tra vị trí của vũ khí để quyết định chế độ
        bool isChildOfWeaponHolder = IsChildOfWeaponHolder();
        
        if (!isInitialized) {
            // Nếu là con của weapon holder, thì đây là vũ khí đang được trang bị
            SetPickupMode(!isChildOfWeaponHolder);
            isInitialized = true;
        }

        // Kiểm tra nếu đây là vũ khí mới nhặt, lấy vị trí đã lưu (nếu có)
        if (isPickupMode && !string.IsNullOrEmpty(weaponName))
        {
            if (lastDroppedPositions.TryGetValue(weaponName, out Vector3 savedPos))
            {
                // Chỉ sử dụng vị trí đã lưu nếu vị trí hiện tại gần đó
                // Điều này tránh trường hợp lấy vị trí của vũ khí trùng tên ở xa
                if (Vector3.Distance(transform.position, savedPos) < 5f)
                {
                    originalPosition = savedPos;
                    Debug.Log($"Vũ khí {weaponName} đang sử dụng vị trí đã lưu: {savedPos}");
                }
            }
        }
    }
    
    // Kiểm tra xem vũ khí có nằm trong weapon holder không
    bool IsChildOfWeaponHolder()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            // Kiểm tra nếu parent có tên chứa "weapon" hoặc "holder"
            if (current.name.ToLower().Contains("weapon") || 
                current.name.ToLower().Contains("holder") ||
                current.GetComponent<SwitchWeapon>() != null)
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }
    
    void OnDisable()
    {
        StopAllCoroutines();
    }
    
    // Chuyển đổi giữa trạng thái nhặt (Pickup) và trạng thái trang bị (Equipped)
    public void SetPickupMode(bool isPickup)
    {
        isPickupMode = isPickup;
        
        if (isPickup)
        {
            // Kích hoạt chế độ nhặt súng
            if (rb != null) 
            {
                rb.isKinematic = false;
                rb.useGravity = true; // Đảm bảo súng bị ảnh hưởng bởi trọng lực
            }
            
            if (pickupCollider != null) pickupCollider.enabled = true;
            
            // Tắt Gun component nếu có
            if (gunComponent != null) gunComponent.enabled = false;
            
            // Không tự động bắt đầu hiệu ứng nổi ở đây nữa
            // Hiệu ứng nổi sẽ được bắt đầu sau khi súng rơi xuống đất từ ApplyDropForce
            
            // Chỉ bắt đầu hiệu ứng nổi nếu không phải là súng vừa được vứt xuống
            if (gameObject.activeInHierarchy && (rb == null || rb.velocity.sqrMagnitude < 0.1f))
            {
                StopAllCoroutines();
                originalPosition = transform.position; // Lưu vị trí hiện tại
                StartCoroutine(FloatAndRotateEffect());
            }
        }
        else
        {
            // Kích hoạt chế độ trang bị
            StopAllCoroutines();
            
            if (rb != null) rb.isKinematic = true;
            if (pickupCollider != null) pickupCollider.enabled = false;
            
            // Kích hoạt Gun component nếu có
            if (gunComponent != null) gunComponent.enabled = true;
        }
    }
    
    // Áp dụng lực đẩy khi vũ khí được vứt ra
    public void ApplyDropForce(Vector3 direction, float force)
    {
        if (rb != null && isPickupMode)
        {
            // Đảm bảo vật lý hoạt động đúng trước khi áp dụng lực
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero; // Reset velocity
            rb.angularVelocity = Vector3.zero; // Reset angular velocity
            
            // Thêm lực đẩy có kiểm soát hơn
            rb.AddForce(direction * force * 0.5f + Vector3.down * 0.5f, ForceMode.Impulse);
            
            // Thêm một lực xoay nhẹ ngẫu nhiên
            rb.AddTorque(new Vector3(
                Random.Range(-1f, 1f), 
                Random.Range(-1f, 1f), 
                Random.Range(-1f, 1f)
            ) * force * 0.1f, ForceMode.Impulse);
            
            // Thay vì bắt đầu hiệu ứng nổi ngay lập tức, chờ một khoảng thời gian cho súng rơi xuống đất
            StartCoroutine(DelayedFloatEffect());
        }
    }
    
    // Chờ súng rơi xuống đất trước khi bắt đầu hiệu ứng nổi
    private IEnumerator DelayedFloatEffect()
    {
        // Chờ lâu hơn để đảm bảo súng đã rơi xuống đất và ổn định
        float waitTime = 3f;
        float elapsedTime = 0;
        
        // Liên tục kiểm tra khi nào vũ khí ổn định trên mặt đất
        while (elapsedTime < waitTime)
        {
            elapsedTime += 0.1f;
            
            // Kiểm tra xem súng có còn tồn tại và đang di chuyển không
            if (rb == null || !gameObject || !isPickupMode)
                yield break;
                
            // Kiểm tra nếu vũ khí đã đứng yên
            if (rb.velocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
            {
                // Thêm thời gian đệm
                yield return new WaitForSeconds(0.5f);
                break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Kiểm tra xem súng có còn tồn tại và đang ở chế độ pickup không
        if (gameObject != null && isPickupMode)
        {
            // Lưu vị trí mới sau khi rơi xuống đất
            originalPosition = transform.position;
            
            // Lưu vị trí vào dictionary để sử dụng lại khi nhặt vũ khí trùng tên
            if (!string.IsNullOrEmpty(weaponName))
            {
                lastDroppedPositions[weaponName] = originalPosition;
                Debug.Log($"Đã lưu vị trí cho vũ khí {weaponName}: {originalPosition}");
            }
            
            // Bắt đầu hiệu ứng nổi và xoay
            StartCoroutine(FloatAndRotateEffect());
        }
    }
    
    // Hiệu ứng nổi và xoay cho vũ khí có thể nhặt
    IEnumerator FloatAndRotateEffect()
    {
        float time = 0;
        
        while (true)
        {
            time += Time.deltaTime;
            
            // Hiệu ứng xoay
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            // Hiệu ứng nổi lên xuống
            float newY = originalPosition.y + Mathf.Sin(time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            yield return null;
        }
    }
    
    // Sao chép thuộc tính từ vũ khí gốc
    public void CopyPropertiesFromGun(Gun sourceGun)
    {
        if (sourceGun == null) return;
        
        // Lưu thông tin cơ bản
        this.remainingAmmo = sourceGun.currentAmmo;
        this.isPistol = sourceGun.isPistol;
        this.isAutomatic = sourceGun.isAutomatic;
        this.damage = sourceGun.damage;
        this.recoilAmount = sourceGun.recoilAmount;
        this.baseSpread = sourceGun.baseSpread;
        this.impactEffect = sourceGun.impactEffect;
        
        // Lưu thông tin animator nếu có
        if (sourceGun.animator != null && sourceGun.animator.runtimeAnimatorController != null)
        {
            this.animatorController = sourceGun.animator.runtimeAnimatorController;
        }
        
        // Sao chép cài đặt âm thanh
        if (sourceGun.gunshotSound != null)
        {
            this.gunshotClip = sourceGun.gunshotSound.clip;
            this.gunVolume = sourceGun.gunshotSound.volume;
        }
        
        // Lưu đường dẫn đến UI
        if (sourceGun.ammoText != null)
        {
            this.ammoTextPath = GetGameObjectPath(sourceGun.ammoText.gameObject);
        }
    }
    
    // Phương thức để đồng bộ thuộc tính từ WeaponPickup vào Gun của cùng GameObject
    public void ApplyPropertiesToGun()
    {
        if (!gunComponent)
        {
            gunComponent = gameObject.GetComponent<Gun>();
            if (!gunComponent) 
            {
                gunComponent = gameObject.AddComponent<Gun>();
            }
        }
        
        if (gunComponent)
        {
            // Áp dụng các thuộc tính cơ bản
            gunComponent.currentAmmo = remainingAmmo;
            gunComponent.isPistol = isPistol;
            gunComponent.isAutomatic = isAutomatic;
            gunComponent.damage = damage;
            gunComponent.recoilAmount = recoilAmount;
            gunComponent.baseSpread = baseSpread;
            gunComponent.impactEffect = impactEffect;
            
            // Khôi phục animator controller - ĐÃ SỬA
            // Luôn ưu tiên tìm animator trong parent trước
            Transform parentTransform = transform.parent;
            if (parentTransform != null)
            {
                Animator parentAnimator = parentTransform.GetComponent<Animator>();
                if (parentAnimator != null)
                {
                    gunComponent.animator = parentAnimator;
                    Debug.Log($"Đang sử dụng animator của parent {parentTransform.name} cho {gameObject.name}");
                    
                    // Chỉ áp dụng controller nếu có và nếu không dùng parent animator
                    if (animatorController != null && parentAnimator.runtimeAnimatorController == null)
                    {
                        parentAnimator.runtimeAnimatorController = animatorController;
                    }
                }
            }
            
            // Nếu vẫn chưa có animator và có controller đã lưu, mới tạo mới
            if (gunComponent.animator == null && animatorController != null)
            {
                // Kiểm tra xem đã có animator component trên vũ khí chưa
                Animator existingAnimator = gameObject.GetComponent<Animator>();
                if (existingAnimator != null) 
                {
                    gunComponent.animator = existingAnimator;
                }
                else
                {
                    // Tạo animator mới chỉ khi không tìm thấy ở parent và không có sẵn
                    gunComponent.animator = gameObject.AddComponent<Animator>();
                    Debug.Log($"Đã tạo animator mới cho {gameObject.name} vì không tìm thấy ở parent");
                }
                
                // Áp dụng controller đã lưu
                gunComponent.animator.runtimeAnimatorController = animatorController;
            }
            
            // Áp dụng thiết lập âm thanh
            if (gunshotClip != null)
            {
                if (gunComponent.gunshotSound == null)
                {
                    gunComponent.gunshotSound = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                }
                
                gunComponent.gunshotSound.clip = gunshotClip;
                gunComponent.gunshotSound.volume = gunVolume;
                gunComponent.gunshotSound.playOnAwake = false;
            }
            
            // Đảm bảo có camera
            if (gunComponent.playerCamera == null)
            {
                gunComponent.playerCamera = Camera.main;
                if (gunComponent.playerCamera == null)
                {
                    // Tìm bất kỳ camera nào nếu không tìm thấy main camera
                    Camera[] cameras = FindObjectsOfType<Camera>();
                    if (cameras.Length > 0)
                    {
                        gunComponent.playerCamera = cameras[0];
                        Debug.LogWarning($"Không tìm thấy camera chính! Sử dụng camera thay thế cho {gameObject.name}");
                    }
                    else
                    {
                        Debug.LogError("Không tìm thấy camera nào trong scene!");
                    }
                }
                else
                {
                    Debug.Log("Đã đặt camera chính cho súng");
                }
            }
            
            // Đảm bảo UI được cập nhật
            if (!string.IsNullOrEmpty(ammoTextPath))
            {
                gunComponent.ammoText = FindUITextFromPath(ammoTextPath);
            }
            
            // Tìm hoặc thêm component WeaponComponentRestore để đảm bảo vị trí và camera đúng
            WeaponComponentRestore componentRestore = gameObject.GetComponent<WeaponComponentRestore>();
            if (componentRestore == null)
            {
                // Tạo mới nếu chưa có
                componentRestore = gameObject.AddComponent<WeaponComponentRestore>();
                Debug.Log("Đã thêm component WeaponComponentRestore cho " + gameObject.name);
            }
            
            // Khôi phục vị trí và các components của súng
            componentRestore.RestoreGunComponents(gunComponent);
            
            // Cập nhật UI
            gunComponent.UpdateAmmoUI();
        }
    }
    
    // Helper method để tìm UI Text từ đường dẫn đã lưu
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
    
    // Helper method để lấy đường dẫn đầy đủ của GameObject trong hierarchy
    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "";
        
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    // OnValidate để đảm bảo inspector luôn hiển thị đúng
    void OnValidate()
    {
        // Đảm bảo tên vũ khí được đặt
        if (string.IsNullOrEmpty(weaponName) && gameObject.name != null)
        {
            weaponName = gameObject.name.Replace("(Clone)", "").Trim();
        }
    }
}