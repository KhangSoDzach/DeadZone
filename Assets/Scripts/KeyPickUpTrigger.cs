using UnityEngine;

public class KeyPickupTrigger : MonoBehaviour
{
    public GameObject keyModel;
    public GameObject instructionUI;

    private bool isPlayerNear = false;

    void Start()
    {
        if (instructionUI == null)
        {
            GameObject foundUI = GameObject.FindGameObjectWithTag("PressE");
            if (foundUI != null)
                instructionUI = foundUI;
        }

        if (instructionUI != null)
            instructionUI.SetActive(false);
    }

    void Update()
    {

        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (InventoryForKey.Instance != null)
                InventoryForKey.Instance.PickUpKey();

            if (keyModel != null)
                keyModel.SetActive(false);

            if (instructionUI != null)
                instructionUI.SetActive(false);

            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.UpdateObjective("Unlock a basement door in White Mansion");

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
}
