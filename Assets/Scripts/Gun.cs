using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Import for UI components
using Scripts;
using Scripts.API; // Add this using statement

public class Gun : MonoBehaviour
{
    public float damage = 10f; 
    public float range = 100f; 
    public bool isAutomatic = false; // Determines if the gun is automatic (rifle) or semi-automatic (pistol)
    public bool isPistol = false;    // Is this a pistol (primary weapon that can't be dropped)
    public int maxAmmo = 30; // Maximum ammo for the gun
    public int currentAmmo; // Current ammo in the gun
    public int totalAmmo = 120; // Tổng số đạn người chơi mang theo cho loại súng này
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

    [Header("Recoil Settings")]
    public float recoilAmount = 1.0f;               // How much recoil each shot produces
    public float recoilRecoverySpeed = 2.0f;        // How fast the view returns to normal
    public float recoilBuildup = 0.5f;              // How fast recoil builds up during sustained fire
    public float horizontalRecoilFactor = 0.3f;     // How much left-right recoil (0-1)
    private PlayerLook playerLookScript;            // Reference to the player look script

    [Header("Bullet Spread Settings")]
    public float baseSpread = 0.02f;              // Base spread when standing still
    public float maxSpread = 0.06f;               // Maximum spread when moving/shooting continuously
    public float spreadIncreasePerShot = 0.01f;   // How much spread increases per shot
    public float spreadRecoveryTime = 0.5f;       // How fast spread recovers when not shooting
    public float currentSpread;                  // Current spread value
    private float lastShotTime;                   // Time when last shot was fired

    [Header("Blood Effects")]
    public GameObject bloodSplatterEffect;  // Hiệu ứng máu bắn tóe
    public GameObject bloodDecalPrefab;     // Prefab vết máu trên sàn
    public float bloodDecalLifetime = 10f;  // Thời gian tồn tại của vết máu (giây)
    public float bloodSplatterChance = 0.8f; // Tỷ lệ xuất hiện hiệu ứng máu (0-1)

    [Header("Weapon Progression")]
    public string weaponName = "Default Gun"; // Name of the weapon (used for API integration)
    public int currentLevel = 1;              // Current upgrade level of the weapon
    public int maxLevel = 5;                  // Maximum level the weapon can be upgraded to

    void Start()
    {
        currentAmmo = maxAmmo; // Initialize ammo
        
        // Make sure all required components are available
        EnsureRequiredComponents();
        
        UpdateAmmoUI(); // Update the ammo display
        
        currentSpread = baseSpread;
        lastShotTime = -10f; // Initialize to ensure we start with base spread
    }
    
