using UnityEngine;
using UnityEngine.SceneManagement;

public class EndlessSceneDetector : MonoBehaviour
{
    [Header("Scene Detection")]
    [SerializeField] private string endlessSceneName = "Endless";
    
    [Header("Components to Enable in Endless Mode")]
    [SerializeField] private GameObject endlessManager;
    [SerializeField] private GameObject endlessUI;
    
    private void Start()
    {
        CheckScene();
    }
    
    private void CheckScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool isEndlessMode = currentScene.Equals(endlessSceneName, System.StringComparison.OrdinalIgnoreCase);
        
        Debug.Log($"Current scene: {currentScene}, Is Endless Mode: {isEndlessMode}");
        
        // Enable/disable endless components based on scene
        if (endlessManager != null)
        {
            endlessManager.SetActive(isEndlessMode);
        }
        
        if (endlessUI != null)
        {
            endlessUI.SetActive(isEndlessMode);
        }
        
        // If not endless mode, ensure we don't interfere with normal gameplay
        if (!isEndlessMode)
        {
            Debug.Log("Not in Endless scene, disabling Endless Mode components");
        }
        else
        {
            Debug.Log("Endless Mode detected, enabling components");
        }
    }
}
