using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class PickupGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    [Tooltip("Light color for the glow effect")]
    public Color glowColor = Color.white;
    
    [Range(0.1f, 10f)]
    [Tooltip("Intensity of the light")]
    public float intensity = 1.5f;
    
    [Range(0.5f, 10f)]
    [Tooltip("Range of the light")]
    public float range = 2f;
    
    [Tooltip("Should the light flicker?")]
    public bool flicker = true;
    
    [Range(0f, 1f)]
    [Tooltip("Amount of flickering (0 = none, 1 = maximum)")]
    public float flickerAmount = 0.1f;
    
    [Range(0.1f, 10f)]
    [Tooltip("Speed of the flickering effect")]
    public float flickerSpeed = 2f;
    
    [Tooltip("Use pulse effect instead of flicker")]
    public bool usePulse = false;
    
    private Light lightComponent;
    private float initialIntensity;
    private float timeOffset;
    
    void Awake()
    {
        // Set up the light component
        lightComponent = GetComponent<Light>();
        if (lightComponent == null)
        {
            lightComponent = gameObject.AddComponent<Light>();
        }
        
        // Configure light settings
        lightComponent.color = glowColor;
        lightComponent.intensity = intensity;
        lightComponent.range = range;
        lightComponent.shadows = LightShadows.None; // No shadows for performance
        lightComponent.type = LightType.Point;
        
        initialIntensity = intensity;
        timeOffset = Random.value * 10f; // Random offset for variety when multiple items are near each other
    }
    
    void Update()
    {
        if (!flicker && !usePulse) return;
        
        if (usePulse)
        {
            // Pulse effect (smoother)
            float pulse = Mathf.PingPong((Time.time + timeOffset) * flickerSpeed, 1f);
            lightComponent.intensity = initialIntensity * (1f - flickerAmount * 0.5f) + (pulse * flickerAmount * initialIntensity);
        }
        else if (flicker)
        {
            // Flickering effect (more random)
            float noise = Mathf.PerlinNoise(timeOffset, (Time.time + timeOffset) * flickerSpeed) * 2f - 1f;
            lightComponent.intensity = initialIntensity * (1f - flickerAmount * 0.5f) + (noise * flickerAmount * initialIntensity);
        }
    }
    
    // Method to change the light color at runtime
    public void SetColor(Color newColor)
    {
        glowColor = newColor;
        lightComponent.color = newColor;
    }
}
