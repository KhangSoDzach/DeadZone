using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    // Singleton instance
    public static ScoreManager Instance { get; private set; }
    
    [Header("UI References")]
    public Text scoreText;
    
    [Header("Settings")]
    public int initialScore = 0;
    public bool persistBetweenLevels = true;

    // Static property that any script can access
    private static int _score = 0;
    public static int Score {
        get { return _score; }
        set {
            _score = value;
            if (Instance != null) {
                Instance.UpdateScoreUI();
            }
        }
    }
    
    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            
            if (persistBetweenLevels)
                DontDestroyOnLoad(gameObject);
            
            _score = initialScore;
            FindScoreText();
            UpdateScoreUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // One more attempt to find scoreText if needed
        if (scoreText == null)
        {
            FindScoreText();
        }
        
        Debug.Log($"ScoreManager initialized with scoreText: {(scoreText != null ? scoreText.name : "null")}");
    }
    
    // Find score text in the scene
    public void FindScoreText()
    {
        if (scoreText == null)
        {
            // Find all Text components in the scene
            Text[] allTexts = FindObjectsOfType<Text>();
            
            foreach (Text text in allTexts)
            {
                if (text.name.ToLower().Contains("score"))
                {
                    scoreText = text;
                    Debug.Log("ScoreManager found scoreText: " + text.name);
                    break;
                }
            }
        }
    }
    
    // Updates the UI text to show current score
    public void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + _score.ToString();
        }
        else
        {
            Debug.LogWarning("Cannot update score UI - scoreText is null!");
            FindScoreText(); // Try to find it again
        }
    }
    
    // Static method to add points to the score
    public static void AddScore(int points)
    {
        // Ensure we have an instance
        if (Instance == null)
        {
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            scoreManagerObj.AddComponent<ScoreManager>();
        }
        
        Score += points;
        Debug.Log("Score increased by " + points + ". Total: " + Score);
    }
    
    // For external scripts to get the scoreText reference
    public static Text GetScoreText()
    {
        return (Instance != null) ? Instance.scoreText : null;
    }
}
