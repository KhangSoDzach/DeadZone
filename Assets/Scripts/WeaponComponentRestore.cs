using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Helper script to restore missing components on weapons after pickup
public class WeaponComponentRestore : MonoBehaviour
{
    private void Start()
    {
        // Run a delayed check to restore components after everything is initialized
        StartCoroutine(DelayedComponentCheck());
    }
    
    private IEnumerator DelayedComponentCheck()
    {
        // Wait a moment for everything else to initialize
        yield return new WaitForSeconds(0.5f);
        
        // Find all Gun components
        Gun[] allGuns = FindObjectsOfType<Gun>(true); // Include inactive weapons
        
        foreach (Gun gun in allGuns)
        {
            RestoreGunComponents(gun);
        }
        
        Debug.Log($"WeaponComponentRestore: Đã kiểm tra {allGuns.Length} vũ khí");
    }
    
    // Called when weapon is picked up or created
    public void RestoreGunComponents(Gun gun)
    {
        if (gun == null) return;
        
        // Check for missing components
        bool componentsFixed = false;
        
        // 1. Check Animator
        if (gun.animator == null)
        {
            gun.animator = gun.GetComponent<Animator>();
            if (gun.animator == null)
                gun.animator = gun.GetComponentInChildren<Animator>();
            if (gun.animator == null)
                gun.animator = gun.gameObject.AddComponent<Animator>();
                
            componentsFixed = true;
            Debug.Log($"Restored animator on {gun.name}");
        }
        
        // 2. Check AudioSource
        if (gun.gunshotSound == null)
        {
            gun.gunshotSound = gun.GetComponent<AudioSource>();
            if (gun.gunshotSound == null)
                gun.gunshotSound = gun.GetComponentInChildren<AudioSource>();
            if (gun.gunshotSound == null)
            {
                AudioSource newAudio = gun.gameObject.AddComponent<AudioSource>();
                newAudio.playOnAwake = false;
                gun.gunshotSound = newAudio;
            }
            
            componentsFixed = true;
            Debug.Log($"Restored AudioSource on {gun.name}");
        }
        
        // 3. Check impactEffect
        if (gun.impactEffect == null)
        {
            // Try to find a default impact effect in the Resources folder
            GameObject defaultImpact = Resources.Load<GameObject>("DefaultImpact");
            if (defaultImpact != null)
            {
                gun.impactEffect = defaultImpact;
                componentsFixed = true;
                Debug.Log($"Restored impactEffect on {gun.name}");
            }
        }
        
        // 4. Check UI references
        if (gun.ammoText == null)
        {
            // Find UI components by common naming patterns
            Text[] allTexts = FindObjectsOfType<Text>();
            foreach (Text text in allTexts)
            {
                if (text.name.ToLower().Contains("ammo"))
                {
                    gun.ammoText = text;
                    componentsFixed = true;
                    Debug.Log($"Found ammoText: {text.name}");
                    break;
                }
            }
        }
        
        // 5. Check for ScoreManager and create if needed
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            GameObject scoreManagerObject = new GameObject("ScoreManager");
            scoreManager = scoreManagerObject.AddComponent<ScoreManager>();
            scoreManager.FindScoreText();
            componentsFixed = true;
            Debug.Log("Created ScoreManager");
        }
        else if (scoreManager.scoreText == null)
        {
            // Find a score text for the ScoreManager
            Text[] allTexts = FindObjectsOfType<Text>();
            foreach (Text text in allTexts)
            {
                if (text.name.ToLower().Contains("score"))
                {
                    scoreManager.scoreText = text;
                    scoreManager.UpdateScoreUI();
                    componentsFixed = true;
                    Debug.Log($"Found and assigned scoreText to ScoreManager: {text.name}");
                    break;
                }
            }
        }
        
        // 6. If playerCamera is missing, use main camera
        if (gun.playerCamera == null)
        {
            gun.playerCamera = Camera.main;
            componentsFixed = true;
            Debug.Log($"Set Camera.main as playerCamera for {gun.name}");
        }
        
        // 7. Now update the UI to show the current ammo
        gun.UpdateAmmoUI();
        
        if (componentsFixed)
        {
            Debug.Log($"WeaponComponentRestore: Fixed missing components on {gun.name}");
        }
    }
}
