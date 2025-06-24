using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    public PlayableDirector director;

    public GameObject healthUI;
    public GameObject staminaUI;
    public GameObject scoreText;

    private GameObject objectiveUI;

    void Start()
    {
        healthUI = GameObject.Find("BorderHealth");
        staminaUI = GameObject.Find("StaminaPanel");
        scoreText = GameObject.Find("ScoreText");
        GameObject scoreManagerObj = new GameObject("ScoreManager");
        ScoreManager scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
        scoreManager.FindScoreText();
        scoreManager.SetScore(0);
        if (ObjectiveManager.Instance != null && ObjectiveManager.Instance.objectiveText != null)
        {
            objectiveUI = ObjectiveManager.Instance.objectiveText.gameObject;
            objectiveUI.SetActive(false);
        }

        if (healthUI != null) healthUI.SetActive(false);
        if (staminaUI != null) staminaUI.SetActive(false);
        if (scoreText != null) scoreText.SetActive(false);

        if (director != null)
            director.stopped += OnCutsceneEnd;
    }

    void OnCutsceneEnd(PlayableDirector d)
    {
        if (healthUI != null) healthUI.SetActive(true);
        if (staminaUI != null) staminaUI.SetActive(true);
        if (scoreText != null) scoreText.SetActive(true);

        if (objectiveUI != null)
            objectiveUI.SetActive(true);

        SceneManager.LoadScene("Scene_A");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            director.Stop();
        }
    }
}
