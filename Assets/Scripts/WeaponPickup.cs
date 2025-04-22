using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public int weaponIndex;     // Index in the weaponPrefabs array
    public string weaponName;   // Name of the weapon
    public int remainingAmmo;   // Store the remaining ammo when dropped
    public bool isPistol;       // Is this a pistol (primary weapon that can't be dropped)
    
    void Start()
    {
        // Add rotation effect to make the weapon more noticeable
        StartCoroutine(RotateWeapon());
    }
    
    // Simple rotation effect for dropped weapons
    IEnumerator RotateWeapon()
    {
        while (true)
        {
            transform.Rotate(new Vector3(0, 1, 0), 50 * Time.deltaTime);
            yield return null;
        }
    }
}