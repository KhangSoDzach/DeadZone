using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevionGames;

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
        
        playerData.checkpoint = new DevionGames.Checkpoint
        {
            sceneId = currentSceneId,
            position = new DevionGames.Position
            {
                x = position.x,
                y = position.y,
                z = position.z
            }
        };
    }
    
    // Extension method to get a weapon by name from the player data
    public static DevionGames.Weapon GetWeaponByName(this PlayerDataModel playerData, string weaponName)
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
    public static void UnlockWeapon(this PlayerDataModel playerData, string weaponId, string weaponName, float damage = 10f)
    {
        if (playerData.weapons == null)
        {
            playerData.weapons = new List<DevionGames.Weapon>();
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
        DevionGames.Weapon newWeapon = new DevionGames.Weapon
        {
            id = weaponId,
            name = weaponName,
            damage = (int)damage,
            level = 1,
            isUnlocked = true,
            ammo = 30
        };
        
        playerData.weapons.Add(newWeapon);
    }
}
