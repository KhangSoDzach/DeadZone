using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class SubtitleManager : MonoBehaviour
{
    public TextMeshProUGUI subtitleText;
    public string[] subtitleList = new string[]
{
    "I beat the boss and got the vaccine.",
    "The lab was falling apart, so I ran out.",
    "I found a boat and left the mansion behind.",
    "The deadzone is far away now.",
    "Maybe this vaccine can save what's left of the world."
}; 
    private int currIndex = 0;

    void Start()
    {
        ShowSubtitle();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            currIndex++;
            if (currIndex < subtitleList.Length)
            {
                ShowSubtitle();
            }
            else
            {
                EndScene(); 
            }
        }
    }

    void ShowSubtitle()
    {
        subtitleText.text = subtitleList[currIndex];
    }

    void EndScene()
    {
        SceneManager.LoadScene("Menu");
    }
}
