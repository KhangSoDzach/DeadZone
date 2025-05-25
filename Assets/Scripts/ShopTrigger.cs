using UnityEngine;
using UnityEngine.UI;

public class ShopTrigger : MonoBehaviour
{
    public GameObject shopUI; 
    public Transform player;
    public float interactDistance = 3f;
    public KeyCode interactKey = KeyCode.E; // Changed from KeyCode.F to KeyCode.E

    private bool playerInZone = false;
    private bool shopOpen = false;

    void Start()
    {
        if (shopUI != null)
            shopUI.SetActive(false);

        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (playerInZone && Vector3.Distance(transform.position, player.position) < interactDistance)
        {
            if (Input.GetKeyDown(interactKey)) // Using the variable instead of hardcoded KeyCode.F
            {
                ToggleShop();
            }
        }
    }    void ToggleShop()
    {
        shopOpen = !shopOpen;
        
        // Get reference to ShopManagement
        ShopManagement shopManager = FindObjectOfType<ShopManagement>();
        
        if (shopOpen)
        {
            // Use ShopManagement to open shop (which also sets isShopOpen to true)
            if (shopManager != null)
            {
                shopManager.OpenShop();
            }
            else
            {
                // Fallback if no ShopManagement is found
                shopUI.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            Time.timeScale = 0f;
        }
        else
        {
            // Use ShopManagement to close shop (which also sets isShopOpen to false)
            if (shopManager != null)
            {
                shopManager.CloseShop();
            }
            else
            {
                // Fallback if no ShopManagement is found
                shopUI.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            Time.timeScale = 1f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInZone = false;
    }
}
