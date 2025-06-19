using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathScreenManager : MonoBehaviour
{
    [Header("Death Screen UI")]
    public GameObject deathScreenPanel; // Panel chứa UI màn hình chết
    public Button returnToCheckpointButton;
    public Button newGameButton;
    public Button returnToMainMenuButton;

    [Header("Scene Names")]
    public string mainMenuSceneName = "Menu"; // Tên scene menu chính
    public string newGameSceneName = "Scene_A"; // Tên scene khi bắt đầu game mới

    private static DeathScreenManager _instance;
    public static DeathScreenManager Instance { get { return _instance; } }

    private void Awake()
    {
        // Đảm bảo chỉ có một instance của DeathScreenManager
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Ẩn panel màn hình chết khi bắt đầu
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Đăng ký các sự kiện cho các nút
        if (returnToCheckpointButton != null)
        {
            returnToCheckpointButton.onClick.AddListener(ReturnToLastCheckpoint);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(StartNewGame);
        }

        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    // Phương thức để hiển thị màn hình chết
    public void ShowDeathScreen()
    {
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(true);
            
            // Dừng thời gian trong game khi hiển thị màn hình chết
            Time.timeScale = 0f;
            
            // Bật chuột để người chơi có thể tương tác với UI
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // Phương thức để quay lại checkpoint gần nhất
    public void ReturnToLastCheckpoint()
    {
        // Reset thời gian và ẩn màn hình chết
        Time.timeScale = 1f;
        deathScreenPanel.SetActive(false);
        
        // Tìm và gọi phương thức hồi sinh người chơi
        // Nếu có checkpoint system, có thể load checkpoint ở đây
        
        // Ví dụ: Reload scene hiện tại
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // Phương thức bắt đầu game mới
    public void StartNewGame()
    {
        // Reset thời gian và ẩn màn hình chết
        Time.timeScale = 1f;
        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(false);

        // Xoá dữ liệu game cũ nếu có (PlayerPrefs và PlayerData)
        // Xoá PlayerPrefs nếu bạn lưu dữ liệu local
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Nếu có GameAPI và PlayerData, reset dữ liệu về mặc định
        if (GameAPI.Instance != null && GameAPI.Instance.PlayerData != null)
        {
            var playerData = GameAPI.Instance.PlayerData;
            playerData.level = 1;
            playerData.experience = 0;
            playerData.money = 0;
            playerData.health = 100f;
            playerData.kills = 0;
            playerData.hasKey = false;
            playerData.checkpoint = null;
            if (playerData.weapons != null)
                playerData.weapons.Clear();
        }

        // Sử dụng SceneTransitionManager để load lại scene gameplay
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGameplayScene(newGameSceneName);
        }
        else
        {
            // Nếu không có manager, fallback về SceneManager
            SceneManager.LoadScene(newGameSceneName);
        }
    }
    
    private System.Collections.IEnumerator ResetOnlinePlayerData()
    {
        // Reset player data for online users
        if (GameAPI.Instance.PlayerData != null)
        {
            var playerData = GameAPI.Instance.PlayerData;
            playerData.level = 1;
            playerData.experience = 0;
            playerData.money = 0;
            playerData.health = 100f;
            playerData.checkpoint = null;
            if (playerData.weapons != null)
            {
                playerData.weapons.Clear();
            }
            
            // Try to save the reset data
            yield return StartCoroutine(GameAPI.Instance.SavePlayerData((success, error) => {
                if (!success)
                {
                    Debug.LogWarning("Failed to save reset data: " + error);
                }
            }));
        }
        
        // Load the new game scene
        SceneManager.LoadScene(newGameSceneName);
    }

    // Phương thức quay lại menu chính
    public void ReturnToMainMenu()
    {
        // Reset thời gian và ẩn màn hình chết
        Time.timeScale = 1f;
        deathScreenPanel.SetActive(false);
        
        // Load scene menu chính
        SceneManager.LoadScene(mainMenuSceneName);
    }
}