using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Import for UI components

public class Gun : MonoBehaviour
{
    public float damage = 10f; 
    public float range = 100f; 
    public bool isAutomatic = false; // Determines if the gun is automatic (rifle) or semi-automatic (pistol)
    public bool isPistol = false;    // Is this a pistol (primary weapon that can't be dropped)
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
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    animator = gameObject.AddComponent<Animator>();
                    Debug.LogWarning($"Created new Animator for {gameObject.name}");
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
    }

    void OnEnable()
    {
        isReloading = false; // Reset reloading state when the gun is enabled
        
        // Chỉ set animator nếu animator tồn tại
        if (animator != null)
        {
            animator.SetBool("Reloading", false); // Stop reload animation
        }
    }

    // Update is called once per frame
    void Update()
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

        // Recover spread when not shooting
        if (Time.time - lastShotTime > spreadRecoveryTime)
        {
            currentSpread = Mathf.Lerp(currentSpread, baseSpread, Time.deltaTime * 3f);
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");
        animator.SetBool("Reloading", true); // Play reload animation

        yield return new WaitForSeconds(reloadTime);

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
            Zombie_1 zombie1 = hit.transform.GetComponent<Zombie_1>();
            Zombie_2 zombie2 = hit.transform.GetComponent<Zombie_2>();
            Zombie_3 zombie3 = hit.transform.GetComponent<Zombie_3>();
            Zombie_4 zombie4 = hit.transform.GetComponent<Zombie_4>();

                // Use ScoreManager to add score directly
            if (zombie1 != null)
            {
                zombie1.zombieGotHit(damage);
                ScoreManager.AddScore((int)damage);
            }
            if (zombie2 != null)
            {
                zombie2.zombieGotHit(damage);
                ScoreManager.AddScore((int)damage);
            }
            if (zombie3 != null)
            {
                zombie3.zombieGotHit(damage);
                ScoreManager.AddScore((int)damage);
            }
            if (zombie4 != null)
            {
                zombie4.zombieGotHit(damage);
                ScoreManager.AddScore((int)damage);
            }
            Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                
                Vector3 forceDirection = hit.point - playerCamera.transform.position;
                forceDirection.Normalize();
                rb.AddForce(forceDirection * impactForce, ForceMode.Impulse);
            }
            
            // Instantiate impact effect at hit point
            GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 2f); 
        }
    }

    // Update to use ScoreManager for score management
    public void IncreaseScore(int points)
    {
        ScoreManager.AddScore(points);
    }

    // Calculate direction with random spread
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

    // Make this public so it can be called from WeaponManager
    public void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}"; // Update the text to show current and max ammo
        }
    }
    
    // This method will be called from WeaponManager when this weapon is enabled
    public void OnWeaponEnabled()
    {
        // Make sure all components are ready
        EnsureRequiredComponents();
        
        // Update the UI
        UpdateAmmoUI();
    }
}
