using UnityEngine;
using UnityEngine.SceneManagement;

public class LockedDoor : MonoBehaviour
{
    public GameObject instructionUI;
    private bool isPlayerNear = false;
    private bool hasUnlocked = false;
    public Transform playerTransform;

    void Start()
    {
        TryFindPlayer();

        if (instructionUI == null)
        {
            GameObject foundUI = GameObject.FindGameObjectWithTag("PressE");
            if (foundUI != null)
                instructionUI = foundUI;
        }

        if (instructionUI != null)
            instructionUI.SetActive(false);

        if (DataPersistenceManager.instance != null && DataPersistenceManager.instance.GetData().hasKey)
        {
            hasUnlocked = true;
        }
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
            //instructionUI?.SetActive(true);


            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("Bấm mở khóa cửa");

                //hasUnlocked = true;
                //instructionUI?.SetActive(false);

                if (DataPersistenceManager.instance != null)
                {
                    var data = DataPersistenceManager.instance.GetData();
                    if (data != null)
                    {
                        data.currentObjectiveText = "Find the Vaccine";
                        DataPersistenceManager.instance.SaveGame();
                    }
                }


                LoadScene();
                
            }
        }
        else if (!isPlayerNear && instructionUI != null)
        {
            instructionUI.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
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
