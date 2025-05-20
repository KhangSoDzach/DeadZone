using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyPickup : MonoBehaviour
{
    [Header("Money Settings")]
    public int value = 10; // Giá trị của đồng tiền
    public float pickupRadius = 2f; // Bán kính nhặt tiền tự động
    public LayerMask playerLayer; // Layer của người chơi
    
    [Header("Effects")]
    public AudioClip pickupSound; // Âm thanh khi nhặt
    public GameObject pickupEffect; // Hiệu ứng khi nhặt

    private bool isCollected = false;
    private Transform player;

    private void Start()
    {
        // Tìm player trong scene
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Debug để kiểm tra layer
        Debug.Log($"Money Layer: {gameObject.layer}, Player Layer: {player?.gameObject.layer}");
        
        // Add glow effect if not already present
        if (GetComponent<PickupGlow>() == null)
        {
            PickupGlow glow = gameObject.AddComponent<PickupGlow>();
            glow.glowColor = new Color(0.8f, 0.8f, 0.1f); // Gold/yellow for money
            glow.intensity = 1.3f;
            glow.usePulse = true;
            glow.flickerSpeed = 1f;
        }
    }
    
    private void Update()
    {
        // Kiểm tra khoảng cách với player để nhặt tự động
        if (!isCollected && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= pickupRadius)
            {
                CollectMoney();
            }
        }
    }

    // Xử lý va chạm trực tiếp
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !isCollected)
        {
            Debug.Log($"Money triggered with: {other.gameObject.name}");
            CollectMoney();
        }
    }
    
    // Hàm nhặt tiền
    private void CollectMoney()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        // Kiểm tra ScoreManager tồn tại
        if (ScoreManager.Instance == null)
        {
            //Debug.LogError("ScoreManager not found in scene! Creating one.");
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            ScoreManager scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
            scoreManager.FindScoreText(); // Tìm text UI trong scene
        }
        
        // Tăng điểm
        ScoreManager.AddScore(value);
        //Debug.Log($"Added {value} money to score. New total: {ScoreManager.Score}");
        
        // Phát âm thanh
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Hiệu ứng
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
        
        // Hủy đối tượng
        Destroy(gameObject);
    }
}
