using System.Collections;
using UnityEngine;
using TMPro;

public class NoteTrigger : MonoBehaviour
{
    public GameObject instructionUI;
    public GameObject noteUI;
    public string noteText;

    private bool isPlayerNear = false;

    void Start()
    {
        TryFindUI();

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
                if (instructionUI != null) instructionUI.SetActive(false);
                ShowNote();
            }
        }

        if (noteUI != null && noteUI.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseNote();
        }
    }

    void ShowNote()
    {
        if (noteUI == null) return;

        noteUI.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //TextMeshProUGUI textComponent = noteUI.GetComponentInChildren<TextMeshProUGUI>();
        //if (textComponent != null)
        //{
        //    textComponent.text = !string.IsNullOrWhiteSpace(noteText) ? noteText : "(No content)";
        //}
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

    private void TryFindUI()
    {
        if (instructionUI == null)
        {
            GameObject pressE = GameObject.FindGameObjectWithTag("PressE");
            if (pressE != null) instructionUI = pressE;
        }

        if (noteUI == null)
        {
            GameObject foundNote = GameObject.FindGameObjectWithTag("NoteUI");
            if (foundNote != null)
            {
                noteUI = foundNote;
            }
            else
            {
                noteUI = transform.GetComponentInChildren<Canvas>()?.gameObject;
            }
        }
    }
}
