using UnityEngine;

public class VaccinePickUpTrigger : MonoBehaviour
{
    public GameObject vaccineModel;
    public GameObject instructionUI;
    public GameObject bossObject;

    private bool isPlayerNear = false;
    private Transform player;

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

        if (bossObject != null)
            bossObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) TryFindPlayer();

        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (InventoryForKey.Instance != null)
                InventoryForKey.Instance.PickUpKey();

            if (vaccineModel != null)
                vaccineModel.SetActive(false);

            if (instructionUI != null)
                instructionUI.SetActive(false);

            if (bossObject != null)
                bossObject.SetActive(true);

            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.UpdateObjectiveFromSave("Get Out");

            gameObject.SetActive(false);
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

    private void TryFindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }
}
