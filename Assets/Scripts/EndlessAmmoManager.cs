using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


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
    


    [ContextMenu("Enable Infinite Ammo")]
    public void EnableInfiniteAmmo()
    {
        enableInfiniteAmmo = true;
        
        if (isEndlessMode)
        {
            StartCoroutine(SetupInfiniteAmmo());
        }
    }
    

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
    

    public void AddEndlessSceneName(string sceneName)
    {
        System.Array.Resize(ref endlessSceneNames, endlessSceneNames.Length + 1);
        endlessSceneNames[endlessSceneNames.Length - 1] = sceneName;
        DebugLog($"Added new endless scene name: {sceneName}");
    }
    

    public string GetStatus()
    {
        return $"Endless Mode: {isEndlessMode}\n" +
               $"Infinite Ammo: {enableInfiniteAmmo}\n" +
               $"Pistol Only: {onlyForPistols}\n" +
               $"Weapons Modified: {weaponsInScene.Count}\n" +
               $"Scene: {SceneManager.GetActiveScene().name}";
    }
    
    void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[EndlessAmmoManager] {message}");
        }
    }
    
    void OnGUI()
    {
        if (debugMode && Application.isEditor)
        {
            GUI.Box(new Rect(10, 10, 300, 120), GetStatus());
        }
    }
}
