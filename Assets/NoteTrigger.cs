using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteTrigger : MonoBehaviour
{
    public GameObject instructionUI; 
    public GameObject noteUI;        
    public string noteText;         

    private bool isPlayerNear = false;

    void Start()
    {
        if (instructionUI != null)
            instructionUI.SetActive(false);

        if (noteUI != null)
            noteUI.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (noteUI != null && noteUI.activeSelf)
            {
                CloseNote();
            }
            else
            {
                instructionUI.SetActive(false);
                ShowNote();
            }
        }

        if (noteUI.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseNote();
        }
    }

    void ShowNote()
    {
        if (noteUI != null)
        {
            noteUI.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            TMPro.TextMeshProUGUI textComponent = noteUI.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null && !string.IsNullOrWhiteSpace(noteText))
            {
                textComponent.text = noteText;
            }
        }
    }

    public void CloseNote()
    {
        if (noteUI != null)
        {
            noteUI.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (instructionUI != null)
                instructionUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (instructionUI != null)
                instructionUI.SetActive(false);
        }
    }
}
