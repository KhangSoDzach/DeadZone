using UnityEngine;

public class KeyPickupTrigger : MonoBehaviour
{
    public GameObject keyModel;            
    public GameObject instructionUI;   
    private bool isPlayerNear = false;

    void Start()
    {
        if (instructionUI != null)
            instructionUI.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            InventoryForKey.Instance.PickUpKey();
            if (keyModel != null)
                keyModel.SetActive(false);
            if (instructionUI != null)
                instructionUI.SetActive(false);

            ObjectiveManager.Instance.UpdateObjective("Unlock a basement door in White Mansion");
            gameObject.SetActive(false); 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && instructionUI != null)
        {
            instructionUI.SetActive(true);
            Debug.Log(instructionUI);
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
