using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Để sử dụng UI elements
using UnityEngine.Audio; // Để sử dụng AudioMixerGroup

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    private float xRotation = 0f;
    public float xSensitivity = 100f;
    public float ySensitivity = 100f;

    // Thêm các biến cho camera shake
    private Vector3 originalPosition;
    private float shakeIntensity = 0.2f;
    private float shakeDuration = 0.3f;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    // Thêm biến cho hiệu ứng viền đỏ
    public Image damageVignette;
    private float damageVignetteAlpha = 0f;
    private float damageVignetteSpeed = 2.5f;
    private GameObject damageCanvas;
    private Sprite damageSprite;
    
    // Thêm biến âm thanh khi bị thương
    [Header("Damage Audio")]
    public AudioSource audioSource;
    public AudioClip[] damageSounds; // Mảng các âm thanh khi bị thương
    public AudioMixerGroup audioMixerGroup; // Mixer group để điều chỉnh âm lượng
    [Range(0f, 1f)]
    public float damageVolume = 0.7f; // Âm lượng mặc định

    // Thêm biến cho hiệu ứng giật khi bắn
    private float currentRecoilX = 0f;      // Recoil on X axis (left-right)
    private float currentRecoilY = 0f;      // Recoil on Y axis (up-down)
    private float maxRecoilY = 5f;          // Maximum vertical recoil
    private float maxRecoilX = 2f;          // Maximum horizontal recoil
    private float recoilRecoverySpeed = 2f; // Recovery speed
    private float recoilBuildup = 0.5f;     // How fast recoil builds up during sustained fire
    private float horizontalRecoilFactor = 0.3f; // How much horizontal vs vertical recoil
    private bool isFiring = false;          // Track if player is actively firing

      public void TakeDamageEffect()
    {
        
        if (cam != null)
            originalPosition = cam.transform.localPosition;
        isShaking = true;
        shakeTimer = shakeDuration;
        shakeIntensity = 0.2f;

        // Kiểm tra nếu hiệu ứng viền đỏ chưa được tạo hoặc không tồn tại
        if (damageVignette == null || damageCanvas == null)
        {
            CreateDamageVignetteEffect();
        }

        // Kiểm tra lại sau khi tạo
        if (damageVignette != null && damageCanvas != null)
        {
            Debug.Log("DamageVignette tồn tại, đang hiển thị viền đỏ");
            damageVignetteAlpha = 0.8f; // Đặt alpha cho hiệu ứng viền đỏ
            // Đảm bảo UI được hiển thị
            damageCanvas.SetActive(true);
            // Đặt màu và độ trong suốt
            damageVignette.color = new Color(1, 0, 0, damageVignetteAlpha);
            Debug.Log("Đã đặt màu: " + damageVignette.color + ", Canvas active: " + damageCanvas.activeInHierarchy);
        }
        else
        {
            // Nếu không thể tạo damage effect (có thể do không ở gameplay scene)
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
            if (sceneName.Contains("menu") || sceneName.Contains("login"))
            {
                Debug.Log("PlayerLook: Bỏ qua tạo damage effect trong menu scene");
                return; // Don't create damage effects in menu scenes
            }
        }

        // Phát âm thanh khi bị thương
        PlayDamageSound();
    }
    
    // Phương thức phát âm thanh khi bị thương
    private void PlayDamageSound()
    {
        // Kiểm tra nếu damageSounds có giá trị và có ít nhất 1 âm thanh
        if (damageSounds != null && damageSounds.Length > 0)
        {
            // Nếu không có AudioSource, tạo mới
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.outputAudioMixerGroup = audioMixerGroup;
            }
            
            // Chọn ngẫu nhiên một âm thanh bị thương từ mảng
            AudioClip damageClip = damageSounds[Random.Range(0, damageSounds.Length)];
            
            // Phát âm thanh
            if (damageClip != null)
            {
                audioSource.volume = damageVolume;
                audioSource.PlayOneShot(damageClip);
                Debug.Log("Đã phát âm thanh bị thương: " + damageClip.name);
            }
        }
        else
        {
            Debug.LogWarning("Không có âm thanh bị thương nào được cấu hình!");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Check scene context first
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("login"))
        {
            Debug.Log("PlayerLook: Skipping initialization in menu scene");
            this.enabled = false;
            return;
        }

        cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("Không tìm thấy Camera! PlayerLook cần camera để hoạt động.");
            return;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        
        // Lưu vị trí ban đầu của camera
        originalPosition = cam.transform.localPosition;

        xSensitivity = 5f;
        ySensitivity = 5f;

        // Tạo hiệu ứng viền đỏ
        CreateDamageVignetteEffect();
        
        // Khởi tạo AudioSource nếu chưa được gán
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("Đã tạo AudioSource mới cho player");
            }
            // Gán mixer group nếu đã được cấu hình
            if (audioMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = audioMixerGroup;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cam == null) return;
        
        // Xử lý camera shake
        UpdateCameraShake();
        
        // Xử lý hiệu ứng viền đỏ
        UpdateDamageVignette();
        
        // Xử lý hồi phục sau giật
        UpdateRecoilRecovery();
    }

    // Xử lý di chuyển chuột
    public void ProcessLook(Vector2 input)
    {
        if (cam == null) return;
        
        float mouseX = input.x * xSensitivity * Time.deltaTime;
        float mouseY = input.y * ySensitivity * Time.deltaTime;

        // Thêm hiệu ứng giật vào góc quay theo cả trục X và Y
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation - currentRecoilY, -90f, 90f);

        // Áp dụng góc xoay dọc cho camera
        cam.transform.localRotation = Quaternion.Euler(xRotation, currentRecoilX, 0f);
        
        // Áp dụng góc xoay ngang cho người chơi, bao gồm cả recoil
        transform.Rotate(Vector3.up * mouseX);
    }

    // Phương thức công khai để Gun script gọi khi bắn
    public void ApplyRecoil(float recoilAmount)
    {
        // Đánh dấu đang bắn
        isFiring = true;
        
        // Thêm giật lên với độ ngẫu nhiên
        float verticalRecoil = recoilAmount * (1.0f + Random.Range(-0.1f, 0.1f));
        
        // Kiểm tra giới hạn recoil dọc
        if (currentRecoilY + verticalRecoil > maxRecoilY)
            verticalRecoil = maxRecoilY - currentRecoilY;
            
        currentRecoilY += verticalRecoil;
        
        // Thêm giật ngang với độ ngẫu nhiên cao hơn (để tạo cảm giác khó kiểm soát)
        float horizontalRecoil = recoilAmount * horizontalRecoilFactor * Random.Range(-1.0f, 1.0f);
        
        // Giới hạn recoil ngang
        currentRecoilX = Mathf.Clamp(currentRecoilX + horizontalRecoil, -maxRecoilX, maxRecoilX);
          // Chỉ thêm camera shake khi bắn nếu không đang có shake mạnh hơn từ việc bị đánh
        if (!isShaking || shakeIntensity < 0.1f) // Chỉ áp dụng shake từ súng nếu không có shake hoặc shake hiện tại yếu
        {
            originalPosition = cam.transform.localPosition;
            isShaking = true;
            shakeTimer = 0.1f; // Thời gian shake ngắn hơn khi bắn
            shakeIntensity = 0.03f * recoilAmount; // Cường độ nhẹ hơn và tỉ lệ với recoil
        }
        
        // Gọi hàm để ngừng trạng thái bắn sau một khoảng thời gian
        CancelInvoke("StopFiring");
        Invoke("StopFiring", 0.2f);
    }
    public void SetSensitivity(float sensitivity)
    {
        xSensitivity = sensitivity;
        ySensitivity = sensitivity;
    }
        
    // Đánh dấu khi ngừng bắn để bắt đầu hồi phục nhanh hơn
    private void StopFiring()
    {
        isFiring = false;
    }
    
    // Cập nhật hồi phục sau giật
    private void UpdateRecoilRecovery()
    {
        if (isFiring)
        {
            // Khi đang bắn, recoil hồi phục chậm hơn hoặc tiếp tục tăng (tuỳ loại vũ khí)
            currentRecoilY = Mathf.Min(maxRecoilY, currentRecoilY + recoilBuildup * Time.deltaTime);
            
            // Recoil ngang thì lắc qua lắc lại một chút khi đang bắn
            currentRecoilX += Random.Range(-0.1f, 0.1f) * Time.deltaTime;
            currentRecoilX = Mathf.Clamp(currentRecoilX, -maxRecoilX, maxRecoilX);
        }
        else
        {
            // Khi không bắn, recoil hồi phục nhanh hơn
            float recoveryMultiplier = 1.0f;
            
            // Hồi phục nhanh hơn khi recoil cao
            if (currentRecoilY > maxRecoilY * 0.7f || Mathf.Abs(currentRecoilX) > maxRecoilX * 0.7f)
                recoveryMultiplier = 1.5f;
            
            // Hồi phục recoil dọc
            if (currentRecoilY > 0)
            {
                currentRecoilY = Mathf.Max(0, currentRecoilY - recoilRecoverySpeed * recoveryMultiplier * Time.deltaTime);
            }
            
            // Hồi phục recoil ngang - hồi phục nhanh hơn một chút so với recoil dọc
            if (Mathf.Abs(currentRecoilX) > 0.01f)
            {
                currentRecoilX = Mathf.Lerp(currentRecoilX, 0, recoilRecoverySpeed * 1.2f * Time.deltaTime);
                
                // Khi gần về 0, đặt thẳng về 0 luôn
                if (Mathf.Abs(currentRecoilX) < 0.01f)
                    currentRecoilX = 0;
            }
        }
    }

    // Đặt tốc độ hồi phục của giật (có thể được đặt bởi Gun script)
    public void SetRecoilRecoverySpeed(float speed)
    {
        recoilRecoverySpeed = speed;
    }
    
    // Thêm phương thức để thiết lập các tham số recoil khác
    public void SetRecoilParameters(float buildup, float horizontalFactor)
    {
        recoilBuildup = buildup;
        horizontalRecoilFactor = horizontalFactor;
    }    // Cập nhật camera shake
    private void UpdateCameraShake()
    {
        if (isShaking && cam != null)
        {
            shakeTimer -= Time.deltaTime;
            
            if (shakeTimer > 0)
            {
                // Tạo hiệu ứng shake ngẫu nhiên với cường độ hiện tại
                cam.transform.localPosition = new Vector3(
                    originalPosition.x + Random.Range(-1f, 1f) * shakeIntensity,
                    originalPosition.y + Random.Range(-1f, 1f) * shakeIntensity,
                    originalPosition.z
                );
            }
            else
            {
                // Kết thúc shake và đặt camera về vị trí ban đầu
                isShaking = false;
                cam.transform.localPosition = originalPosition;
            }
        }
    }

    // Cập nhật hiệu ứng viền đỏ
    private void UpdateDamageVignette()
    {
        if (damageVignette != null)
        {
            // Giảm dần độ trong suốt của viền đỏ theo thời gian
            if (damageVignetteAlpha > 0)
            {
                damageVignetteAlpha -= Time.deltaTime * damageVignetteSpeed;
                Color color = damageVignette.color;
                color.a = Mathf.Clamp01(damageVignetteAlpha);
                damageVignette.color = color;
                
                // Ẩn canvas khi hoàn toàn trong suốt
                if (damageVignetteAlpha <= 0 && damageCanvas != null)
                {
                    damageCanvas.SetActive(false);
                }
            }
        }
    }

    // Tạo hiệu ứng viền đỏ nếu chưa có
    private void CreateDamageVignetteEffect()
    {
        if (damageCanvas != null)
        {
            damageCanvas.SetActive(false);  
            return;
        }

        try
        {
            damageCanvas = new GameObject("DamageEffectCanvas");
            damageCanvas.transform.SetParent(null);
            Canvas canvasComponent = damageCanvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 500; // Đảm bảo nhỏ hơn DeathScreen

            // Thêm GraphicRaycaster để UI hoạt động
            damageCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // CanvasScaler để hiển thị đúng trên mọi độ phân giải
            CanvasScaler scaler = damageCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Tạo image cho viền đỏ
            GameObject vignetteObject = new GameObject("DamageVignette");
            vignetteObject.transform.SetParent(damageCanvas.transform, false);

            damageVignette = vignetteObject.AddComponent<Image>();

            RectTransform rectTransform = vignetteObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            Texture2D tex = CreateDamageTexture();
            damageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            damageVignette.sprite = damageSprite;
            damageVignette.color = new Color(1, 0, 0, 0);
            damageVignette.type = Image.Type.Sliced;

            damageVignette.raycastTarget = false;

            damageCanvas.SetActive(false); 
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error creating damage vignette: " + e.Message);
        }
    }

    // Tạo một texture tốt hơn cho viền đỏ
    private Texture2D CreateDamageTexture()
    {
        int size = 256; // Giảm kích thước xuống để đỡ tốn bộ nhớ
        Texture2D tex = new Texture2D(size, size);
        
        // Điểm bắt đầu của viền (0: ở rìa, 1: ở giữa)
        float borderStart = 0.6f; // Đã điều chỉnh để viền rõ ràng hơn
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Tính khoảng cách từ tâm (0-1)
                float centerX = size * 0.5f;
                float centerY = size * 0.5f;
                float distance = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2)) / (size * 0.5f);
                
                // Alpha tăng dần từ giữa ra mép
                float alpha = 0;
                if (distance > borderStart)
                {
                    // Ánh xạ lại khoảng cách để tạo gradient viền mạnh hơn
                    alpha = Mathf.Clamp01((distance - borderStart) / (1 - borderStart));
                    alpha = Mathf.Pow(alpha, 0.8f); // Giảm xuống để làm viền rõ ràng hơn
                }
                
                tex.SetPixel(x, y, new Color(1, 0, 0, alpha));
            }
        }
        
        tex.Apply();
        return tex;
    }
}
