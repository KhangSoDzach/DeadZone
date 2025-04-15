using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Import for UI components

public enum WeaponType
{
    Small, // Súng nhỏ (pistol)
    Large  // Súng lớn (rifle, shotgun)
}

public class Gun : MonoBehaviour
{
    // Các thuộc tính vũ khí
    public float damage = 10f; 
    public float range = 100f; 
    public bool isAutomatic = false; // Determines if the gun is automatic (rifle) or semi-automatic (pistol)
    public int maxAmmo = 30; // Maximum ammo for the gun
    public int currentAmmo; // Current ammo in the gun
    public float reloadTime = 2f; // Time it takes to reload
    private bool isReloading = false; // Whether the gun is currently reloading
    public Animator animator; // Animator for reload animation
    // Start is called before the first frame update
    public Camera playerCamera;
    public ParticleSystem muzzleFlash; // Particle system for the muzzle flash
    public AudioSource gunshotSound; // Sound effect for the gunshot
    public GameObject impactEffect; // Prefab for the impact effect
    public float impactForce = 10f; // Force of the impact effect
    public float fireRate = 0.5f; // Rate of fire in seconds
    private float nextFireTime = 0f; // Time when the gun can fire again
    public Text ammoText; // Reference to the UI Text for displaying ammo
    public Text scoreText; // Reference to the UI Text for displaying score
    private int score = 0; // Player's score
    
    // Weapon drop and pickup properties
    public bool isLargeWeapon = true; // True for large weapons (rifles, shotguns), False for small weapons (pistols)
    public GameObject weaponDropPrefab; // Prefab used when dropping this weapon
    public float dropForce = 5f; // Force to apply when dropping the weapon
    private SwitchWeapon weaponHolder; // Reference to the weapon holder script
    public string weaponName; // Tên của vũ khí, dùng để tải prefab từ Resources khi nhặt lại
    
    // Thuộc tính cho chế độ pickup (khi vũ khí nằm trên mặt đất)
    public bool isPickupMode = false; // Có đang ở trạng thái pickup không
    public float rotationSpeed = 50f; // Tốc độ xoay khi vũ khí nằm trên mặt đất
    public float bobSpeed = 1f; // Tốc độ nhấp nhô lên xuống
    public float bobHeight = 0.1f; // Độ cao nhấp nhô
    private Vector3 startPosition; // Vị trí ban đầu để tính toán hiệu ứng nhấp nhô
    private bool isPickable = false; // Vũ khí có thể nhặt sau khi rơi xuống và nằm yên

    // UI for pickup prompt
    public GameObject pickupPromptUI;
    
    public void Start()
    {
        if (isPickupMode)
        {
            // Khởi tạo chế độ pickup
            InitializePickupMode();
        }
        else
        {
            // Khởi tạo chế độ vũ khí thông thường
            InitializeWeaponMode();
        }
    }
    
    // Khởi tạo chế độ vũ khí thông thường
    void InitializeWeaponMode()
    {
        currentAmmo = maxAmmo; // Initialize ammo
        UpdateAmmoUI(); // Update the ammo display
        UpdateScoreUI(); // Initialize the score display
        if (playerCamera == null)
        {
            playerCamera = Camera.main; // Get the main camera if not assigned
        }
        if (muzzleFlash == null)
        {
            Debug.LogWarning("Muzzle flash particle system is not assigned!");
        }
        if (gunshotSound == null)
        {
            Debug.LogWarning("Gunshot sound is not assigned!");
        }
        if (impactEffect == null)
        {
            Debug.LogWarning("Impact effect prefab is not assigned!");
        }
        
        // Find and store the weapon holder script
        weaponHolder = GetComponentInParent<SwitchWeapon>();
        if (weaponHolder == null)
        {
            Debug.LogWarning("Could not find SwitchWeapon script on parent. Weapon drop functionality will not work properly.");
        }
        
        // Lưu lại tên vũ khí nếu chưa có
        if (string.IsNullOrEmpty(weaponName))
        {
            weaponName = gameObject.name.Replace("(Clone)", "").Trim();
        }

        // Check if weaponDropPrefab is missing
        if (weaponDropPrefab == null)
            Debug.LogWarning("WeaponDropPrefab not assigned for " + gameObject.name + ". Weapon drop functionality will not work properly.");
    }
    
