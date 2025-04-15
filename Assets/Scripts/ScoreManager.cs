using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance; // Singleton instance

    public Text scoreText; // Reference to the UI Text for displaying score
    private int score = 0; // Player's score

    private void Awake()
    {
        // Ensure there's only one instance of ScoreManager
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreUI(); // Initialize the score display
    }

    public void AddScore(int amount)
    {
        score += amount; // Increase the score
        UpdateScoreUI(); // Update the UI
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}"; // Update the score display
        }
    }
}