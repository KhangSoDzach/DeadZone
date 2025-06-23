using UnityEngine;
using UnityEngine.SceneManagement;

public class LockedDoor : MonoBehaviour
{
    public GameObject instructionUI;
    private bool isPlayerNear = false;
    private bool hasUnlocked = false;
    private Transform playerTransform; 

    void Start()
    {
        TryFindPlayer();
        if (instructionUI == null)
        {
            GameObject foundUI = GameObject.FindGameObjectWithTag("PressE");
            if (foundUI != null)
            {
                instructionUI = foundUI;
            }
        }

        if (instructionUI != null)
            instructionUI.SetActive(false);
    }

    void Update()
    {
        if (playerTransform == null)
        {
            TryFindPlayer(); 
            return;
        }

        if (isPlayerNear && !hasUnlocked)
        {
            if (instructionUI != null)
                instructionUI.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (InventoryForKey.Instance != null && InventoryForKey.Instance.hasKey)
                {
                    hasUnlocked = true;
                    instructionUI.SetActive(false);
                    LoadScene();
                    if (ObjectiveManager.Instance != null)
                        ObjectiveManager.Instance.UpdateObjective("Find the Vaccine");
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (instructionUI != null)
                instructionUI.SetActive(false);
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

    void LoadScene()
    {
        SceneManager.LoadScene("Lab_Scene");
    }

    private void TryFindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
}
