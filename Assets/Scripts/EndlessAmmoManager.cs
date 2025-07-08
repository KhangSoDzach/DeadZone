using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages infinite ammo for pistols in Endless mode
/// This script automatically detects all pistols in the scene and enables infinite ammo
/// </summary>
public class EndlessAmmoManager : MonoBehaviour
{
    [Header("Endless Mode Settings")]
    [SerializeField] private bool enableInfiniteAmmo = true;
    [SerializeField] private bool onlyForPistols = true;
    [SerializeField] private bool debugMode = true;
    
    [Header("Scene Detection")]
    [SerializeField] private string[] endlessSceneNames = {"Endless", "EndlessMode", "Survival"};
    
    private List<Gun> weaponsInScene = new List<Gun>();
    private bool isEndlessMode = false;
    
    void Start()
    {
        CheckEndlessMode();
        
        if (isEndlessMode)
        {
            StartCoroutine(SetupInfiniteAmmo());
        }
    }
    
    /// <summary>
    /// Check if current scene is an endless mode scene
    /// </summary>
    void CheckEndlessMode()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        foreach (string sceneName in endlessSceneNames)
        {
            if (currentSceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                isEndlessMode = true;
                DebugLog($"Endless mode detected in scene: {currentSceneName}");
                return;
            }
        }
        
        isEndlessMode = false;
        DebugLog($"Not in endless mode. Current scene: {currentSceneName}");
    }
    
    /// <summary>
    /// Setup infinite ammo for all pistols in the scene
    /// </summary>
    IEnumerator SetupInfiniteAmmo()
    {
        // Wait a frame to ensure all weapons are initialized
        yield return null;
        
        // Find all Gun components in the scene
        Gun[] allGuns = FindObjectsOfType<Gun>();
        weaponsInScene.Clear();
        
        int pistolsModified = 0;
        int riflesModified = 0;
        
        foreach (Gun gun in allGuns)
        {
            // Check if this weapon should have infinite ammo
            bool shouldHaveInfiniteAmmo = enableInfiniteAmmo && (!onlyForPistols || gun.isPistol);
            
            if (shouldHaveInfiniteAmmo)
            {
                gun.SetInfiniteAmmo(true);
                weaponsInScene.Add(gun);
                
                if (gun.isPistol)
                {
                    pistolsModified++;
                    DebugLog($"Enabled infinite reserve ammo for pistol: {gun.weaponName}");
                }
                else
                {
                    riflesModified++;
                    DebugLog($"Enabled infinite reserve ammo for rifle: {gun.weaponName}");
                }
            }
        }
        
        DebugLog($"Endless Ammo Manager: Modified {pistolsModified} pistols and {riflesModified} rifles");
        
        // Continuously monitor for new weapons that might be spawned
        if (enableInfiniteAmmo)
        {
            StartCoroutine(MonitorForNewWeapons());
        }
    }
    
    /// <summary>
    /// Monitor for new weapons that might be spawned during gameplay
    /// </summary>
    IEnumerator MonitorForNewWeapons()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f); // Check every 2 seconds
            
            Gun[] allGuns = FindObjectsOfType<Gun>();
            
            foreach (Gun gun in allGuns)
            {
                // Check if this is a new weapon we haven't processed yet
                if (!weaponsInScene.Contains(gun))
                {
                    bool shouldHaveInfiniteAmmo = enableInfiniteAmmo && (!onlyForPistols || gun.isPistol);
                    
                    if (shouldHaveInfiniteAmmo)
                    {
                        gun.SetInfiniteAmmo(true);
                        weaponsInScene.Add(gun);
                        DebugLog($"New weapon detected! Enabled infinite reserve ammo for: {gun.weaponName}");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Manually enable infinite ammo for all weapons
    /// </summary>
    [ContextMenu("Enable Infinite Ammo")]
    public void EnableInfiniteAmmo()
    {
        enableInfiniteAmmo = true;
        
        if (isEndlessMode)
        {
            StartCoroutine(SetupInfiniteAmmo());
        }
    }
    
    /// <summary>
    /// Manually disable infinite ammo for all weapons
    /// </summary>
    [ContextMenu("Disable Infinite Ammo")]
    public void DisableInfiniteAmmo()
    {
        enableInfiniteAmmo = false;
        
        foreach (Gun gun in weaponsInScene)
        {
            if (gun != null)
            {
                gun.SetInfiniteAmmo(false);
            }
        }
        
        weaponsInScene.Clear();
        DebugLog("Infinite reserve ammo disabled for all weapons");
    }
    
    /// <summary>
    /// Toggle infinite ammo on/off
    /// </summary>
    public void ToggleInfiniteAmmo()
    {
        if (enableInfiniteAmmo)
        {
            DisableInfiniteAmmo();
        }
        else
        {
            EnableInfiniteAmmo();
        }
    }
    
    /// <summary>
    /// Set whether only pistols should have infinite ammo
    /// </summary>
    public void SetPistolOnlyMode(bool pistolOnly)
    {
        onlyForPistols = pistolOnly;
        
        if (isEndlessMode && enableInfiniteAmmo)
        {
            // Refresh the infinite ammo settings
            DisableInfiniteAmmo();
            StartCoroutine(SetupInfiniteAmmo());
        }
    }
    
    /// <summary>
    /// Add a new endless scene name to the detection list
    /// </summary>
    public void AddEndlessSceneName(string sceneName)
    {
        System.Array.Resize(ref endlessSceneNames, endlessSceneNames.Length + 1);
        endlessSceneNames[endlessSceneNames.Length - 1] = sceneName;
        DebugLog($"Added new endless scene name: {sceneName}");
    }
    
    /// <summary>
    /// Get status information about the ammo manager
    /// </summary>
    public string GetStatus()
    {
        return $"Endless Mode: {isEndlessMode}\n" +
               $"Infinite Ammo: {enableInfiniteAmmo}\n" +
               $"Pistol Only: {onlyForPistols}\n" +
               $"Weapons Modified: {weaponsInScene.Count}\n" +
               $"Scene: {SceneManager.GetActiveScene().name}";
    }
    
    /// <summary>
    /// Debug logging with manager name
    /// </summary>
    void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[EndlessAmmoManager] {message}");
        }
    }
    
    /// <summary>
    /// Draw status information in the editor
    /// </summary>
    void OnGUI()
    {
        if (debugMode && Application.isEditor)
        {
            GUI.Box(new Rect(10, 10, 300, 120), GetStatus());
        }
    }
}
