using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [Header("UI Components")]
    public Image staminaBarImage;
    public Text staminaText;
    
    [Header("References")]
    public HealthManager healthManager;
    
    [Header("Animation")]
    public float animationSpeed = 5f;
    private float targetFill;
    
    void Start()
    {
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
                healthManager.staminaBarImage = staminaBarImage;
                healthManager.staminaText = staminaText;
            }
            else
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
                if (!sceneName.Contains("menu") && !sceneName.Contains("login"))
                {
                    Debug.LogWarning("HealthManager not found in scene. Stamina bar will not work.");
                }
                this.enabled = false;
                return;
            }
        }
        
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
        if (healthManager != null && staminaBarImage != null)
        {
            targetFill = healthManager.currentStamina / healthManager.maxStamina;
            staminaBarImage.fillAmount = Mathf.Lerp(staminaBarImage.fillAmount, targetFill, Time.deltaTime * animationSpeed);
            UpdateColor();
            if (staminaText != null)
            {
                staminaText.text = Mathf.Round(healthManager.currentStamina).ToString() + " / " + healthManager.maxStamina.ToString();
            }
        }
    }
    
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