    // Khởi tạo chế độ pickup (vũ khí rơi trên mặt đất)
    void InitializePickupMode()
    {
        startPosition = transform.position;
        
        // Sau 2 giây, vũ khí có thể nhặt được
        StartCoroutine(EnablePickup());
        
        // Nếu không có Rigidbody, thêm vào
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody newRb = gameObject.AddComponent<Rigidbody>();
            newRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        // Nếu không có Collider, thêm BoxCollider
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true; // Để phát hiện va chạm nhưng không cản vật lý
        }

        // Setup Rigidbody for physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = true;
            rb.isKinematic = false;
        }
        
        // Setup Collider for trigger detection
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.isTrigger = false; // Start with physical collider
            boxCol.size = new Vector3(0.3f, 0.2f, 0.7f); // Adjust size to match weapon shape
            
            // Add a separate trigger collider for pickup detection
            GameObject pickupTrigger = new GameObject("PickupTrigger");
            pickupTrigger.transform.SetParent(transform);
            pickupTrigger.transform.localPosition = Vector3.zero;
            
            BoxCollider triggerCol = pickupTrigger.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(1f, 1f, 1f); // Larger area for pickup detection
            
            // Add a script to forward trigger events
            PickupTriggerForwarder forwarder = pickupTrigger.AddComponent<PickupTriggerForwarder>();
            forwarder.parentGun = this;
        }
        else
        {
            // If collider exists, ensure it's not a trigger for physics
            col.isTrigger = false;
            
            // Add a separate trigger collider for pickup detection if needed
            bool hasTriggerCollider = false;
            foreach (Collider childCol in GetComponentsInChildren<Collider>())
            {
                if (childCol.isTrigger)
                {
                    hasTriggerCollider = true;
                    break;
                }
            }
            
            if (!hasTriggerCollider)
            {
                GameObject pickupTrigger = new GameObject("PickupTrigger");
                pickupTrigger.transform.SetParent(transform);
                pickupTrigger.transform.localPosition = Vector3.zero;
                
                BoxCollider triggerCol = pickupTrigger.AddComponent<BoxCollider>();
                triggerCol.isTrigger = true;
                triggerCol.size = new Vector3(1f, 1f, 1f);
                
                PickupTriggerForwarder forwarder = pickupTrigger.AddComponent<PickupTriggerForwarder>();
                forwarder.parentGun = this;
            }
        }
        
        // Setup pickup prompt if available
        if (pickupPromptUI != null)
            pickupPromptUI.SetActive(false);
    }
    
    // Cho phép nhặt vũ khí sau 2 giây
    IEnumerator EnablePickup()
    {
        yield return new WaitForSeconds(2f);
        isPickable = true;

        // After settling, make pickable and enable bobbing
        isPickable = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Make it stay in place
        }
        
        // Update start position for bobbing
        startPosition = transform.position;
    }
    
    void OnEnable()
    {
        if (!isPickupMode)
        {
            isReloading = false; // Reset reloading state when the gun is enabled
            if (animator != null)
                animator.SetBool("Reloading", false); // Stop reload animation
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isPickupMode)
        {
            // Chế độ vũ khí rơi trên mặt đất
            UpdatePickupMode();
        }
        else
        {
            // Chế độ vũ khí trong tay
            UpdateWeaponMode();
        }
    }
    
    // Cập nhật trong chế độ vũ khí
    void UpdateWeaponMode()
    {
        if (isReloading)
        {
            return; // Prevent shooting while reloading
        }

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (isAutomatic)
        {
            // Allow continuous firing for automatic guns
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate; // Set the next fire time
                Shoot();
            }
        }
        else
        {
            // Allow single-shot firing for semi-automatic guns
            if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate; // Set the next fire time
                Shoot();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload()); // Start reloading when pressing "R"
        }
        
        // Handle weapon drop when pressing G
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropWeapon();
        }
    }
    
    // Cập nhật trong chế độ pickup
    void UpdatePickupMode()
    {
        // Xoay vũ khí để thu hút sự chú ý
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Hiệu ứng nhấp nhô lên xuống
        if (isPickable)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    // Xử lý va chạm khi ở chế độ pickup
    void OnTriggerEnter(Collider other)
    {
        if (!isPickupMode) return;
        
        // Khi người chơi đi vào vùng có vũ khí
        if (isPickable && other.CompareTag("Player"))
        {
            Debug.Log("Player entered weapon pickup zone");
            // Hiển thị thông báo "Press E to pick up"
            ShowPickupPrompt(true);
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (!isPickupMode) return;
        
        // Khi người chơi đứng gần và nhấn E
        if (isPickable && other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            // Tìm vũ khí hiện tại của người chơi
            Gun playerGun = FindPlayerGun(other.gameObject);
            if (playerGun != null)
            {
                // Nếu người chơi đang cầm vũ khí cùng loại, thì thả vũ khí đó xuống
                if ((isLargeWeapon && playerGun.isLargeWeapon) ||
                    (!isLargeWeapon && !playerGun.isLargeWeapon))
                {
                    playerGun.DropWeapon();
                }
                
                // Nói cho SwitchWeapon biết để tạo vũ khí mới
                SwitchWeapon weaponHolder = other.GetComponentInChildren<SwitchWeapon>();
                if (weaponHolder != null)
                {
                    weaponHolder.PickupWeapon(this);
                    Destroy(gameObject); // Xóa vũ khí khỏi mặt đất
                }
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!isPickupMode) return;
        
        // Khi người chơi rời khỏi vùng có vũ khí
        if (other.CompareTag("Player"))
        {
            ShowPickupPrompt(false);
        }
    }
    
    // Tìm vũ khí hiện tại của người chơi
    Gun FindPlayerGun(GameObject player)
    {
        // Tìm kiếm trong các con của đối tượng SwitchWeapon
        SwitchWeapon weaponHolder = player.GetComponentInChildren<SwitchWeapon>();
        if (weaponHolder != null)
        {
            foreach (Transform child in weaponHolder.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    return child.GetComponent<Gun>();
                }
            }
        }
        return null;
    }
    
    // Hiển thị/ẩn thông báo nhặt vũ khí
    void ShowPickupPrompt(bool show)
    {
        // Có thể thêm UI thông báo "Press E to pickup" ở đây
        // Ví dụ: pickupPromptUI.SetActive(show);
        
        Debug.Log(show ? "Press E to pick up weapon" : "");
    }

    // Method to drop the current weapon
    public void DropWeapon()
    {
        if (weaponHolder != null && weaponDropPrefab != null)
        {
            // Tạo vị trí thả vũ khí (hơi phía trước người chơi)
            Vector3 dropPosition = transform.position + transform.forward * 0.5f;
            
            // Create the weapon pickup at the current position
            GameObject droppedWeapon = Instantiate(weaponDropPrefab, dropPosition, transform.rotation);
            
            // Add rigidbody for physics if needed
            Rigidbody rb = droppedWeapon.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply force in the forward direction to throw the weapon
                rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
                // Thêm lực xoay ngẫu nhiên để tạo cảm giác tự nhiên
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
            
            // Transfer weapon stats to the pickup
            Gun pickupGun = droppedWeapon.GetComponent<Gun>();
            if (pickupGun != null)
            {
                // ĐẶC BIỆT QUAN TRỌNG: Đảm bảo rằng vũ khí rơi cũng có tham chiếu đến prefab
                // để có thể thả lại sau khi nhặt
                pickupGun.weaponDropPrefab = this.weaponDropPrefab;
                
                // Chuyển sang chế độ pickup
                pickupGun.isPickupMode = true;
                pickupGun.isLargeWeapon = this.isLargeWeapon;
                pickupGun.currentAmmo = this.currentAmmo;
                pickupGun.maxAmmo = this.maxAmmo;
                pickupGun.damage = this.damage;
                pickupGun.weaponName = this.weaponName;
                pickupGun.range = this.range;
                pickupGun.fireRate = this.fireRate;
                pickupGun.isAutomatic = this.isAutomatic;
                
                // Sao chép tham chiếu đến animator cho vũ khí rơi nếu cần
                if (this.animator != null)
                {
                    Animator droppedAnimator = droppedWeapon.GetComponent<Animator>();
                    if (droppedAnimator != null)
                    {
                        pickupGun.animator = droppedAnimator;
                    }
                }
                
                // Đặt lại để khởi tạo ngay
                pickupGun.Start();
                
                Debug.Log("Dropped weapon with weaponDropPrefab reference: " + (pickupGun.weaponDropPrefab != null ? "OK" : "Missing"));
            }
            else
            {
                Debug.LogError("Dropped weapon prefab doesn't have Gun component!");
            }
            
            // Deactivate current weapon and notify the weapon holder
            gameObject.SetActive(false);
        }
        else
        {
            if (weaponHolder == null)
                Debug.LogError("Cannot drop weapon - missing WeaponHolder reference!");
            if (weaponDropPrefab == null)
                Debug.LogError("Cannot drop weapon - missing WeaponDropPrefab!");
        }
    }
    
    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");
        
        if (animator != null)
            animator.SetBool("Reloading", true); // Play reload animation

        yield return new WaitForSeconds(reloadTime);

        if (animator != null)
            animator.SetBool("Reloading", false); // Stop reload animation
        
        currentAmmo = maxAmmo; // Refill ammo
        UpdateAmmoUI(); // Update the ammo display after reloading
        isReloading = false;
    }

    void Shoot()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }

        currentAmmo--; // Decrease ammo count
        UpdateAmmoUI(); // Update the ammo display after shooting
        
        if (gunshotSound != null)
            gunshotSound.Play(); // Play the gunshot sound
        
        if (muzzleFlash != null)
            muzzleFlash.Play(); // Play the muzzle flash effect
        
        RaycastHit hit;
        if (Physics.Raycast(playerCamera != null ? playerCamera.transform.position : transform.position, 
                          playerCamera != null ? playerCamera.transform.forward : transform.forward, 
                          out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);
            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);
                IncreaseScore(10); // Increase score by 10 when hitting a zombie
            }
            
            // Check for weapon pickup (now handled in Gun script)
            Gun gunPickup = hit.transform.GetComponent<Gun>();
            if (gunPickup != null && gunPickup.isPickupMode)
            {
                // Được xử lý bởi OnTriggerStay nếu người chơi nhấn E
            }
            
            Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = hit.point - transform.position;
                forceDirection.Normalize();
                rb.AddForce(forceDirection * impactForce, ForceMode.Impulse);
            }
            
            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }
        }
    }

    void IncreaseScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}"; // Update the text to show current and max ammo
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}"; // Update the score display
        }
    }

    // These methods are called by the PickupTriggerForwarder
    public void OnWeaponTriggerEnter(Collider other)
    {
        if (!isPickupMode || !isPickable) return;
        
        if (other.CompareTag("Player"))
        {
            ShowPickupPrompt(true);
            Debug.Log("Player can pick up weapon: " + gameObject.name);
        }
    }
    
    public void OnWeaponTriggerStay(Collider other)
    {
        if (!isPickupMode || !isPickable) return;
        
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            // Try to find player's current weapon
            Gun playerGun = FindPlayerGun(other.gameObject);
            SwitchWeapon playerWeaponHolder = other.GetComponentInChildren<SwitchWeapon>();
            
            if (playerWeaponHolder != null)
            {
                // If player has a weapon of same type, make that weapon drop first
                if (playerGun != null && 
                    ((isLargeWeapon && playerGun.isLargeWeapon) || 
                    (!isLargeWeapon && !playerGun.isLargeWeapon)))
                {
                    playerGun.DropWeapon();
                }
                
                // Tell the weapon holder to spawn the new weapon
                playerWeaponHolder.PickupWeapon(this);
                
                // Destroy the dropped weapon
                Destroy(gameObject);
                
                Debug.Log("Player picked up: " + gameObject.name);
            }
        }
    }
    
    public void OnWeaponTriggerExit(Collider other)
    {
        if (!isPickupMode) return;
        
        if (other.CompareTag("Player"))
            ShowPickupPrompt(false);
    }
}

// Helper class to forward trigger events to the parent gun
[RequireComponent(typeof(Collider))]
public class PickupTriggerForwarder : MonoBehaviour
{
    public Gun parentGun;
    
    void OnTriggerEnter(Collider other)
    {
        if (parentGun != null)
            parentGun.OnWeaponTriggerEnter(other);
    }
    
    void OnTriggerStay(Collider other)
    {
        if (parentGun != null)
            parentGun.OnWeaponTriggerStay(other);
    }
    
    void OnTriggerExit(Collider other)
    {
        if (parentGun != null)
            parentGun.OnWeaponTriggerExit(other);
    }
}
