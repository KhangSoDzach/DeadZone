using UnityEngine;

/// <summary>
/// Script để quản lý player khi load scene mới, đảm bảo không có duplicate
/// Đặt script này vào một GameObject trong mỗi scene
/// </summary>
public class ScenePlayerManager : MonoBehaviour
{
    [Header("Player Setup")]
    public Transform playerSpawnPoint; // Điểm spawn cho player
    public bool resetPlayerPosition = true; // Có reset vị trí player không
      void Start()
    {
        // Check if this is a menu scene and cleanup gameplay objects
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (currentSceneName.Contains("menu") || currentSceneName.Contains("login"))
        {
            CleanupGameplayUI();
        }
        
        CleanupDuplicatePlayers();
        SetupPlayer();
    }
    
    /// <summary>
    /// Dọn dẹp các player trùng lặp
    /// </summary>
    private void CleanupDuplicatePlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        if (players.Length > 1)
        {
            Debug.LogWarning($"Found {players.Length} players in scene, cleaning up duplicates...");
            
            // Tìm player có DontDestroyOnLoad (player chính)
            GameObject mainPlayer = null;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].scene.name == "DontDestroyOnLoad")
                {
                    mainPlayer = players[i];
                    break;
                }
            }
            
            // Nếu không tìm thấy player chính, giữ lại player đầu tiên
            if (mainPlayer == null)
            {
                mainPlayer = players[0];
            }
            
            // Xóa tất cả player khác
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != mainPlayer)
                {
                    Debug.LogWarning($"Destroying duplicate player: {players[i].name}");
                    Destroy(players[i]);
                }
            }
        }
    }
    
    /// <summary>
    /// Thiết lập player trong scene
    /// </summary>
    private void SetupPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            // Reset vị trí nếu cần
            if (resetPlayerPosition && playerSpawnPoint != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.enabled = false;
                    player.transform.position = playerSpawnPoint.position;
                    player.transform.rotation = playerSpawnPoint.rotation;
                    controller.enabled = true;
                }
                else
                {
                    player.transform.position = playerSpawnPoint.position;
                    player.transform.rotation = playerSpawnPoint.rotation;
                }
                
                Debug.Log($"Player position reset to: {playerSpawnPoint.position}");
            }
            
            // Reset vận tốc để tránh bay đi
            ResetPlayerPhysics(player);
        }
        else
        {
            Debug.LogError("No player found in scene!");
        }
    }
    
    /// <summary>
    /// Reset physics của player để tránh bay đi
    /// </summary>
    private void ResetPlayerPhysics(GameObject player)
    {
        // Reset CharacterController
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            controller.enabled = true;
        }
        
        // Reset Rigidbody nếu có
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Reset PlayerMovement component
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
            movement.enabled = true;
        }
        
        Debug.Log("Player physics reset successfully");
    }
    
    /// <summary>
    /// Clean up gameplay UI elements that shouldn't be visible in menu
    /// </summary>
    private void CleanupGameplayUI()
    {
        // Find and disable all DontDestroyOnLoad Canvas objects that are gameplay-related
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            // Check if this canvas is in DontDestroyOnLoad scene and appears to be gameplay UI
            if (canvas.gameObject.scene.name == "DontDestroyOnLoad")
            {
                string canvasName = canvas.name.ToLower();
                // Disable UI canvases that are clearly gameplay-related
                if (canvasName.Contains("game") || canvasName.Contains("hud") || 
                    canvasName.Contains("damage") || canvasName.Contains("pickup") || 
                    canvasName.Contains("score") || canvasName.Contains("death") ||
                    canvasName.Contains("pause") || canvasName.Contains("ui") ||
                    canvasName.Contains("crosshair") || canvasName.Contains("weapon") ||
                    canvasName.Contains("health") || canvasName.Contains("ammo"))
                {
                    canvas.gameObject.SetActive(false);
                    Debug.Log($"Disabled gameplay canvas in menu: {canvas.name}");
                }
            }
        }

        // Also disable any gameplay managers that have UI components
        var gameplayManagers = new string[] 
        { 
            "PickupDisplayManager", "ScoreManager", "DeathScreenManager", 
            "WeaponUIManager", "HealthUIManager", "AmmoUIManager" 
        };

        foreach (string managerName in gameplayManagers)
        {
            GameObject manager = GameObject.Find(managerName);
            if (manager != null && manager.scene.name == "DontDestroyOnLoad")
            {
                manager.SetActive(false);
                Debug.Log($"Disabled gameplay manager in menu: {managerName}");
            }
        }
    }
}