    // New method to ensure all required components exist
    void EnsureRequiredComponents()
    {
        // Ensure the animator reference is valid
        if (animator == null)
        {
            // First check if there's an animator in the parent (weapon holder)
            if (transform.parent != null)
            {
                animator = transform.parent.GetComponent<Animator>();
                if (animator != null)
                {
                    Debug.Log($"Using parent's animator from {transform.parent.name} for {gameObject.name}");
                }
            }
            
            // If no parent animator found, check for weapon pickup component
            if (animator == null)
            {
                WeaponPickup pickup = GetComponent<WeaponPickup>();
                if (pickup != null && pickup.animatorController != null)
                {
                    // Create new animator with the saved controller from the original weapon
                    Animator newAnimator = gameObject.GetComponent<Animator>() ?? gameObject.AddComponent<Animator>();
                    newAnimator.runtimeAnimatorController = pickup.animatorController;
                    animator = newAnimator;
                    Debug.Log($"Restored animator with saved controller for {gameObject.name}");
                }
                else
                {
                    // Only as last resort, try standard component search
                    animator = GetComponent<Animator>();
                    if (animator == null)
                        animator = GetComponentInChildren<Animator>();
                    if (animator == null)
                    {
                        animator = gameObject.AddComponent<Animator>();
                        Debug.LogWarning($"Created new Animator for {gameObject.name} as last resort");
                    }
                }
            }
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main; // Get the main camera if not assigned
            Debug.Log("Set Camera.main as playerCamera");
        }
        
        if (muzzleFlash == null)
        {
            // Try to find in children
            muzzleFlash = GetComponentInChildren<ParticleSystem>();
            if (muzzleFlash == null)
            {
                Debug.LogWarning("Muzzle flash particle system not found.");
            }
        }
        
        if (gunshotSound == null)
        {
            gunshotSound = GetComponent<AudioSource>();
            if (gunshotSound == null)
            {
                gunshotSound = gameObject.AddComponent<AudioSource>();
                gunshotSound.playOnAwake = false;
                Debug.LogWarning($"Created new AudioSource for {gameObject.name}");
            }
        }
        
        if (impactEffect == null)
        {
            // Try to find default impact in resources
            GameObject defaultImpact = Resources.Load<GameObject>("DefaultImpact");
            if (defaultImpact != null)
            {
                impactEffect = defaultImpact;
                Debug.Log("Using default impact effect from Resources");
            }
            else
            {
                Debug.LogWarning("Impact effect prefab not found. Please assign in Inspector.");
            }
        }
        
        // Try to find UI references
        if (ammoText == null)
        {
            Text[] allTexts = FindObjectsOfType<Text>();
            
            foreach (Text text in allTexts)
            {
                if (ammoText == null && text.name.ToLower().Contains("ammo"))
                {
                    ammoText = text;
                    Debug.Log($"Found ammoText: {text.name}");
                }
            }
        }
        
        // Look for PlayerLook script if needed
        if (playerLookScript == null && playerCamera != null)
        {
            playerLookScript = playerCamera.GetComponentInParent<PlayerLook>();
            if (playerLookScript == null)
            {
                playerLookScript = playerCamera.transform.root.GetComponent<PlayerLook>();
            }
        }
        
        // Kiểm tra hiệu ứng máu
        if (bloodSplatterEffect == null)
        {
            // Thử tìm bloodSplatterEffect trong resources
            GameObject defaultBloodEffect = Resources.Load<GameObject>("BloodSplatter");
            if (defaultBloodEffect != null)
            {
                bloodSplatterEffect = defaultBloodEffect;
                Debug.Log("Using default blood splatter effect from Resources");
            }
            else
            {
                Debug.LogWarning("Blood splatter effect not assigned. Blood effects will be disabled.");
            }
        }
        
        // Kiểm tra prefab vết máu
        if (bloodDecalPrefab == null)
        {
            // Thử tìm bloodDecalPrefab trong resources
            GameObject defaultBloodDecal = Resources.Load<GameObject>("BloodDecal");
            if (defaultBloodDecal != null)
            {
                bloodDecalPrefab = defaultBloodDecal;
                Debug.Log("Using default blood decal from Resources");
            }
            else
            {
                Debug.LogWarning("Blood decal prefab not assigned. Floor blood will be disabled.");
            }
        }
    }

    void OnEnable()
    {
        isReloading = false; // Reset reloading state when the gun is enabled
        
        // Chỉ set animator nếu animator tồn tại
        if (animator != null)
        {
            animator.SetBool("Reloading", false); // Stop reload animation
        }
    }    // Update is called once per frame
    void Update()
    {
        // Check if game is paused before processing any input
        if (PauseMenu.IsGamePaused)
        {
            return; // Exit early if game is paused
        }

        if (isReloading)
        {
            return; // Prevent shooting while reloading
        }

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }
        
        // Check if shop is open using direct reference to ShopManagement
        // This is more reliable and doesn't require the ShopWeaponBlocker to be in the scene
        ShopManagement shopManager = FindObjectOfType<ShopManagement>();
        if (shopManager != null && shopManager.IsShopOpen())
        {
            return; // Prevent shooting while shop is open
        }
        
        // Also check the ShopWeaponBlocker as a backup if it exists in the scene
        if (ShopWeaponBlocker.Instance != null && ShopWeaponBlocker.ShouldBlockWeaponFiring())
        {
            return; // Prevent shooting while shop is open (via the blocker)
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

        // Recover spread when not shooting
        if (Time.time - lastShotTime > spreadRecoveryTime)
        {
            currentSpread = Mathf.Lerp(currentSpread, baseSpread, Time.deltaTime * 3f);
        }
    }

    IEnumerator Reload()
    {
        // Kiểm tra nếu không còn đạn dự trữ hoặc đã đầy đạn
        if (totalAmmo <= 0 || currentAmmo == maxAmmo)
        {
            if (currentAmmo == maxAmmo)
                Debug.Log("Đạn đã đầy!");
            else
                Debug.Log("Không còn đạn dự trữ!");
            yield break;
        }
            
        isReloading = true;
        Debug.Log("Đang nạp đạn...");
        
        if (animator != null)
            animator.SetBool("Reloading", true); // Play reload animation

        yield return new WaitForSeconds(reloadTime);

        if (animator != null)
            animator.SetBool("Reloading", false); // Stop reload animation
            
        // Tính toán số đạn cần nạp
        int ammoToReload = maxAmmo - currentAmmo; // Số đạn cần để nạp đầy băng
        
        // Kiểm tra nếu đạn dự trữ không đủ để nạp đầy băng
        if (totalAmmo < ammoToReload)
        {
            // Nếu đạn dự trữ không đủ, nạp tất cả số đạn dự trữ còn lại
            currentAmmo += totalAmmo;
            totalAmmo = 0;
        }
        else
        {
            // Nếu đạn dự trữ đủ, nạp đầy băng
            totalAmmo -= ammoToReload;
            currentAmmo = maxAmmo;
        }
        
        UpdateAmmoUI(); // Cập nhật hiển thị đạn sau khi nạp đạn
        isReloading = false;
    }

