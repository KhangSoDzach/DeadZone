using UnityEngine;
using UnityEngine.UI;

public class ShopTrigger : MonoBehaviour
{
    public GameObject shopUI; 
    public Transform player;
    public float interactDistance = 3f;

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
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleShop();
            }
        }
    }

    void ToggleShop()
    {
        shopOpen = !shopOpen;
        shopUI.SetActive(shopOpen);

        if (shopOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f; 
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
