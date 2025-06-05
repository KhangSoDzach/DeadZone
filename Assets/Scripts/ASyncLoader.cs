using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ASyncLoader : MonoBehaviour
{
    [Header("Loading Settings")]
    [SerializeField] private GameObject sceneToLoad; // UI panel for loading
    [SerializeField] private GameObject mainMenu; // Main menu panel
    [SerializeField] private Slider slider; // Progress slider
    
    public void LoadLevelBtn(string levelToLoad)
    {
        // Validate input
        if (string.IsNullOrEmpty(levelToLoad))
        {
            Debug.LogError("ASyncLoader: Level name is empty!");
            return;
        }

        // Check if scene exists
        if (!Application.CanStreamedLevelBeLoaded(levelToLoad))
        {
            Debug.LogError($"ASyncLoader: Scene '{levelToLoad}' not found in build settings!");
            return;
        }

        if (mainMenu != null)
            mainMenu.SetActive(false);
        
        if (sceneToLoad != null)
            sceneToLoad.SetActive(true);

        StartCoroutine(LoadLevelAsync(levelToLoad));
    }
    
    IEnumerator LoadLevelAsync(string levelToLoad)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(levelToLoad);
        
        if (operation == null)
        {
            Debug.LogError("Failed to start async scene loading!");
            yield break;
        }

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // Chuyển đổi progress về khoảng 0-1
            
            if (slider != null)
            {
                slider.value = progress; // Cập nhật giá trị của slider
            }
            
            yield return null; // Chờ frame tiếp theo
        }
    }
}
