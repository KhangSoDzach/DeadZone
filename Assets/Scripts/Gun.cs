using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Import for UI components

public class Gun : MonoBehaviour
{
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

    void Start()
    {
        currentAmmo = maxAmmo; // Initialize ammo
        UpdateAmmoUI(); // Update the ammo display
        if (playerCamera == null)
        {
            playerCamera = Camera.main; // Get the main camera if not assigned
        }
        if (muzzleFlash == null)
        {
            Debug.LogError("Muzzle flash particle system is not assigned!");
        }
        if (gunshotSound == null)
        {
            Debug.LogError("Gunshot sound is not assigned!");
        }
        if (impactEffect == null)
        {
            Debug.LogError("Impact effect prefab is not assigned!");
        }
    }
    void OnEnable()
    {
        isReloading = false; // Reset reloading state when the gun is enabled
        animator.SetBool("Reloading", false); // Stop reload animation
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
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);
            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
            // Apply force to the object hit by the raycast
            Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = hit.point - transform.position;
                forceDirection.Normalize();
                rb.AddForce(forceDirection * impactForce, ForceMode.Impulse);
            }
            GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 2f); // Destroy the impact effect after 2 seconds
        }
    }

    void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}"; // Update the text to show current and max ammo
        }
    }
}
