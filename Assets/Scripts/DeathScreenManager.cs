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
    public string newGameSceneName = "Cutscene"; // Tên scene khi bắt đầu game mới

    private static DeathScreenManager _instance;
    public static DeathScreenManager Instance { get { return _instance; } }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ShowDeathScreen();
        }
    }

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
            // Dừng thời gian trong game khi hiển thị màn hình chết
            Time.timeScale = 0f;

            
            // Bật chuột để người chơi có thể tương tác với UI
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            deathScreenPanel.SetActive(true);

        }
    }

    // Phương thức để quay lại checkpoint gần nhất
    public void ReturnToLastCheckpoint()
    {
        deathScreenPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        GameObject oldPlayer = GameObject.FindGameObjectWithTag("Player");
        if (oldPlayer != null)
        {
            Destroy(oldPlayer);
        }

        if (DataPersistenceManager.instance != null)
        {
            Scene currentScene = SceneManager.GetActiveScene();

            DataPersistenceManager.instance.LoadGame(currentScene.name);
        }
        else
        {
            Debug.LogWarning("DataPersistenceManager không tồn tại.");
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

    }

    public void StartNewGame()
    {
        deathScreenPanel.SetActive(false);
        Time.timeScale = 1f;

        GameData data = DataPersistenceManager.instance?.GetData();
        float diff = data != null ? data.difficultyMode : 1f;

        bool isLoggedIn = GameAPI.Instance != null && GameAPI.Instance.PlayerData != null;

        if (isLoggedIn)
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


            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadGameplayScene(newGameSceneName);
            }
            else
            {
                SceneManager.LoadScene(newGameSceneName);
            }
        }
        else
        {

            if (DataPersistenceManager.instance != null)
            {
                DataPersistenceManager.instance.NewGame();
                DataPersistenceManager.instance.GetData().difficultyMode = diff;
            }

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

    public void ReturnToMainMenu()
    {
        deathScreenPanel.SetActive(false);

        Time.timeScale = 1f; 
        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;

        GameObject oldPlayer = GameObject.FindGameObjectWithTag("Player");
        if (oldPlayer != null)
        {
            Destroy(oldPlayer);
        }


        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMenuScene("Menu");
        }
        else
        {
            SceneManager.LoadScene("Menu");
        }
    }

}