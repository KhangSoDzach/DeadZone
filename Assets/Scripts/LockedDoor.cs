using UnityEngine;
using UnityEngine.SceneManagement;

public class LockedDoor : MonoBehaviour
{

    public GameObject instructionUI;
    private bool isPlayerNear = false;
    private bool hasUnlocked = false;

    void Start()
    {
        if (instructionUI != null)
            instructionUI.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNear && !hasUnlocked)
        {
            if (instructionUI != null)
                instructionUI.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (InventoryForKey.Instance.hasKey)
                {
                    hasUnlocked = true;
                    instructionUI.SetActive(false);
                    LoadScene();
                    ObjectiveManager.Instance.UpdateObjective("Find the vaccine");
                   
                }
                else
                {

                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = true;
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
}
