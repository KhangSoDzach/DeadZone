using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Properties")]
    public string weaponName = "Default Weapon";
    public int remainingAmmo = 30;
    public float damage = 10f;
    public bool isAutomatic = true;
    
    [Header("Movement Effect")]
    public float rotationSpeed = 30f;
    public float floatSpeed = 0.5f;
    public float floatHeight = 0.2f;
    
    private Vector3 startPosition;
    
    // Weapon detail properties
    public bool isPistol;
    public float recoilAmount;
    public float baseSpread;
    public GameObject impactEffect;  // Prefab hiệu ứng va chạm
    
    // Tham chiếu đến các components
    [HideInInspector] public RuntimeAnimatorController animatorController;
    [HideInInspector] public AudioClip gunshotClip;
    [HideInInspector] public float gunVolume;
    
    // UI info
    [HideInInspector] public string ammoTextPath;
    
    // Reference to original gameplay weapon prefab
    [HideInInspector] public GameObject originalWeaponPrefab;
    [HideInInspector] public int originalWeaponIndex = -1;
    
    // Store original position for floating effect
    private Vector3 originalPosition;
    private bool isPickupMode = false;
    private bool isInitialized = false;
    
    // Store last dropped positions for reusing when picking up again
    private static Dictionary<string, Vector3> lastDroppedPositions = new Dictionary<string, Vector3>();
    
    // Missing variables needed for the script
    [HideInInspector] public int weaponIndex = -1; // Add weaponIndex to fix WeaponManager errors
    public int remainingTotalAmmo = 0; // Total ammo remaining in reserve
    
    // Component references
    private Rigidbody rb;
    private Collider pickupCollider;
    private Gun gunComponent;
    
    void Awake()
    {
        // Lưu vị trí ban đầu để làm hiệu ứng nổi
        originalPosition = transform.position;
        
        // Initialize components
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        pickupCollider = GetComponent<Collider>();
        if (pickupCollider == null) pickupCollider = gameObject.AddComponent<BoxCollider>();
        
        gunComponent = GetComponent<Gun>();
    }
    
    void Start()
    {
        startPosition = transform.position;
        
        // Make sure it's in the pickup layer
        gameObject.layer = LayerMask.NameToLayer("WeaponPickup");
        
        // Add glow effect if not already present
        if (GetComponent<PickupGlow>() == null)
        {
            PickupGlow glow = gameObject.AddComponent<PickupGlow>();
            glow.glowColor = new Color(0.2f, 0.6f, 1f); // Blue for weapons
            glow.intensity = 1.5f;
            glow.range = 3f;
            glow.flickerAmount = 0.08f;
        }
        
        // Check weapon position to decide mode
        bool isChildOfWeaponHolder = IsChildOfWeaponHolder();
        
        if (!isInitialized) {
            // If child of weapon holder, this is an equipped weapon
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
            
            // Kích hoạt hiệu ứng phát sáng nếu có
            PickupGlow glow = GetComponent<PickupGlow>();
            if (glow != null)
            {
                glow.enabled = true;
            }
            
            // Bật tất cả Light components trên vũ khí khi ở chế độ pickup
            Light[] lights = GetComponentsInChildren<Light>(true);
            foreach (Light light in lights)
            {
                light.enabled = true;
            }
            
            // Không tự động bắt đầu hiệu ứng nổi ở đây nữa
            // Hiệu ứng nổi sẽ được bắt đầu sau khi súng rơi xuống đất từ ApplyDropForce
            
            // Only start floating effect if not just dropped
            if (gameObject.activeInHierarchy && (rb == null || rb.velocity.sqrMagnitude < 0.1f))
            {
                StopAllCoroutines();
                originalPosition = transform.position;
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
            
            // Tắt hiệu ứng phát sáng khi đã được nhặt lên
            PickupGlow glow = GetComponent<PickupGlow>();
            if (glow != null)
            {
                glow.enabled = false;
            }
            
            // Tắt tất cả Light components trên vũ khí khi được trang bị
            Light[] lights = GetComponentsInChildren<Light>(true);
            foreach (Light light in lights)
            {
                light.enabled = false;
            }
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
                Debug.Log($"Saved position for weapon {weaponName}: {originalPosition}");
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

    // Helper method to find UI Text from saved path
    private TextMeshProUGUI FindUITextFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        GameObject obj = GameObject.Find(path);
        if (obj != null)
        {
            return obj.GetComponent<TextMeshProUGUI>();
        }
        TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.name.ToLower().Contains("ammo"))
            {
                return text;
            }
        }
        return null;
    }
    // Helper method to get full path of GameObject in hierarchy
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