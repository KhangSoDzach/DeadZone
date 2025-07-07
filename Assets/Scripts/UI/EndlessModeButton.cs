using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Standalone component for Endless Mode button
/// Can be attached to any button to enable Endless Mode functionality
/// </summary>
public class EndlessModeButton : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private Button endlessButton;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private string endlessSceneName = "Endless";
    [SerializeField] private string buttonLabel = "Endless Mode";
    
    [Header("Loading UI (Optional)")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;
    
    [Header("Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float loadingDelay = 0.5f;

    private void Start()
    {
        InitializeButton();
    }

    private void InitializeButton()
    {
        // Auto-assign button if not set
        if (endlessButton == null)
        {
            endlessButton = GetComponent<Button>();
        }

        // Auto-assign text if not set
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TMP_Text>();
        }

        // Set button text
        if (buttonText != null && !string.IsNullOrEmpty(buttonLabel))
        {
            buttonText.text = buttonLabel;
        }

        // Setup button listener
        if (endlessButton != null)
        {
            endlessButton.onClick.AddListener(OnEndlessModeButtonClicked);
            DebugLog("Endless Mode button initialized successfully");
        }
        else
        {
            DebugLog("Error: No button component found!");
        }
    }

    /// <summary>
    /// Called when the Endless Mode button is clicked
    /// </summary>
    public void OnEndlessModeButtonClicked()
    {
        DebugLog("Endless Mode button clicked");
        StartEndlessMode();
    }

    /// <summary>
    /// Start loading Endless Mode scene
    /// </summary>
    public void StartEndlessMode()
    {
        DebugLog("Starting Endless Mode...");
        
        // Show loading if available
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null)
            {
                loadingText.text = "Loading Endless Mode...";
            }
        }

        // Start loading coroutine
        StartCoroutine(LoadEndlessSceneCoroutine());
    }

    /// <summary>
    /// Coroutine to load Endless scene with proper loading feedback
    /// </summary>
    private IEnumerator LoadEndlessSceneCoroutine()
    {
        // Wait for loading delay
        yield return new WaitForSeconds(loadingDelay);

        // Update loading text
        if (loadingText != null)
        {
            loadingText.text = "Entering endless battle...";
        }

        yield return new WaitForSeconds(0.3f);

        // Try to load the scene
        bool sceneLoaded = false;

        // Method 1: Try SceneTransitionManager if available
        var sceneTransitionManager = FindObjectOfType<SceneTransitionManager>();
        if (sceneTransitionManager != null)
        {
            DebugLog("Using SceneTransitionManager to load Endless scene");
            try
            {
                sceneTransitionManager.LoadGameplayScene(endlessSceneName);
                sceneLoaded = true;
            }
            catch (System.Exception e)
            {
                DebugLog("SceneTransitionManager failed: " + e.Message);
            }
        }

        // Method 2: Direct scene loading if SceneTransitionManager failed or not available
        if (!sceneLoaded)
        {
            if (Application.CanStreamedLevelBeLoaded(endlessSceneName))
            {
                DebugLog($"Loading Endless scene directly: {endlessSceneName}");
                SceneManager.LoadScene(endlessSceneName);
                sceneLoaded = true;
            }
            else
            {
                DebugLog($"Error: Scene '{endlessSceneName}' not found in build settings!");
                
                // Show error message
                if (loadingText != null)
                {
                    loadingText.text = $"Error: Scene '{endlessSceneName}' not found!";
                }
                
                // Hide loading panel after delay
                yield return new WaitForSeconds(2f);
                if (loadingPanel != null)
                {
                    loadingPanel.SetActive(false);
                }
            }
        }

        if (!sceneLoaded)
        {
            DebugLog("Failed to load Endless scene!");
        }
    }

    /// <summary>
    /// Debug logging with component name
    /// </summary>
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[EndlessModeButton] {message}");
        }
    }

    /// <summary>
    /// Public method to set the endless scene name from other scripts
    /// </summary>
    public void SetEndlessSceneName(string sceneName)
    {
        endlessSceneName = sceneName;
        DebugLog($"Endless scene name set to: {sceneName}");
    }

    /// <summary>
    /// Public method to set button label
    /// </summary>
    public void SetButtonLabel(string label)
    {
        buttonLabel = label;
        if (buttonText != null)
        {
            buttonText.text = label;
        }
        DebugLog($"Button label set to: {label}");
    }

    /// <summary>
    /// Enable or disable the button
    /// </summary>
    public void SetButtonEnabled(bool enabled)
    {
        if (endlessButton != null)
        {
            endlessButton.interactable = enabled;
            DebugLog($"Button enabled: {enabled}");
        }
    }

    /// <summary>
    /// Show or hide the button
    /// </summary>
    public void SetButtonVisible(bool visible)
    {
        if (endlessButton != null)
        {
            endlessButton.gameObject.SetActive(visible);
            DebugLog($"Button visible: {visible}");
        }
    }
}
