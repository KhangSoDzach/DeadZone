using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnDoorTrigger : MonoBehaviour
{
    public GameObject instructionUI;
    public string bossTag = "Boss";                

    private bool isPlayerNear = false;
    private bool hasReturned = false;

    void Start()
    {
        if (instructionUI != null)
            instructionUI.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNear && !hasReturned)
        {
            if (instructionUI != null)
                instructionUI.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (IsBossDead())
                {
                    hasReturned = true;
                    instructionUI.SetActive(false);
                    LoadScene();
                    ObjectiveManager.Instance.UpdateObjective("Escape the island");
                }
                else
                {
                    Debug.Log("Boss is still alive. Defeat the boss to leave.");
                }
            }
        }
    }

    bool IsBossDead()
    {
        GameObject boss = GameObject.FindGameObjectWithTag(bossTag);
        return boss == null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = true;
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
        SceneManager.LoadScene("FinalScene");
    }

}
