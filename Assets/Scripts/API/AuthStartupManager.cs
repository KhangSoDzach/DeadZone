using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DevionGames;

public class AuthStartupManager : MonoBehaviour
{
    public string gameSceneName = "MainHub";
    public string loginSceneName = "Login";
    public float autoLoginCheckDelay = 0.5f;
    
    [Header("Optional Debug UI")]
    public TMP_Text statusText;
    
    private void Start()
    {
        // Check for auto-login after a slight delay to let everything initialize
        Invoke("CheckForAutoLogin", autoLoginCheckDelay);
    }
    
    private void CheckForAutoLogin()
    {
        if (GameAPI.Instance.IsLoggedIn)
        {
            ShowStatus("Found saved login session. Authenticating...");
            
            // Try to validate the saved token
            StartCoroutine(GameAPI.Instance.GetPlayerData((success, message) => 
            {
                if (success)
                {
                    ShowStatus("Auto-login successful. Loading game...");
                    LoadGameScene();
                }
                else
                {
                    ShowStatus("Session expired. Please log in again.");
                    LoadLoginScene();
                }
            }));
        }
        else
        {
            ShowStatus("No saved session found.");
            LoadLoginScene();
        }
    }
    
    private void LoadGameScene()
    {
        if (SceneManager.GetActiveScene().name != gameSceneName)
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
    
    private void LoadLoginScene()
    {
        if (SceneManager.GetActiveScene().name != loginSceneName)
        {
            SceneManager.LoadScene(loginSceneName);
        }
    }
    
    private void ShowStatus(string message)
    {
        Debug.Log("AuthStartupManager: " + message);
        
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
