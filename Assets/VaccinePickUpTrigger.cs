using UnityEngine;

public class VaccinePickUpTrigger : MonoBehaviour
{
    public GameObject vaccineModel;
    public GameObject instructionUI;
    public GameObject bossObject; 
    private bool isPlayerNear = false;

    void Start()
    {
        if (instructionUI != null)
            instructionUI.SetActive(false);

        if (bossObject != null)
        {
            bossObject.SetActive(false);
        }

    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            InventoryForKey.Instance.PickUpKey();

            if (vaccineModel != null)
                vaccineModel.SetActive(false);

            if (instructionUI != null)
                instructionUI.SetActive(false);

            bossObject.SetActive(true);

            ObjectiveManager.Instance.UpdateObjective("Get Out");

            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && instructionUI != null)
        {
            instructionUI.SetActive(true);
        }
        isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && instructionUI != null)
        {
            instructionUI.SetActive(false);
        }
        isPlayerNear = false;
    }
}
