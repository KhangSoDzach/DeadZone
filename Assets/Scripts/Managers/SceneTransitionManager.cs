using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SceneTransitionManager");
                _instance = go.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }    /// <summary>
    /// Clean up gameplay-specific objects before transitioning to menu
    /// </summary>
    public void CleanupGameplayObjects()
    {
        // Clean up gameplay UI and disable canvases from DontDestroyOnLoad
        var gameplayCanvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in gameplayCanvases)
        {
            // More comprehensive canvas name checking
            if (canvas.name.Contains("Game") || canvas.name.Contains("HUD") || canvas.name.Contains("Damage") || 
                canvas.name.Contains("Pickup") || canvas.name.Contains("Score") || canvas.name.Contains("Death") ||
                canvas.name.Contains("Pause") || canvas.name.Contains("UI") || canvas.name.Contains("Canvas"))
            {
                if (canvas.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    // Disable instead of destroy for DontDestroyOnLoad objects
                    canvas.gameObject.SetActive(false);
                    Debug.Log($"Disabled DontDestroyOnLoad canvas: {canvas.name}");
                }
                else
                {
                    // For scene-specific canvases, just disable them
                    canvas.gameObject.SetActive(false);
                    Debug.Log($"Disabled scene canvas: {canvas.name}");
                }
            }
        }

        // Also disable any UI widgets that might be from gameplay
        var uiWidgets = FindObjectsOfType<MonoBehaviour>().Where(mb => 
            mb.GetType().Namespace == "DevionGames.UIWidgets" && 
            mb.gameObject.scene.name == "DontDestroyOnLoad");
        
        foreach (var widget in uiWidgets)
        {
            widget.gameObject.SetActive(false);
            Debug.Log($"Disabled DontDestroyOnLoad UI widget: {widget.name}");
        }

        // Clean up and disable gameplay managers that shouldn't be active in menu
        DisableGameplayManagers();

        // Clean up player objects that shouldn't persist
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }
        }

        // Reset time scale
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Disable gameplay-specific managers when transitioning to menu
    /// </summary>
    private void DisableGameplayManagers()
    {
        // Disable PickupDisplayManager
        if (PickupDisplayManager.Instance != null)
        {
            PickupDisplayManager.Instance.gameObject.SetActive(false);
            Debug.Log("Disabled PickupDisplayManager");
        }

        // Disable ScoreManager if it exists
        if (ScoreManager.Instance != null)
        {
            var scoreCanvas = ScoreManager.Instance.GetComponentInChildren<Canvas>();
            if (scoreCanvas != null)
            {
                scoreCanvas.gameObject.SetActive(false);
            }
            Debug.Log("Disabled ScoreManager UI");
        }

        // Disable DeathScreenManager
        if (DeathScreenManager.Instance != null)
        {
            if (DeathScreenManager.Instance.deathScreenPanel != null)
            {
                DeathScreenManager.Instance.deathScreenPanel.SetActive(false);
            }
            Debug.Log("Disabled DeathScreenManager");
        }

        // Disable ShopWeaponBlocker
        if (ShopWeaponBlocker.Instance != null)
        {
            ShopWeaponBlocker.Instance.enabled = false;
            Debug.Log("Disabled ShopWeaponBlocker");
        }

        // Disable PlayerMovement for all DontDestroyOnLoad players
        var allPlayerMovements = FindObjectsOfType<PlayerMovement>();
        foreach (var pm in allPlayerMovements)
        {
            if (pm.gameObject.scene.name == "DontDestroyOnLoad")
            {
                pm.enabled = false;
                pm.gameObject.SetActive(false);
                Debug.Log("Disabled DontDestroyOnLoad PlayerMovement");
            }
        }
    }

    /// <summary>
    /// Re-enable gameplay managers when entering gameplay scenes
    /// </summary>
    public void EnableGameplayManagers()
    {
        // Re-enable PickupDisplayManager
        if (PickupDisplayManager.Instance != null)
        {
            PickupDisplayManager.Instance.gameObject.SetActive(true);
        }

        // Re-enable ScoreManager UI
        if (ScoreManager.Instance != null)
        {
            var scoreCanvas = ScoreManager.Instance.GetComponentInChildren<Canvas>();
            if (scoreCanvas != null)
            {
                scoreCanvas.gameObject.SetActive(true);
            }
        }

        // Re-enable ShopWeaponBlocker
        if (ShopWeaponBlocker.Instance != null)
        {
            ShopWeaponBlocker.Instance.enabled = true;
        }

        // Re-enable PlayerMovement for DontDestroyOnLoad players
        var allPlayerMovements = FindObjectsOfType<PlayerMovement>();
        foreach (var pm in allPlayerMovements)
        {
            if (pm.gameObject.scene.name == "DontDestroyOnLoad")
            {
                pm.gameObject.SetActive(true);
                pm.enabled = true;
            }
        }
    }    /// <summary>
    /// Load scene with cleanup
    /// </summary>
    public void LoadScene(string sceneName, bool cleanupFirst = true)
    {
        if (cleanupFirst)
        {
            CleanupGameplayObjects();
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Load menu scene with proper cleanup
    /// </summary>
    public void LoadMenuScene(string sceneName)
    {
        Debug.Log($"Loading menu scene: {sceneName}");
        CleanupGameplayObjects();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Load gameplay scene with proper setup
    /// </summary>
    public void LoadGameplayScene(string sceneName)
    {
        Debug.Log($"Loading gameplay scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
        
        // Enable gameplay managers after scene loads
        StartCoroutine(EnableGameplayManagersAfterLoad());
    }

    /// <summary>
    /// Check if a scene is a menu scene based on name
    /// </summary>
    public bool IsMenuScene(string sceneName)
    {
        string lowerSceneName = sceneName.ToLower();
        return lowerSceneName.Contains("menu") || 
               lowerSceneName.Contains("login") || 
               lowerSceneName.Contains("main") ||
               lowerSceneName == "scene_menu" ||
               lowerSceneName == "mainmenu";
    }

    /// <summary>
    /// Check if current scene is a menu scene
    /// </summary>
    public bool IsCurrentSceneMenu()
    {
        return IsMenuScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator EnableGameplayManagersAfterLoad()
    {
        yield return new WaitForEndOfFrame();
        EnableGameplayManagers();
    }
}
