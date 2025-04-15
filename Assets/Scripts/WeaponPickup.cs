using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public WeaponType weaponType = WeaponType.Large; // Loại vũ khí (mặc định là vũ khí lớn)
    public string weaponName; // Tên của vũ khí, dùng để tải prefab từ Resources
    public int currentAmmo; // Đạn còn lại trong vũ khí khi rơi xuống
    public int maxAmmo; // Số đạn tối đa của vũ khí
    public float damage; // Sát thương của vũ khí
    
    public float rotationSpeed = 50f; // Tốc độ xoay khi vũ khí nằm trên mặt đất
    public float bobSpeed = 1f; // Tốc độ nhấp nhô lên xuống
    public float bobHeight = 0.1f; // Độ cao nhấp nhô
    
    private Vector3 startPosition; // Vị trí ban đầu để tính toán hiệu ứng nhấp nhô
    private bool isPickable = false; // Vũ khí có thể nhặt sau khi rơi xuống và nằm yên
    
    void Start()
    {
        startPosition = transform.position;
        
        // Sau 2 giây, vũ khí có thể nhặt được
        StartCoroutine(EnablePickup());
        
        // Nếu không có Rigidbody, thêm vào
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        // Nếu không có Collider, thêm BoxCollider
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true; // Để phát hiện va chạm nhưng không cản vật lý
        }
    }
    
    IEnumerator EnablePickup()
    {
        yield return new WaitForSeconds(2f);
        isPickable = true;
    }
    
    void Update()
    {
        // Xoay vũ khí lên xuống để thu hút sự chú ý
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Hiệu ứng nhấp nhô lên xuống
        if (isPickable)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Khi người chơi đi vào vùng có vũ khí
        if (isPickable && other.CompareTag("Player"))
        {
            Debug.Log("Player entered weapon pickup zone");
            // Hiển thị thông báo "Press E to pick up"
            ShowPickupPrompt(true);
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        // Khi người chơi đứng gần và nhấn E
        if (isPickable && other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            // Tìm vũ khí hiện tại của người chơi
            Gun playerGun = FindPlayerGun(other.gameObject);
            if (playerGun != null)
            {
                // Nếu người chơi đang cầm vũ khí cùng loại, thì thả vũ khí đó xuống
                if ((weaponType == WeaponType.Large && playerGun.isLargeWeapon) ||
                    (weaponType == WeaponType.Small && !playerGun.isLargeWeapon))
                {
                    playerGun.DropWeapon();
                }
                
                // Nói cho SwitchWeapon biết để tạo vũ khí mới
                SwitchWeapon weaponHolder = other.GetComponentInChildren<SwitchWeapon>();
                if (weaponHolder != null)
                {
                    // Load the Gun prefab from Resources using weaponName
                    GameObject gunPrefab = Resources.Load<GameObject>(weaponName);
                    if (gunPrefab != null)
                    {
                        GameObject gunInstance = Instantiate(gunPrefab);
                        Gun gunComponent = gunInstance.GetComponent<Gun>();
                        if (gunComponent != null)
                        {
                            weaponHolder.PickupWeapon(gunComponent);
                        }
                        else
                        {
                            Debug.LogError("The prefab does not contain a Gun component.");
                            Destroy(gunInstance);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to load Gun prefab with name: " + weaponName);
                    }
                    Destroy(gameObject); // Xóa vũ khí khỏi mặt đất
                }
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Khi người chơi rời khỏi vùng có vũ khí
        if (other.CompareTag("Player"))
        {
            ShowPickupPrompt(false);
        }
    }
    
    // Tìm vũ khí hiện tại của người chơi
    Gun FindPlayerGun(GameObject player)
    {
        // Tìm kiếm trong các con của đối tượng SwitchWeapon
        SwitchWeapon weaponHolder = player.GetComponentInChildren<SwitchWeapon>();
        if (weaponHolder != null)
        {
            foreach (Transform child in weaponHolder.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    return child.GetComponent<Gun>();
                }
            }
        }
        return null;
    }
    
    // Hiển thị/ẩn thông báo nhặt vũ khí
    void ShowPickupPrompt(bool show)
    {
        // Có thể thêm UI thông báo "Press E to pickup" ở đây
        // Ví dụ: pickupPromptUI.SetActive(show);
        
        Debug.Log(show ? "Press E to pick up weapon" : "");
    }
}