using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Đảm bảo namespace UI được import

public class HealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    private PlayerLook playerLook;
    
    // Hiệu ứng khi bị tấn công
    public float immunityTime = 1.0f; // Thời gian miễn nhiễm sau mỗi lần bị tấn công
    private float immunityTimer = 0f;
    private bool isImmune = false;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 10f; // Tốc độ hồi thể lực/giây khi không chạy
    public float staminaDepletionRate = 20f; // Tốc độ tiêu hao thể lực/giây khi chạy
    public float staminaRegenDelay = 1.5f; // Thời gian chờ (giây) trước khi bắt đầu hồi thể lực
    private float staminaRegenTimer = 0f;
    private bool isRegeneratingStamina = true;

    [Header("UI Elements")]
    public Image healthBarImage; // Thanh máu
    public Text healthText;
    public Image staminaBarImage; // Thanh thể lực
    public Text staminaText;

    [Header("Health Bar Colors")]
    [Tooltip("Màu khi thanh máu đầy (100%)")]
    public Color healthColor100 = new Color(0.0f, 1.0f, 0.0f);       // Xanh lá đậm
    [Tooltip("Màu khi thanh máu còn 90%")]
    public Color healthColor90 = new Color(0.2f, 1.0f, 0.0f);        // Xanh lá nhạt hơn
    [Tooltip("Màu khi thanh máu còn 80%")]
    public Color healthColor80 = new Color(0.4f, 1.0f, 0.0f);        // Xanh lá-vàng
    [Tooltip("Màu khi thanh máu còn 70%")]
    public Color healthColor70 = new Color(0.6f, 1.0f, 0.0f);        // Xanh lá-vàng đậm
    [Tooltip("Màu khi thanh máu còn 60%")]
    public Color healthColor60 = new Color(0.8f, 1.0f, 0.0f);        // Vàng-xanh lá
    [Tooltip("Màu khi thanh máu còn 50%")]
    public Color healthColor50 = new Color(1.0f, 1.0f, 0.0f);        // Vàng
    [Tooltip("Màu khi thanh máu còn 40%")]
    public Color healthColor40 = new Color(1.0f, 0.8f, 0.0f);        // Vàng-cam
    [Tooltip("Màu khi thanh máu còn 30%")]
    public Color healthColor30 = new Color(1.0f, 0.6f, 0.0f);        // Cam
    [Tooltip("Màu khi thanh máu còn 20%")]
    public Color healthColor20 = new Color(1.0f, 0.4f, 0.0f);        // Cam-đỏ
    [Tooltip("Màu khi thanh máu còn 10% hoặc thấp hơn")]
    public Color healthColor10 = new Color(1.0f, 0.0f, 0.0f);        // Đỏ

    [Header("Stamina Bar Colors")]
    [Tooltip("Màu khi thanh thể lực đầy (100%)")]
    public Color staminaColor100 = new Color(0.0f, 0.8f, 1.0f);      // Xanh dương
    [Tooltip("Màu khi thanh thể lực còn 50%")]
    public Color staminaColor50 = new Color(0.5f, 0.5f, 1.0f);       // Xanh dương nhạt
    [Tooltip("Màu khi thanh thể lực còn 10% hoặc thấp hơn")]
    public Color staminaColor10 = new Color(0.7f, 0.7f, 1.0f);       // Xanh dương rất nhạt

    void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("login"))
        {
            this.enabled = false;
            return;
        }

        StartCoroutine(DelayedUIBind());
    }

    IEnumerator DelayedUIBind()
    {
        yield return null;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (healthBarImage == null)
            {
                Transform t = canvas.transform.Find("BorderHealth/HealthBar");
                if (t != null) healthBarImage = t.GetComponent<Image>();
            }


            if (staminaBarImage == null)
            {
                Transform t = canvas.transform.Find("StaminaPanel/StaminaBackground/StaminaFill");
                if (t != null) staminaBarImage = t.GetComponent<Image>();
            }


        }

        if (!LoadStatsFromGameData())
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
        }

        playerLook = GetComponentInChildren<PlayerLook>() ?? GetComponentInParent<PlayerLook>() ?? GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<PlayerLook>();

        UpdateHealthUI();
        UpdateStaminaUI();
    }



    // Update is called once per frame
    void Update()
    {
        // Xử lý thời gian miễn nhiễm sau khi bị tấn công
        if (isImmune)
        {
            immunityTimer -= Time.deltaTime;
            if (immunityTimer <= 0)
            {
                isImmune = false;
            }
        }
        
        // Xử lý hồi thể lực
        ManageStaminaRegeneration();
    }

    // Phương thức để người chơi nhận sát thương từ zombie
    public void TakeDamage(float damage)
    {
        // Nếu đang trong thời gian miễn nhiễm, không nhận thêm sát thương
        if (isImmune)
            return;
            
        currentHealth -= damage;
        
        // Giới hạn không cho health xuống dưới 0
        if (currentHealth < 0) 
            currentHealth = 0;
        // Thực hiện các hiệu ứng khi bị tấn công
        if (playerLook != null)
        {
            playerLook.TakeDamageEffect();
        }
        // Cập nhật UI
        UpdateHealthUI();
        
        // Kiểm tra nếu người chơi chết
        if (currentHealth <= 0)
        {
            Die();
        }



        // Bắt đầu thời gian miễn nhiễm
        isImmune = true;
        immunityTimer = immunityTime;
    }
   
    // Phương thức khi người chơi chết
    void Die()
    {
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tìm Death Screen Manager để hiển thị màn hình chết
        StartCoroutine(ShowDeathScreenWithDelay());


        // Vô hiệu hóa điều khiển người chơi
        var playerMovement = GetComponent<PlayerMovementScript>();
        if (playerMovement == null)
        {
            // Try alternative component names
            var altMovement = GetComponent<PlayerMovement>();
            if (altMovement != null)
            {
                altMovement.enabled = false;
            }
        }
        else
        {
            playerMovement.enabled = false;
        }
        
        var playerLookComponent = GetComponent<PlayerLook>();
        if (playerLookComponent != null)
        {
            playerLookComponent.enabled = false;
        }
        
        // Vô hiệu hóa weapon controller nếu có
        var gunInventory = GetComponent<GunInventory>();
        if (gunInventory != null)
        {
            gunInventory.DeadMethod();
        }
        
        // Vô hiệu hóa tất cả các input component để tránh xung đột
        var playerInputs = GetComponents<MonoBehaviour>();
        foreach (var input in playerInputs)
        {
            // Tránh vô hiệu hóa HealthManager chính nó
            if (input != this && input.GetType().Name.Contains("Input"))
            {
                input.enabled = false;
            }
        }
        if (ObjectiveManager.Instance != null && ObjectiveManager.Instance.objectiveText != null)
        {
            ObjectiveManager.Instance.objectiveText.gameObject.SetActive(false);
        }
    }
    private IEnumerator ShowDeathScreenWithDelay()
    {
        yield return new WaitForSecondsRealtime(1f); 
        DeathScreenManager deathManager = DeathScreenManager.Instance;

        if (deathManager != null)
        {
            deathManager.ShowDeathScreen();
        }
        else
        {
            Debug.LogWarning("DeathScreenManager không tồn tại trong scene. Vui lòng thêm Death Screen Manager vào scene.");
        }
    }


    // Phương thức hồi máu
    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        
        // Giới hạn không cho health vượt quá máu tối đa
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
            
        // Cập nhật UI
        UpdateHealthUI();
    }

    // Phương thức mới: Sử dụng thể lực (khi chạy)
    public bool UseStamina(float amount)
    {
        // Nếu không đủ thể lực, trả về false
        if (currentStamina <= 0)
            return false;
            
        currentStamina -= amount * Time.deltaTime;
        if (currentStamina < 0)
            currentStamina = 0;
            
        // Reset bộ đếm thời gian hồi thể lực
        staminaRegenTimer = 0;
        isRegeneratingStamina = false;
        
        // Cập nhật UI
        UpdateStaminaUI();
        
        return true;
    }
    
    // Phương thức mới: Hồi thể lực khi không chạy
    private void ManageStaminaRegeneration()
    {
        // Nếu thể lực đã đầy, không cần hồi
        if (currentStamina >= maxStamina)
        {
            currentStamina = maxStamina;
            return;
        }
        
        // Nếu đang trong thời gian chờ trước khi hồi
        if (!isRegeneratingStamina)
        {
            staminaRegenTimer += Time.deltaTime;
            // Sau khoảng thời gian chờ, bắt đầu hồi
            if (staminaRegenTimer >= staminaRegenDelay)
            {
                isRegeneratingStamina = true;
            }
        }
        
        // Tiến hành hồi thể lực
        if (isRegeneratingStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            
            // Giới hạn không cho thể lực vượt quá mức tối đa
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
                
            // Cập nhật UI
            UpdateStaminaUI();
        }
    }

    // Cập nhật giao diện hiển thị máu với 10 nấc thang màu
    void UpdateHealthUI()
    {
        float healthPercentage = currentHealth / maxHealth;
        
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = healthPercentage;
            
            if (healthPercentage > 0.9f)
            {
                healthBarImage.color = healthColor100;
            }
            else if (healthPercentage > 0.8f)
            {
                healthBarImage.color = healthColor90;
            }
            else if (healthPercentage > 0.7f)
            {
                healthBarImage.color = healthColor80;
            }
            else if (healthPercentage > 0.6f)
            {
                healthBarImage.color = healthColor70;
            }
            else if (healthPercentage > 0.5f)
            {
                healthBarImage.color = healthColor60;
            }
            else if (healthPercentage > 0.4f)
            {
                healthBarImage.color = healthColor50;
            }
            else if (healthPercentage > 0.3f)
            {
                healthBarImage.color = healthColor40;
            }
            else if (healthPercentage > 0.2f)
            {
                healthBarImage.color = healthColor30;
            }
            else if (healthPercentage > 0.1f)
            {
                healthBarImage.color = healthColor20;
            }
            else
            {
                healthBarImage.color = healthColor10;
            }
        }
        
        if (healthText != null)
        {
            healthText.text = Mathf.Round(currentHealth).ToString() + " / " + maxHealth.ToString();
        }
    }
    
    // Phương thức mới: Cập nhật giao diện hiển thị thể lực
    void UpdateStaminaUI()
    {
        float staminaPercentage = currentStamina / maxStamina;
        
        if (staminaBarImage != null)
        {
            staminaBarImage.fillAmount = staminaPercentage;
            
            if (staminaPercentage > 0.5f)
            {
                staminaBarImage.color = Color.Lerp(staminaColor50, staminaColor100, (staminaPercentage - 0.5f) * 2);
            }
            else if (staminaPercentage > 0.1f)
            {
                staminaBarImage.color = Color.Lerp(staminaColor10, staminaColor50, (staminaPercentage - 0.1f) * 2.5f);
            }
            else
            {
                staminaBarImage.color = staminaColor10;
            }
        }
        
        if (staminaText != null)
        {
            staminaText.text = Mathf.Round(currentStamina).ToString() + " / " + maxStamina.ToString();
        }
    }

    // Phương thức để thiết lập giá trị sức khỏe một cách rõ ràng (để tải từ dữ liệu đã lưu)
    public void SetHealth(float health)
    {
        currentHealth = health;
        UpdateHealthUI();
        
        // Nếu sức khỏe thấp hơn mức cảnh báo, hiển thị hiệu ứng trực quan
        if (currentHealth <= maxHealth * 0.2f)
        {
            // Hiển thị hiệu ứng sức khỏe thấp nếu đã được triển khai
            ShowLowHealthEffect();
        }
    }
   

    public bool LoadStatsFromGameData()
    {
        GameData data = DataPersistenceManager.instance.GetData();
        if (data != null)
        {
            currentHealth = data.playerHealth;
            UpdateHealthUI();
            return true;
        }
        return false;
    }



    // Phương thức để hiển thị hiệu ứng trực quan khi sức khỏe thấp
    private void ShowLowHealthEffect()
    {
        // Triển khai hiệu ứng trực quan khi sức khỏe thấp ở đây
        // Ví dụ: có thể làm cho các cạnh màn hình chuyển sang màu đỏ, hoặc thêm hiệu ứng nhấp nháy
        Debug.Log("Hiệu ứng sức khỏe thấp đã được kích hoạt");
    }
}