using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DevionGames;

public class GameDataSynchronizer : MonoBehaviour
{
    private static GameDataSynchronizer _instance;
    public static GameDataSynchronizer Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameDataSynchronizer");
                _instance = go.AddComponent<GameDataSynchronizer>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    // Events
    public delegate void PlayerDataUpdatedHandler(PlayerDataModel data);
    public event PlayerDataUpdatedHandler OnPlayerDataUpdated;
    
    // Status flags
    public bool IsDataLoaded { get; private set; }
    public PlayerDataModel CurrentPlayerData { get; private set; }
    private bool _isSaving = false;
    private float _lastSaveTime = 0f;
    public float saveCooldown = 5f;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (GameAPI.Instance.IsLoggedIn)
        {
            LoadPlayerData();
        }
    }
    
    public void LoadPlayerData()
    {
        StartCoroutine(LoadPlayerDataCoroutine());
    }
    
    private IEnumerator LoadPlayerDataCoroutine()
    {
        yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, message) => {
            if (success)
            {
                CurrentPlayerData = GameAPI.Instance.PlayerData;
                IsDataLoaded = true;
                OnPlayerDataUpdated?.Invoke(CurrentPlayerData);
                Debug.Log("Player data loaded successfully");
            }
            else
            {
                Debug.LogError("Failed to load player data: " + message);
                IsDataLoaded = false;
            }
        }));
    }
    
    public void SaveGameData(Action<bool, string> callback = null)
    {
        if (_isSaving || Time.time - _lastSaveTime < saveCooldown)
        {
            callback?.Invoke(false, "Save in progress or on cooldown");
            return;
        }
        
        if (!GameAPI.Instance.IsLoggedIn || !IsDataLoaded)
        {
            callback?.Invoke(false, "Not logged in or no data loaded");
            return;
        }
        
        _isSaving = true;
        StartCoroutine(SaveGameDataCoroutine(callback));
    }
      private IEnumerator SaveGameDataCoroutine(Action<bool, string> callback)
    {
        // Update data from game objects
        UpdateDataFromGame();
          // Update GameAPI's PlayerData with our current data
        GameAPI.Instance.UpdatePlayerDataModel(CurrentPlayerData);        // Save to server using GameAPI
        yield return StartCoroutine(GameAPI.Instance.SavePlayerData((success, message) => {
            _isSaving = false;
            _lastSaveTime = Time.time;
            
            if (success)
            {
                Debug.Log("Game data saved successfully");
            }
            else
            {
                Debug.LogError("Failed to save game data: " + message);
            }
            
            callback?.Invoke(success, message);
        }));
    }
    
    private void UpdateDataFromGame()
    {
        if (CurrentPlayerData == null) return;
        
        // Get health from HealthManager
        HealthManager healthManager = FindObjectOfType<HealthManager>();
        if (healthManager != null)
        {
            CurrentPlayerData.health = healthManager.currentHealth;
        }
        
        // Get money from ScoreManager
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null)
        {
            CurrentPlayerData.money = scoreManager.currentScore;
        }
        
        // Update player position for checkpoint
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {            CurrentPlayerData.checkpoint = new DevionGames.Checkpoint
            {
                sceneId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                position = new DevionGames.Position
                {
                    x = player.transform.position.x,
                    y = player.transform.position.y,
                    z = player.transform.position.z
                }
            };
        }
        
        // Update weapon data
        UpdateWeaponData();
    }
    
    private void UpdateWeaponData()
    {
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null && CurrentPlayerData != null)
        {
            // Update current weapon
            if (weaponManager.CurrentGun != null)
            {
                CurrentPlayerData.currentWeapon = weaponManager.CurrentGun.weaponName;
            }
            
            // Update ammunition
            if (CurrentPlayerData.ammunition != null)
            {
                CurrentPlayerData.ammunition.pistol = weaponManager.GetAmmoCount("pistol");
                CurrentPlayerData.ammunition.rifle = weaponManager.GetAmmoCount("rifle");
            }
            
            // Update weapons data
            if (CurrentPlayerData.weapons != null)
            {
                foreach (var weaponData in CurrentPlayerData.weapons)
                {
                    Gun gun = weaponManager.GetGunByName(weaponData.name);
                    if (gun != null)
                    {
                        weaponData.level = gun.currentLevel;
                        weaponData.damage = (int)gun.damage;
                        weaponData.ammo = gun.currentAmmo;
                    }
                }
            }
        }
    }
    
    public void ApplyDataToGame()
    {
        if (!IsDataLoaded || CurrentPlayerData == null) return;
        
        StartCoroutine(ApplyDataToGameCoroutine());
    }
    
    private IEnumerator ApplyDataToGameCoroutine()
    {
        // Wait for a frame to let all game objects initialize
        yield return null;
        
        // Apply health
        HealthManager healthManager = FindObjectOfType<HealthManager>();
        if (healthManager != null)
        {
            healthManager.SetHealth(CurrentPlayerData.health);
        }
        
        // Apply score/money
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.SetScore(CurrentPlayerData.money);
        }
        
        // Apply weapon data
        ApplyWeaponData();
    }
    
    private void ApplyWeaponData()
    {
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null && CurrentPlayerData != null)
        {
            // Set ammo counts
            if (CurrentPlayerData.ammunition != null)
            {
                weaponManager.SetAmmoCount("pistol", CurrentPlayerData.ammunition.pistol);
                weaponManager.SetAmmoCount("rifle", CurrentPlayerData.ammunition.rifle);
            }
            
            // Setup weapons
            if (CurrentPlayerData.weapons != null)
            {
                foreach (var weaponData in CurrentPlayerData.weapons)
                {
                    if (weaponData.isUnlocked)
                    {
                        weaponManager.UnlockWeapon(weaponData.name);
                        
                        Gun gun = weaponManager.GetGunByName(weaponData.name);
                        if (gun != null)
                        {
                            gun.SetLevel(weaponData.level);
                            gun.SetAmmo(weaponData.ammo);
                        }
                    }
                }
            }
            
            // Equip current weapon
            if (!string.IsNullOrEmpty(CurrentPlayerData.currentWeapon))
            {
                weaponManager.EquipWeapon(CurrentPlayerData.currentWeapon);
            }
        }
    }
}
