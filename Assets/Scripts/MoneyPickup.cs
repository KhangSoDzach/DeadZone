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
    public float soundVolume = 1.0f; // Âm lượng tiếng nhặt tiền

    private bool isCollected = false;
    private Transform player;
    private AudioSource audioSource;

    private void Start()
    {
        // Kiểm tra xem script có đang chạy trên Player không
        if (gameObject.CompareTag("Player"))
        {
            Debug.LogError("MoneyPickup script không nên được gắn trên Player! Đang tắt script...");
            this.enabled = false;
            return;
        }
        
        // Tìm Player, nhưng đảm bảo không phải chính object này
        FindPlayerReference();
        
        // Setup AudioSource an toàn
        SetupAudioSource();
        
        // Setup glow effect
        SetupGlowEffect();
        
        // Đảm bảo object này không phải là Player
        ValidateObjectSetup();
    }
    
    private void FindPlayerReference()
    {
        // Tìm tất cả objects có tag Player
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject playerObj in playerObjects)
        {
            // Đảm bảo không lấy chính object này làm player reference
            if (playerObj != this.gameObject)
            {
                player = playerObj.transform;
                Debug.Log($"MoneyPickup tìm thấy Player: {playerObj.name}");
                break;
            }
        }
        
        if (player == null)
        {
            Debug.LogWarning("MoneyPickup không tìm thấy Player trong scene!");
        }
    }
    
    private void SetupAudioSource()
    {
        // Chỉ thêm AudioSource nếu chưa có và không phải Player
        if (!gameObject.CompareTag("Player"))
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Cấu hình AudioSource
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 10f;
        }
    }
    
    private void SetupGlowEffect()
    {
        if (GetComponent<PickupGlow>() == null && !gameObject.CompareTag("Player"))
        {
            PickupGlow glow = gameObject.AddComponent<PickupGlow>();
            glow.glowColor = new Color(0.8f, 0.8f, 0.1f); // Gold/yellow for money
            glow.intensity = 1.3f;
            glow.usePulse = true;
            glow.flickerSpeed = 1f;
        }
    }
    
    private void ValidateObjectSetup()
    {
        // Đảm bảo object có layer phù hợp (không phải Player layer)
        if (gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.LogWarning("MoneyPickup đang ở Player layer, chuyển sang Default layer");
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
        
        // Đảm bảo có Collider để trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
            Debug.Log("Thêm SphereCollider cho MoneyPickup");
        }
        
        // Đảm bảo collider là trigger
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    private void Update()
    {
        // Không chạy Update nếu đang trên Player
        if (gameObject.CompareTag("Player"))
        {
            return;
        }
        
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
        // Đảm bảo không tự trigger với chính mình
        if (other.gameObject == this.gameObject) return;
        
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
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            ScoreManager scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
            scoreManager.FindScoreText();
        }
        
        // Tăng điểm
        ScoreManager.Instance.AddScore(value);
        
        // Phát âm thanh nhặt tiền
        PlayPickupSound();
        
        // Hiệu ứng
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
        
        // Hủy đối tượng (với delay nếu có âm thanh)
        if (pickupSound != null && audioSource != null)
        {
            StartCoroutine(DestroyAfterSound());
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void PlayPickupSound()
    {
        if (pickupSound != null)
        {
            if (audioSource != null && !gameObject.CompareTag("Player"))
            {
                audioSource.clip = pickupSound;
                audioSource.volume = soundVolume;
                audioSource.Play();
            }
            else
            {
                // Fallback: Phát âm thanh tại vị trí này
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
            }
        }
    }
    
    private IEnumerator DestroyAfterSound()
    {
        // Ẩn visual components ngay lập tức
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
        
        // Tắt collider để không thể nhặt lại
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Chờ âm thanh phát xong
        if (audioSource != null && audioSource.clip != null)
        {
            yield return new WaitForSeconds(audioSource.clip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        Destroy(gameObject);
    }
    
    // Method để kiểm tra xem script có được setup đúng không
    public void ValidateSetup()
    {
        if (gameObject.CompareTag("Player"))
        {
            Debug.LogError("CẢNH BÁO: MoneyPickup script đang được gắn trên Player object! Điều này có thể gây xung đột.");
            Debug.LogError("Hãy tạo một GameObject riêng cho money pickup thay vì gắn vào Player.");
        }
    }
    
    // Gọi trong OnValidate để kiểm tra trong Editor
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ValidateSetup();
        }
    }
}
