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
        // Check if we're in a login scene - if so, don't auto-login
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene.ToLower().Contains("login") || currentScene.ToLower().Contains("menu"))
        {
            ShowStatus("In login scene - skipping auto-login check");
            return;
        }

        if (GameAPI.Instance.IsLoggedIn)
        {
            ShowStatus("Found saved login session. Authenticating...");
            
            // Try to validate the saved token
            StartCoroutine(GameAPI.Instance.GetPlayerData((success, message) => 
            {
                if (success)
                {
                    ShowStatus("Auto-login successful. Game ready.");
                    // Don't load scene - we're already in game
                }
                else
                {
                    ShowStatus("Session expired. Need to login again.");
                    LoadLoginScene();
                }
            }));
        }
        else
        {
            ShowStatus("No saved session found. Loading login scene.");
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
