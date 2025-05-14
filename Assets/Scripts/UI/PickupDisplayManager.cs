using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PickupDisplayManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject displayPanel;
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    
    [Header("Display Settings")]
    public float displayDuration = 3f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Item Icon References")]
    public Sprite defaultWeaponIcon;
    public Sprite defaultMedkitIcon;
    
    // Singleton instance
    private static PickupDisplayManager _instance;
    public static PickupDisplayManager Instance { get { return _instance; } }
    
    private Coroutine displayCoroutine;
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Make sure panel is hidden at start
        if (displayPanel != null)
            displayPanel.SetActive(false);
    }
    
    // Display weapon pickup notification
    public void ShowWeaponPickup(string weaponName, int ammo, float damage, bool isAutomatic)
    {
        string description = $"Đạn: {ammo}  |  Sát thương: {damage}  |  {(isAutomatic ? "Tự động" : "Bán tự động")}";
        ShowDisplay(weaponName, description, defaultWeaponIcon);
    }
    
    // Display medkit pickup notification
    public void ShowMedkitPickup(float healAmount)
    {
        string description = $"Hồi phục: {healAmount}% máu";
        ShowDisplay("Medkit", description, defaultMedkitIcon);
    }
    
    // Generic display method
    private void ShowDisplay(string itemName, string description, Sprite icon)
    {
        // Stop any active display
        if (displayCoroutine != null)
            StopCoroutine(displayCoroutine);
            
        // Start new display
        displayCoroutine = StartCoroutine(DisplayRoutine(itemName, description, icon));
    }
    
    private IEnumerator DisplayRoutine(string itemName, string description, Sprite icon)
    {
        // Set up the display
        itemNameText.text = itemName;
        itemDescriptionText.text = description;
        itemIcon.sprite = icon;
        
        // Show panel
        displayPanel.SetActive(true);
        
        // Fade in
        float elapsed = 0f;
        float fadeTime = 0.5f;
        CanvasGroup canvasGroup = displayPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = displayPanel.AddComponent<CanvasGroup>();
            
        canvasGroup.alpha = 0f;
        
        while (elapsed < fadeTime)
        {
            canvasGroup.alpha = fadeCurve.Evaluate(elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            canvasGroup.alpha = 1 - fadeCurve.Evaluate(elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        // Hide panel
        displayPanel.SetActive(false);
        displayCoroutine = null;
    }
}
