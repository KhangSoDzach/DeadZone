using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUI : MonoBehaviour
{
    public static BossHealthUI Instance;

    public Image healthFill;
    public TextMeshProUGUI bossNameText;
    public GameObject uiPanel;

    private void Awake()
    {
        Instance = this;
        HideUI();
    }

    public void ShowUI(string bossName)
    {
        uiPanel.SetActive(true);
        bossNameText.text = bossName;
    }

    public void UpdateHealth(float curHealth, float maxHealth)
    {
        float fillAmount = curHealth / maxHealth;
        healthFill.fillAmount = fillAmount;
    }

    public void HideUI()
    {
        uiPanel.SetActive(false);
    }
}