using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This file contains extension methods to make API integration easier
public static class GameAPIExtensions
{
    // Extension method for PlayerDataModel to create a checkpoint at the current player position
    public static void CreateCheckpointAtCurrentPosition(this PlayerDataModel playerData, GameObject player)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
        }
        
        string currentSceneId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Vector3 position = player.transform.position;
        
        playerData.checkpoint = new CheckpointData
        {
            sceneId = currentSceneId,
            position = position,
            timestamp = System.DateTime.Now.ToBinary().ToString(),
            additionalData = ""
        };
    }
    
    // Extension method to get a weapon by name from the player data
    public static WeaponData GetWeaponByName(this PlayerDataModel playerData, string weaponName)
    {
        if (playerData.weapons == null) return null;
        
        foreach (var weapon in playerData.weapons)
        {
            if (weapon.name.Equals(weaponName, System.StringComparison.OrdinalIgnoreCase))
            {
                return weapon;
            }
        }
        
        return null;
    }
    
    // Extension method to unlock a weapon in player data
    public static void UnlockWeapon(this PlayerDataModel playerData, string weaponId, string weaponName, int damage = 10)
    {
        if (playerData.weapons == null)
        {
            playerData.weapons = new List<WeaponData>();
        }
        
        // Check if weapon already exists
        foreach (var weapon in playerData.weapons)
        {
            if (weapon.id.Equals(weaponId, System.StringComparison.OrdinalIgnoreCase))
            {
                weapon.isUnlocked = true;
                return;
            }
        }
        
        // Create new weapon
        WeaponData newWeapon = new WeaponData
        {
            id = weaponId,
            name = weaponName,
            damage = damage,
            level = 1,
            isUnlocked = true,
            ammo = 30
        };
        
        playerData.weapons.Add(newWeapon);
    }
    
    // Extension method to save current weapon states from the game
    public static void SyncWeaponsFromGame(this PlayerDataModel playerData)
    {
        // Find all guns in the scene and sync their data
        Gun[] allGuns = Object.FindObjectsOfType<Gun>();
        
        if (playerData.weapons == null)
        {
            playerData.weapons = new List<WeaponData>();
        }
        
        foreach (Gun gun in allGuns)
        {
            if (gun != null)
            {
                WeaponData weaponData = playerData.GetWeaponByName(gun.name);
                
                if (weaponData == null)
                {
                    // Create new weapon data
                    weaponData = new WeaponData
                    {
                        id = gun.name.Replace(" ", "_").ToLower(),
                        name = gun.name,
                        damage = (int)gun.damage,
                        level = 1,
                        isUnlocked = true,
                        ammo = gun.totalAmmo
                    };
                    playerData.weapons.Add(weaponData);
                }
                else
                {
                    // Update existing weapon data
                    weaponData.damage = (int)gun.damage;
                    weaponData.ammo = gun.totalAmmo;
                }
            }
        }
    }
    
    // Extension method to apply weapon data to guns in the scene
    public static void ApplyWeaponsToGame(this PlayerDataModel playerData)
    {
        if (playerData.weapons == null) return;
        
        Gun[] allGuns = Object.FindObjectsOfType<Gun>(true); // Include inactive guns
        
        foreach (WeaponData weaponData in playerData.weapons)
        {
            foreach (Gun gun in allGuns)
            {
                if (gun.name.Equals(weaponData.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    gun.damage = weaponData.damage;
                    gun.totalAmmo = weaponData.ammo;
                    gun.UpdateAmmoUI();
                    break;
                }
            }
        }
    }
}
