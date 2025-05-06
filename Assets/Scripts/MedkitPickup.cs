using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MedkitPickup : MonoBehaviour
{
    [Header("Medkit Settings")]
    public float healPercent = 30f; // Phần trăm máu hồi khi sử dụng
    public float rotationSpeed = 50f; // Tốc độ xoay của medkit
    public float floatSpeed = 0.5f; // Tốc độ di chuyển lên xuống
    public float floatHeight = 0.2f; // Độ cao di chuyển lên xuống

    [Header("Effects")]
    public GameObject pickupEffect; // Hiệu ứng khi nhặt
    public AudioClip pickupSound; // Âm thanh khi nhặt

    private Vector3 startPosition; // Vị trí ban đầu để tính toán di chuyển lên xuống
    private bool canPickup = true; // Medkit có thể được nhặt hay không

    void Start()
    {
        startPosition = transform.position;

        // Đặt medkit vào layer Pickups giống như vũ khí
        gameObject.layer = LayerMask.NameToLayer("Pickups");
    }

    void Update()
    {
        // Hiệu ứng xoay
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Hiệu ứng di chuyển lên xuống
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // Sử dụng medkit
    public void Use(HealthManager playerHealth)
    {
        if (!canPickup) return;

        if (playerHealth != null)
        {
            // Tính toán lượng máu hồi dựa trên phần trăm của máu tối đa
            float healAmount = playerHealth.maxHealth * (healPercent / 100f);
            
            // Kiểm tra nếu người chơi đã có đủ máu
            if (playerHealth.currentHealth >= playerHealth.maxHealth)
            {
                Debug.Log("Máu đã đầy, không cần sử dụng medkit!");
                return;
            }

            // Hồi máu cho người chơi
            playerHealth.Heal(healAmount);
            Debug.Log($"Đã sử dụng medkit, hồi {healAmount} máu ({healPercent}%)");

            // Phát âm thanh nếu có
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Tạo hiệu ứng nếu có
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            // Hủy đối tượng sau khi sử dụng
            Destroy(gameObject);
        }
    }
}