    void Shoot()
    {
        // Don't shoot if game is paused
        if (PauseMenu.IsGamePaused)
        {
            return;
        }
        
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }

        currentAmmo--; // Decrease ammo count
        UpdateAmmoUI(); // Update the ammo display after shooting
        gunshotSound.Play(); // Play the gunshot sound
        muzzleFlash.Play(); // Play the muzzle flash effect
        
        // Apply enhanced recoil effect
        if (playerLookScript != null)
        {
            // Set recoil parameters based on this weapon's characteristics
            playerLookScript.SetRecoilParameters(recoilBuildup, horizontalRecoilFactor);
            
            // Set recovery speed
            playerLookScript.SetRecoilRecoverySpeed(recoilRecoverySpeed);
            
            // Apply actual recoil - will be different each shot
            playerLookScript.ApplyRecoil(recoilAmount);
        }
        
        // Increase spread with each shot
        currentSpread = Mathf.Min(maxSpread, currentSpread + spreadIncreasePerShot);
        lastShotTime = Time.time;
        
        // Apply bullet spread
        Vector3 spreadVector = CalculateSpreadVector();
        
        RaycastHit hit;
        // Use camera's position and direction with added spread for the raycast
        Ray ray = new Ray(playerCamera.transform.position, spreadVector);
        
        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);
            
            // Kiểm tra nếu bắn trúng zombie
            bool hitZombie = false;
            float damageDealt = 0;
            
            Zombie_1 zombie1 = hit.transform.GetComponent<Zombie_1>();
            Zombie_2 zombie2 = hit.transform.GetComponent<Zombie_2>();
            Zombie_3 zombie3 = hit.transform.GetComponent<Zombie_3>();
            Zombie_4 zombie4 = hit.transform.GetComponent<Zombie_4>();
            ZombieMiniboss ZombieMiniboss = hit.transform.GetComponent<ZombieMiniboss>();
            Boss Boss = hit.transform.GetComponent<Boss>();

            // Use ScoreManager to add score directly
            if (zombie1 != null)
            {
                zombie1.zombieGotHit(damage);
                hitZombie = true;
                damageDealt = damage;
            }
            if (zombie2 != null)
            {
                zombie2.zombieGotHit(damage);
                hitZombie = true;
                damageDealt = damage;
            }
            if (zombie3 != null)
            {
                zombie3.zombieGotHit(damage);
                hitZombie = true;
                damageDealt = damage;
            }
            if (zombie4 != null)
            {
                zombie4.zombieGotHit(damage);
                hitZombie = true;
                damageDealt = damage;
            }
            if (ZombieMiniboss != null)
            {
                ZombieMiniboss.zombieGotHit(damage);
            }
            if (Boss != null)
            {
                Boss.zombieGotHit(damage);
            }
            Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = hit.point - playerCamera.transform.position;
                forceDirection.Normalize();
                rb.AddForce(forceDirection * impactForce, ForceMode.Impulse);
            }
            
            if (hitZombie && Random.value <= bloodSplatterChance)
            {
                CreateBloodEffects(hit, damageDealt);
            }
            else
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }
        }
    }    public void IncreaseScore(int points)
    {
        ScoreManager.Instance.AddScore(points);
    }

    private Vector3 CalculateSpreadVector()
    {
        // Start with camera forward direction (center of screen)
        Vector3 direction = playerCamera.transform.forward;
        
        // Add random spread within a circle
        float spreadX = Random.Range(-currentSpread, currentSpread);
        float spreadY = Random.Range(-currentSpread, currentSpread);
        
        // Add spread to direction
        direction += playerCamera.transform.right * spreadX;
        direction += playerCamera.transform.up * spreadY;
        
        // Normalize to ensure consistent range
        return direction.normalized;
    }

    // Update the ammo display
    public void UpdateAmmoUI()
    {
        // Update ammo UI if available
        if (ammoText != null)
        {
            ammoText.text = currentAmmo + " / " + totalAmmo;
        }
    }
    
    // Set the weapon level and update stats accordingly
    public void SetLevel(int level)
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        if (level == currentLevel) return;
        
        currentLevel = level;
        
        // Scale damage based on weapon level
        damage *= (1f + (currentLevel - 1) * 0.2f);
        
        // Other stat improvements based on level could be added here
        Debug.Log($"Weapon {weaponName} upgraded to level {currentLevel}");
    }
    
    // Set the current ammo of the weapon
    public void SetAmmo(int ammo)
    {
        currentAmmo = Mathf.Clamp(ammo, 0, maxAmmo);
        UpdateAmmoUI();
    }
    
    // This method will be called from WeaponManager when this weapon is enabled
    public void OnWeaponEnabled()
    {
        // Make sure all components are ready
        EnsureRequiredComponents();
        
        // Update the UI
        UpdateAmmoUI();
    }
    
    // Add ammo to the total ammo count
    public void AddAmmo(int amount)
    {
        if (amount <= 0) return;
        
        totalAmmo += amount;
        UpdateAmmoUI();
    }

    // Method to set maxAmmo (for weapon upgrades)
    public void SetMaxAmmo(int newMaxAmmo)
    {
        maxAmmo = newMaxAmmo;
        UpdateAmmoUI();
    }

    // Method to set reload time (for weapon upgrades)
    public void SetReloadTime(float newReloadTime)
    {
        reloadTime = newReloadTime;
    }

    // Method to set damage (for weapon upgrades)
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    private void CreateBloodEffects(RaycastHit hit, float damageAmount)
    {
        if (bloodSplatterEffect != null)
        {
            // Tạo hiệu ứng máu bắn tóe tại điểm va chạm
            GameObject bloodSplatter = Instantiate(bloodSplatterEffect, hit.point, Quaternion.LookRotation(-hit.normal));
            
            // Điều chỉnh kích thước hiệu ứng dựa vào sát thương
            float scale = Mathf.Clamp(damageAmount / 30f, 0.5f, 2.0f);
            bloodSplatter.transform.localScale *= scale;
            
            // Gắn bloodSplatter vào zombie để nó di chuyển theo
            bloodSplatter.transform.SetParent(hit.transform);
            
            // Hủy hiệu ứng sau một khoảng thời gian ngắn hơn
            Destroy(bloodSplatter, .3f); // Giảm thời gian từ 2f xuống 0.3f
            
            // Xoay ngẫu nhiên hiệu ứng máu
            bloodSplatter.transform.Rotate(0, 0, Random.Range(0, 360));
        }
        else
        {
            Debug.LogWarning("Blood splatter effect prefab is missing!");
        }
        
        CreateBloodDecal(hit);
    }

    private void CreateBloodDecal(RaycastHit zombieHit)
    {
        if (bloodDecalPrefab == null) return;
        
        // Kiểm tra xem có sàn/mặt đất phía dưới không
        RaycastHit floorHit;
        if (Physics.Raycast(zombieHit.point, Vector3.down, out floorHit, 3f))
        {
            //Create a blood decal at the hit point
            Quaternion decalRotation = Quaternion.FromToRotation(Vector3.up, floorHit.normal);
            GameObject bloodDecal = Instantiate(bloodDecalPrefab, floorHit.point + floorHit.normal * 0.01f, decalRotation);
            
            // Adjust the size of the blood decal based on the damage amount
            float randomScale = Random.Range(0.8f, 1.5f);
            bloodDecal.transform.localScale *= randomScale;
            
            // Randomly rotate the blood decal for variety
            bloodDecal.transform.Rotate(0, Random.Range(0, 360), 0);
            
            // Destroy the blood decal after a certain time
            Destroy(bloodDecal, bloodDecalLifetime);
        }
    }

    private void CreateBloodBurstEffect(RaycastHit hit, float damageAmount)
    {
        // Kiểm tra xem bloodSplatterEffect có chứa ParticleSystem không
        ParticleSystem bloodParticles = bloodSplatterEffect.GetComponent<ParticleSystem>();
        
        if (bloodParticles != null)
        {
            // Tạo hiệu ứng particle tại điểm va chạm
            ParticleSystem burstEffect = Instantiate(bloodParticles, hit.point, Quaternion.LookRotation(-hit.normal));
            
            // Gắn vào zombie để di chuyển theo
            burstEffect.transform.SetParent(hit.transform);
            
            // Điều chỉnh số lượng particle dựa vào sát thương
            var mainModule = burstEffect.main;
            mainModule.startSizeMultiplier *= Mathf.Clamp(damageAmount / 20f, 0.7f, 1.5f);
            
            // Phát một lần duy nhất, không lặp lại
            burstEffect.Stop();
            burstEffect.Play();
            
            // Hủy particle system sau khi nó hoàn thành
            Destroy(burstEffect.gameObject, burstEffect.main.duration + 0.1f);
        }
    }
}
