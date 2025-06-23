using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnDoorTrigger : MonoBehaviour
{
    public GameObject instructionUI;
    public string bossTag = "Boss";

    private bool isPlayerNear = false;
    private bool hasReturned = false;
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

        if (isPlayerNear && !hasReturned)
        {
            if (instructionUI != null)
                instructionUI.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (IsBossDead())
                {
                    hasReturned = true;
                    if (instructionUI != null)
                        instructionUI.SetActive(false);

                    LoadScene();

                    if (ObjectiveManager.Instance != null)
                        ObjectiveManager.Instance.UpdateObjective("Escape the island");
                }
                else
                {
                }
            }
        }
    }

    private bool IsBossDead()
    {
        GameObject boss = GameObject.FindGameObjectWithTag(bossTag);
        return boss == null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerTransform = other.transform;
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

    private void LoadScene()
    {
        SceneManager.LoadScene("FinalScene");
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
