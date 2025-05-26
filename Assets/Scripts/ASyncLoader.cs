using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ASyncLoader : MonoBehaviour
{
    [Header("Loading Settings")]
    [SerializeField] private GameObject sceneToLoad; // Tên scene cần tải
    [SerializeField] private GameObject mainMenu; // Thời gian delay trước khi bắt đầu tải
    [Header("Loading Settings")]
    [SerializeField] private Slider slider; // Thời gian delay trước khi bắt đầu tải
    public void LoadLevelBtn(string levelToLoad)
    {
        mainMenu.SetActive(false);
        sceneToLoad.SetActive(true);

        StartCoroutine(LoadLevelAsync(levelToLoad));
    }
    
    IEnumerator LoadLevelAsync(string levelToLoad)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(levelToLoad);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // Chuyển đổi progress về khoảng 0-1
            slider.value = progress; // Cập nhật giá trị của slider
            yield return null; // Chờ frame tiếp theo
        }
    }
}
