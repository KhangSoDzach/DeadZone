using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    public PlayableDirector director;
    void Start()
    {
        if (director != null)
        {
            director.stopped += OnCutsceneEnd;
        }
    }

    void OnCutsceneEnd(PlayableDirector d)
    {
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
