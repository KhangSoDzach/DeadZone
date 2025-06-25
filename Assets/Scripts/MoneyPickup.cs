using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoneyPickup : MonoBehaviour
{
    [Header("Money Settings")]
    public int value = 10; // Giá trị của đồng tiền
    public float pickupRadius = 2f; // Bán kính nhặt tiền tự động
    public LayerMask playerLayer; // Layer của người chơi
    
    [Header("Effects")]
    public AudioSource audioSource; // Âm thanh khi nhặt
    public AudioClip pickupSound; // Âm thanh khi nhặt tiền
    public GameObject pickupEffect; // Hiệu ứng khi nhặt
    public float soundVolume = 1.0f; // Âm lượng tiếng nhặt tiền

    private bool isCollected = false;
    private Transform player;
    private Vector3 startPosition;
    public float floatSpeed = 1.0f; // Tốc độ di chuyển lên xuống

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
        
        // Lưu vị trí bắt đầu để di chuyển lên xuống
        startPosition = transform.position;
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
            if (audioSource == null)
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
                
                // Gán âm thanh nếu chưa có
                if (audioSource.clip == null && pickupSound != null)
                {
                    audioSource.clip = pickupSound;
                }
            }
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
        
        // Hiệu ứng xoay và di chuyển lên xuống (was missing implementation)
        transform.Rotate(Vector3.up, 30f * Time.deltaTime);
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * 0.1f;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
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
        
        // Phát âm thanh nhặt tiền (đảm bảo âm thanh được phát)
        PlayPickupSound();
        
        // Hiệu ứng
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
        
        // Quan trọng: Luôn dùng coroutine để đảm bảo âm thanh có thời gian phát
        StartCoroutine(DestroyAfterSound());
    }
    
    private void PlayPickupSound()
    {
        // Kiểm tra clip âm thanh
        AudioClip soundToPlay = null;
        if (audioSource != null && audioSource.clip != null)
        {
            soundToPlay = audioSource.clip;
        }
        else if (pickupSound != null)
        {
            soundToPlay = pickupSound;
        }
        
        if (soundToPlay != null)
        {
            // Create a temporary game object to play the sound that will survive after this object is destroyed
            GameObject soundObject = new GameObject("Money Pickup Sound");
            soundObject.transform.position = transform.position;
            
            // Add audio source to the temporary object
            AudioSource tempAudioSource = soundObject.AddComponent<AudioSource>();
            tempAudioSource.clip = soundToPlay;
            tempAudioSource.volume = soundVolume;
            tempAudioSource.spatialBlend = 0.5f; // Mix of 2D and 3D for better audibility
            
            // Try to connect to SFX mixer group if available
            tempAudioSource.outputAudioMixerGroup = FindSFXMixerGroup();
            
            // Play the sound
            tempAudioSource.Play();
            
            // Destroy the sound object after the clip has finished playing
            Destroy(soundObject, tempAudioSource.clip.length + 0.5f);
            
            Debug.Log($"Created temporary sound object to play money pickup sound at volume {soundVolume}");
        }
        else
        {
            Debug.LogWarning("Money pickup has no audio clip assigned! Add an AudioClip to pickupSound field.");
        }
    }

    // Helper method to find the SFX mixer group in the scene
    private UnityEngine.Audio.AudioMixerGroup FindSFXMixerGroup()
    {
        // Try to find AudioMixer in the scene
        UnityEngine.Audio.AudioMixer audioMixer = Resources.FindObjectsOfTypeAll<UnityEngine.Audio.AudioMixer>()
            .FirstOrDefault(m => m.name == "AudioMixer");
        
        if (audioMixer != null)
        {
            // Find the SFX group
            UnityEngine.Audio.AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("SFX");
            if (groups.Length > 0)
            {
                Debug.Log("Found SFX mixer group for money pickup sound");
                return groups[0];
            }
        }
        
        Debug.Log("Could not find SFX mixer group, using default");
        return null;
    }
    
    private IEnumerator DestroyAfterSound()
    {
        // Hide visual components
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
        
        // Disable collider to prevent multiple pickups
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Play sound effect
        PlayPickupSound();
        
        // Destroy this object after a short delay
        Debug.Log($"Money pickup object will be destroyed after 0.2 seconds");
        yield return new WaitForSeconds(0.2f);
        
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
        
        // Cảnh báo khi không có âm thanh
        if (pickupSound == null && (audioSource == null || audioSource.clip == null))
        {
            Debug.LogWarning("MoneyPickup: Không có âm thanh được gán! Hãy gán âm thanh vào pickupSound để phát khi nhặt tiền.");
        }
    }
}
