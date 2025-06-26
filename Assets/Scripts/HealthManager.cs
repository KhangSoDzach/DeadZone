using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    public float immunityTime = 1.0f;
    private float immunityTimer = 0f;
    private bool isImmune = false;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 10f;
    public float staminaDepletionRate = 20f;
    public float staminaRegenDelay = 1.5f;
    private float staminaRegenTimer = 0f;
    private bool isRegeneratingStamina = true;

    [Header("UI Elements")]
    public Image healthBarImage;
    public Text healthText;
    public Image staminaBarImage;
    public Text staminaText;

    [Header("Health Bar Colors")]
    [Tooltip("Color when health bar is full (100%)")]
    public Color healthColor100 = new Color(0.0f, 1.0f, 0.0f);
    [Tooltip("Color when health bar is at 90%")]
    public Color healthColor90 = new Color(0.2f, 1.0f, 0.0f);
    [Tooltip("Color when health bar is at 80%")]
    public Color healthColor80 = new Color(0.4f, 1.0f, 0.0f);
    [Tooltip("Color when health bar is at 70%")]
    public Color healthColor70 = new Color(0.6f, 1.0f, 0.0f);
    [Tooltip("Color when health bar is at 60%")]
    public Color healthColor60 = new Color(0.8f, 1.0f, 0.0f);
    [Tooltip("Color when health bar is at 50%")]
    public Color healthColor50 = new Color(1.0f, 1.0f, 0.0f);
    [Tooltip("Color when health bar is at 40%")]
    public Color healthColor40 = new Color(1.0f, 0.8f, 0.0f);
    [Tooltip("Color when health bar is at 30%")]
    public Color healthColor30 = new Color(1.0f, 0.6f, 0.0f);
    [Tooltip("Color when health bar is at 20%")]
    public Color healthColor20 = new Color(1.0f, 0.4f, 0.0f);
    [Tooltip("Color when health bar is at 10% or less")]
    public Color healthColor10 = new Color(1.0f, 0.0f, 0.0f);

    [Header("Stamina Bar Colors")]
    [Tooltip("Color when stamina bar is full (100%)")]
    public Color staminaColor100 = new Color(0.0f, 0.8f, 1.0f);
    [Tooltip("Color when stamina bar is at 50%")]
    public Color staminaColor50 = new Color(0.5f, 0.5f, 1.0f);
    [Tooltip("Color when stamina bar is at 10% or less")]
    public Color staminaColor10 = new Color(0.7f, 0.7f, 1.0f);

    [Header("Damage Effects")]
    [SerializeField] private AudioClip playerHurtSound;
    private AudioSource audioSource;
    private PlayerLook playerLook;

    void Awake()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("login"))
        {
            this.enabled = false;
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        playerLook = GetComponent<PlayerLook>();
        if (playerLook == null)
        {
            playerLook = GetComponentInChildren<PlayerLook>();
        }
        if (playerLook == null && Camera.main != null)
        {
            playerLook = Camera.main.GetComponent<PlayerLook>();
            if (playerLook == null)
            {
                playerLook = Camera.main.GetComponentInParent<PlayerLook>();
            }
        }
        ObjectiveManager.Instance.objectiveText.gameObject.SetActive(true);
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

    private void Start()
    {
        StartCoroutine(DelayedUIBind());
    }

    void Update()
    {
        if (isImmune)
        {
            immunityTimer -= Time.deltaTime;
            if (immunityTimer <= 0)
            {
                isImmune = false;
            }
        }
        ManageStaminaRegeneration();
    }

    public void TakeDamage(float damage)
    {
        if (isImmune)
            return;
        
        currentHealth -= damage;
        if (currentHealth < 0) 
            currentHealth = 0;
        if (playerLook != null)
        {
            playerLook.TakeDamageEffect();
        }
        UpdateHealthUI();
        if (currentHealth <= 0)
        {
            Die();
        }
        isImmune = true;
        immunityTimer = immunityTime;
        if (audioSource != null && playerHurtSound != null)
        {
            audioSource.clip = playerHurtSound;
            audioSource.Play();
        }
        if (playerLook != null)
        {
            playerLook.TakeDamageEffect();
            Debug.Log("Damage effect triggered on PlayerLook");
        }
        else
        {
            Debug.LogWarning("PlayerLook reference is missing! Cannot show damage effect.");
            playerLook = FindObjectOfType<PlayerLook>();
            if (playerLook != null)
            {
                playerLook.TakeDamageEffect();
                Debug.Log("Found PlayerLook and triggered damage effect");
            }
        }
        Debug.Log("Player took damage: " + damage + ", Health left: " + currentHealth);
    }
   
    void Die()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(ShowDeathScreenWithDelay());
        var playerMovement = GetComponent<PlayerMovementScript>();
        if (playerMovement == null)
        {
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
        var gunInventory = GetComponent<GunInventory>();
        if (gunInventory != null)
        {
            gunInventory.DeadMethod();
        }
        var playerInputs = GetComponents<MonoBehaviour>();
        foreach (var input in playerInputs)
        {
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
            Debug.LogWarning("DeathScreenManager does not exist in scene. Please add Death Screen Manager to the scene.");
        }
    }

    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina <= 0)
            return false;
        currentStamina -= amount * Time.deltaTime;
        if (currentStamina < 0)
            currentStamina = 0;
        staminaRegenTimer = 0;
        isRegeneratingStamina = false;
        UpdateStaminaUI();
        return true;
    }
    
    private void ManageStaminaRegeneration()
    {
        if (currentStamina >= maxStamina)
        {
            currentStamina = maxStamina;
            return;
        }
        if (!isRegeneratingStamina)
        {
            staminaRegenTimer += Time.deltaTime;
            if (staminaRegenTimer >= staminaRegenDelay)
            {
                isRegeneratingStamina = true;
            }
        }
        if (isRegeneratingStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
            UpdateStaminaUI();
        }
    }

    public void UpdateHealthUI()
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

    public void SetHealth(float health)
    {
        currentHealth = health;
        UpdateHealthUI();
        if (currentHealth <= maxHealth * 0.2f)
        {
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

    private void ShowLowHealthEffect()
    {
        Debug.Log("Low health effect triggered");
    }
}