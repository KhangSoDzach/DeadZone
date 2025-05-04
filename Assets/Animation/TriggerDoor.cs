using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    private Animator doorAni = null;
    private bool isOpen = false;
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Open");
            if (!isOpen)
            {
                doorAni.SetTrigger("Open");
                isOpen = true;
            }
            else
            {
                doorAni.SetTrigger("Close");
                isOpen = false;
            }
        }
    }
    void Start()
    {
        doorAni = GetComponent<Animator>();
        if (doorAni == null)
        {
            Debug.LogError("Không tìm th?y Animator trên GameObject c?a!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
