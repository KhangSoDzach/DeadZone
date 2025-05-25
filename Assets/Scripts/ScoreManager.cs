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
    public Text stackMoneyText; // Thêm reference cho StackMoney Text
    
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
    
    // Direct access to the current score for PlayerDataManager
    public int currentScore
    {
        get { return _score; }
        private set { _score = value; }
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
        if (scoreText == null || stackMoneyText == null)
        {
            FindScoreText();
        }
        
        Debug.Log($"ScoreManager initialized with scoreText: {(scoreText != null ? scoreText.name : "null")}, stackMoneyText: {(stackMoneyText != null ? stackMoneyText.name : "null")}");
    }
    
    // Find score text in the scene
    public void FindScoreText()
    {
        // Find all Text components in the scene
        Text[] allTexts = FindObjectsOfType<Text>();
        
        // Tìm scoreText nếu chưa có
        if (scoreText == null)
        {
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
        
        // Tìm stackMoneyText nếu chưa có
        if (stackMoneyText == null)
        {
            foreach (Text text in allTexts)
            {
                if (text.name.ToLower().Contains("stackmoney"))
                {
                    stackMoneyText = text;
                    Debug.Log("ScoreManager found stackMoneyText: " + text.name);
                    break;
                }
            }
        }
    }
    
    // Updates the UI text to show current score
    public void UpdateScoreUI()
    {
        // Cập nhật scoreText
        if (scoreText != null)
        {
            scoreText.text = "Money: " + _score.ToString();
        }
        
        // Cập nhật stackMoneyText
        if (stackMoneyText != null)
        {
            stackMoneyText.text = _score.ToString();
        }
        
        // Nếu không tìm thấy một trong hai text element, thử tìm lại
        if (scoreText == null || stackMoneyText == null)
        {
            FindScoreText(); // Try to find it again
        }
    }
    
    // Method to set the score from saved data
    public void SetScore(int newScore)
    {
        _score = newScore;
        UpdateScoreUI();
    }
    
    // Method to add points/money
    public void AddScore(int points)
    {
        _score += points;
        UpdateScoreUI();
    }
    
    // Method to subtract points/money
    public void SubtractScore(int points)
    {
        _score = Mathf.Max(0, _score - points);
        UpdateScoreUI();
    }
    
    // For external scripts to get the scoreText reference
    public static Text GetScoreText()
    {
        return (Instance != null) ? Instance.scoreText : null;
    }
    
    // Add method to get stackMoneyText reference
    public static Text GetStackMoneyText()
    {
        return (Instance != null) ? Instance.stackMoneyText : null;
    }
}
