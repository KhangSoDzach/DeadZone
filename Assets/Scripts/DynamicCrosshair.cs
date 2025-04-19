using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("Crosshair Elements")]
    public RectTransform topRect;
    public RectTransform bottomRect;
    public RectTransform leftRect;
    public RectTransform rightRect;
    public RectTransform centerDot;
    
    [Header("Crosshair Settings")]
    public float minSpread = 10f;         // Minimum distance from center (pixels)
    public float maxSpread = 100f;        // Maximum distance from center (pixels)
    public Color normalColor = Color.white;
    public Color targetColor = Color.red;
    public float transitionSpeed = 5f;    // Speed of crosshair animation
    
    private Gun currentGun;               // Reference to active gun
    private float targetSpread;           // Target spread to animate towards
    private bool isTargetingEnemy = false;
    
    void Start()
    {
        // Find the active gun in the scene
        UpdateCurrentGun();
        
        // Set initial spread
        targetSpread = minSpread;
        UpdateCrosshairSpread(minSpread);
    }
    
    void Update()
    {
        // Check if we need to update the gun reference
        UpdateCurrentGun();
        
        // Check if player is aiming at an enemy
        CheckTargeting();
        
        // If we have a gun reference, update spread based on gun's current spread
        if (currentGun != null)
        {
            float spread = Mathf.Lerp(minSpread, maxSpread, 
                (currentGun.currentSpread - currentGun.baseSpread) / 
                (currentGun.maxSpread - currentGun.baseSpread));
            
            targetSpread = spread;
        }
        
        // Animate crosshair to target spread
        float currentSpread = Vector2.Distance(topRect.anchoredPosition, Vector2.zero);
        float newSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * transitionSpeed);
        UpdateCrosshairSpread(newSpread);
        
        // Update crosshair color
        UpdateCrosshairColor();
    }
    
    void UpdateCurrentGun()
    {
        // Find active gun if we don't have one
        if (currentGun == null)
        {
            // Look for an active gun in the scene
            Gun[] guns = FindObjectsOfType<Gun>();
            foreach (Gun gun in guns)
            {
                if (gun.gameObject.activeInHierarchy)
                {
                    currentGun = gun;
                    break;
                }
            }
        }
    }
    
    void CheckTargeting()
    {
        if (currentGun != null && currentGun.playerCamera != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(currentGun.playerCamera.transform.position, 
                currentGun.playerCamera.transform.forward, out hit, 
                currentGun.range))
            {
                // Check if hitting an enemy (adapt this to match your enemy tag/component)
                if (hit.collider.CompareTag("Enemy") || hit.collider.GetComponent<Target>() != null)
                {
                    isTargetingEnemy = true;
                    return;
                }
            }
        }
        
        isTargetingEnemy = false;
    }
    
    void UpdateCrosshairSpread(float spread)
    {
        if (topRect) topRect.anchoredPosition = new Vector2(0, spread);
        if (bottomRect) bottomRect.anchoredPosition = new Vector2(0, -spread);
        if (leftRect) leftRect.anchoredPosition = new Vector2(-spread, 0);
        if (rightRect) rightRect.anchoredPosition = new Vector2(spread, 0);
    }
    
    void UpdateCrosshairColor()
    {
        Color targetColorNow = isTargetingEnemy ? targetColor : normalColor;
        
        // Apply color to all crosshair elements
        ApplyColorToElement(topRect, targetColorNow);
        ApplyColorToElement(bottomRect, targetColorNow);
        ApplyColorToElement(leftRect, targetColorNow);
        ApplyColorToElement(rightRect, targetColorNow);
        ApplyColorToElement(centerDot, targetColorNow);
    }
    
    void ApplyColorToElement(RectTransform element, Color targetColor)
    {
        if (element != null)
        {
            Image image = element.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.Lerp(image.color, targetColor, Time.deltaTime * transitionSpeed);
            }
        }
    }
}
