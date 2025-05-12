using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponPickup : MonoBehaviour
{
    public int weaponIndex;          // Index trong weaponPrefabs array
    public string weaponName;        // Tên của vũ khí
    public int remainingAmmo;        // Lưu số đạn còn lại trong băng khi vứt xuống
    public int remainingTotalAmmo;   // Lưu số đạn dự trữ khi vứt xuống
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

        // Đảm bảo vũ khí ở đúng layer
        gameObject.layer = LayerMask.NameToLayer("WeaponPickup");
        
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
                   // Debug.Log($"Vũ khí {weaponName} đang sử dụng vị trí đã lưu: {savedPos}");
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
        this.remainingTotalAmmo = sourceGun.totalAmmo; // Lưu đạn dự trữ
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
                Debug.Log($"[PICKUP] Created new Gun component for {gameObject.name}");
            }
        }
        
        if (gunComponent)
        {
            // Log initial values for debugging
            Debug.Log($"[PICKUP] BEFORE applying properties to Gun ({gameObject.GetInstanceID()}):" +
                     $"\n - Current Gun state: Damage={gunComponent.damage}, Recoil={gunComponent.recoilAmount}, Spread={gunComponent.baseSpread}" +
                     $"\n - Current Pickup state: Damage={damage}, Recoil={recoilAmount}, Spread={baseSpread}");
            
            // Áp dụng các thuộc tính cơ bản
            gunComponent.currentAmmo = remainingAmmo;
            gunComponent.totalAmmo = remainingTotalAmmo;
            gunComponent.maxAmmo = remainingAmmo > 0 ? remainingAmmo : 30;
            gunComponent.isPistol = isPistol;
            gunComponent.isAutomatic = isAutomatic;
            gunComponent.damage = damage;
            gunComponent.recoilAmount = recoilAmount;
            gunComponent.baseSpread = baseSpread;
            gunComponent.impactEffect = impactEffect;
            
            // PRIORITIZE PARENT ANIMATOR - Always look for a parent animator first
            bool foundParentAnimator = false;
            
            // First, try to get animator from parent
            Transform parentTransform = transform.parent;
            if (parentTransform != null)
            {
                Animator parentAnimator = parentTransform.GetComponent<Animator>();
                if (parentAnimator != null)
                {
                    // Use the parent animator
                    gunComponent.animator = parentAnimator;
                    foundParentAnimator = true;
                    Debug.Log($"[PICKUP] Using parent animator from {parentTransform.name} for {gameObject.name}");
                    
                    // Apply animator controller to parent if it doesn't have one
                    if (animatorController != null && parentAnimator.runtimeAnimatorController == null)
                    {
                        parentAnimator.runtimeAnimatorController = animatorController;
                        Debug.Log($"[PICKUP] Applied animator controller to parent {parentTransform.name}");
                    }
                }
                else
                {
                    // Try to look for animator in parent's parent
                    Transform grandParent = parentTransform.parent;
                    if (grandParent != null)
                    {
                        Animator grandParentAnimator = grandParent.GetComponent<Animator>();
                        if (grandParentAnimator != null)
                        {
                            gunComponent.animator = grandParentAnimator;
                            foundParentAnimator = true;
                            Debug.Log($"[PICKUP] Using grandparent animator from {grandParent.name} for {gameObject.name}");
                        }
                    }
                }
            }
            
            // Only if we didn't find a parent animator, look for or create a local one
            if (!foundParentAnimator)
            {
                // Check if there's a local animator
                Animator localAnimator = GetComponent<Animator>();
                if (localAnimator != null)
                {
                    gunComponent.animator = localAnimator;
                    Debug.Log($"[PICKUP] No parent animator found. Using local animator on {gameObject.name}");
                    
                    // Apply animator controller to local animator if needed
                    if (animatorController != null && localAnimator.runtimeAnimatorController == null)
                    {
                        localAnimator.runtimeAnimatorController = animatorController;
                    }
                }
                else if (animatorController != null)
                {
                    // If we have a controller but no animator, create one as last resort
                    localAnimator = gameObject.AddComponent<Animator>();
                    gunComponent.animator = localAnimator;
                    localAnimator.runtimeAnimatorController = animatorController;
                    Debug.Log($"[PICKUP] Created new animator for {gameObject.name} as last resort");
                }
                else
                {
                    Debug.LogWarning($"[PICKUP] No animator found or created for {gameObject.name}. Weapon animations may not work.");
                }
            }
            
            // Apply audio settings
            if (gunshotClip != null)
            {
                if (gunComponent.gunshotSound == null)
                {
                    AudioSource existingAudio = GetComponent<AudioSource>();
                    gunComponent.gunshotSound = existingAudio ?? gameObject.AddComponent<AudioSource>();
                }
                
                gunComponent.gunshotSound.clip = gunshotClip;
                gunComponent.gunshotSound.volume = gunVolume;
                gunComponent.gunshotSound.playOnAwake = false;
            }
            
            // Ensure camera reference
            if (gunComponent.playerCamera == null)
            {
                gunComponent.playerCamera = Camera.main;
            }
            
            // Apply UI text if applicable
            if (!string.IsNullOrEmpty(ammoTextPath))
            {
                gunComponent.ammoText = FindUITextFromPath(ammoTextPath);
            }
            
            // Log final values
            Debug.Log($"[PICKUP] AFTER applying properties to Gun ({gameObject.GetInstanceID()}):" +
                     $"\n - Damage: {gunComponent.damage} (from {damage})" +
                     $"\n - Recoil: {gunComponent.recoilAmount} (from {recoilAmount})" +
                     $"\n - Animator: {(gunComponent.animator ? gunComponent.animator.gameObject.name : "null")}" +
                     $"\n - Ammo: {gunComponent.currentAmmo}/{gunComponent.totalAmmo}");
            
            // Update UI
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