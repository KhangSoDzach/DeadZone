using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public float damage = 10f; 
    public float range = 100f; 
    // Start is called before the first frame update
    public Camera playerCamera;
    //public GameObject bulletPrefab; // Prefab for the bullet

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }
    void Shoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);
            // Here you can apply damage to the hit object if it has a health component
            // Example: hit.transform.GetComponent<Health>().TakeDamage(damage);
        }
    }
}
