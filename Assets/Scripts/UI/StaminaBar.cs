using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [Header("UI Components")]
    public Image staminaBarImage; // Hình ảnh thanh thể lực
    public Text staminaText; // Text hiển thị giá trị thể lực (nếu cần)
    
    [Header("References")]
    public HealthManager healthManager; // Tham chiếu đến HealthManager
    
    [Header("Animation")]
    public float animationSpeed = 5f; // Tốc độ animation khi thay đổi giá trị thanh
    private float targetFill; // Giá trị mục tiêu cần đạt được
    
    // Các màu sắc được kế thừa từ HealthManager
    
    void Start()
    {
        // Nếu chưa gán HealthManager, tự động tìm trong scene
        if (healthManager == null)
        {
            healthManager = FindObjectOfType<HealthManager>();
            
            if (healthManager == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    healthManager = playerObject.GetComponent<HealthManager>();
                    
                    if (healthManager == null)
                    {
                        healthManager = playerObject.GetComponentInChildren<HealthManager>();
                    }
                }
            }
            
            if (healthManager != null)
            {
                // Gán references cho HealthManager để nó cũng có thể cập nhật UI này
                healthManager.staminaBarImage = staminaBarImage;
                healthManager.staminaText = staminaText;
            }
            else
            {
                Debug.LogError("Không tìm thấy HealthManager trong scene. Thanh thể lực sẽ không hoạt động.");
            }
        }
        
        // Thiết lập giá trị ban đầu
        if (healthManager != null && staminaBarImage != null)
        {
            targetFill = healthManager.currentStamina / healthManager.maxStamina;
            staminaBarImage.fillAmount = targetFill;
            
            if (staminaText != null)
            {
                staminaText.text = Mathf.Round(healthManager.currentStamina).ToString() + " / " + healthManager.maxStamina.ToString();
            }
        }
    }
    
    void Update()
    {
        // Cập nhật giao diện thanh thể lực mỗi frame với hiệu ứng mượt mà
        if (healthManager != null && staminaBarImage != null)
        {
            targetFill = healthManager.currentStamina / healthManager.maxStamina;
            staminaBarImage.fillAmount = Mathf.Lerp(staminaBarImage.fillAmount, targetFill, Time.deltaTime * animationSpeed);
            
            // Cập nhật màu sắc
            UpdateColor();
            
            // Cập nhật text nếu có
            if (staminaText != null)
            {
                staminaText.text = Mathf.Round(healthManager.currentStamina).ToString() + " / " + healthManager.maxStamina.ToString();
            }
        }
    }
    
    // Cập nhật màu sắc dựa trên lượng thể lực còn lại
    private void UpdateColor()
    {
        if (healthManager != null && staminaBarImage != null)
        {
            float staminaPercentage = healthManager.currentStamina / healthManager.maxStamina;
            
            if (staminaPercentage > 0.5f)
            {
                staminaBarImage.color = Color.Lerp(healthManager.staminaColor50, healthManager.staminaColor100, (staminaPercentage - 0.5f) * 2);
            }
            else if (staminaPercentage > 0.1f)
            {
                staminaBarImage.color = Color.Lerp(healthManager.staminaColor10, healthManager.staminaColor50, (staminaPercentage - 0.1f) * 2.5f);
            }
            else
            {
                staminaBarImage.color = healthManager.staminaColor10;
            }
        }
    }
}