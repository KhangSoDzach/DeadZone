using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpController : MonoBehaviour
{
    public Gun gunScript; // Reference to the Gun script
    public Rigidbody rb; // Reference to the Rigidbody component
    public BoxCollider col; // Reference to the BoxCollider component
    public Transform player,gunContainer,fpsCam;
    public bool equipped; 
    public float pickUpRange = 2f; // Range to pick up the weapon
    public float dropForwardForce, dropUpwardForce; // Forces applied when dropping the weapon
    public static bool slotfull; // Static variable to check if the weapon is equipped

    // Update is called once per frame
    private void Update()
    {
        Vector3 distanceToPlayer = player.position - transform.position;
        if (!equipped && distanceToPlayer.magnitude <= pickUpRange && Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }
        else if (equipped && Input.GetKeyDown(KeyCode.G))
        {
            Drop();
        }
    }
    void Drop()
    {
        equipped = false; // Set equipped to false when dropping the weapon
        slotfull = false; // Set the static variable to true when the weapon is picked up

        rb.isKinematic = false; // Make the Rigidbody kinematic to stop physics interactions
        col.isTrigger = false; // Disable the collider to prevent further interactions

        gunScript.enabled = false; // Disable the Gun script to stop firing
    }
    void PickUp()
    {
        equipped = true;
        slotfull = true; // Set the static variable to true when the weapon is picked up

        rb.isKinematic = true; // Make the Rigidbody kinematic to stop physics interactions
        col.isTrigger = true; // Enable the collider to prevent further interactions

        gunScript.enabled = true; // Enable the Gun script to stop firing
    }
}
