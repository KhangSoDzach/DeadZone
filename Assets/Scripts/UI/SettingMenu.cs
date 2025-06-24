using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public TextMeshProUGUI warningText; // UI Text để hiện thông báo

    // Biến lưu giá trị gốc
    private float originalVolume;
    private float originalMusicVolume;
    private float originalSensitivity;

    // Biến lưu giá trị tạm thời
    private float tempVolume;
    private float tempMusicVolume;
    private float tempSensitivity;

    private bool isDirty = false;

    [Header("UI Navigation")]
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingPanel;

    // Thêm các button và panel cho từng mục
    [SerializeField] private Button videoButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Button gameplayButton;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject gameplayPanel;

    void Start()
    {
        audioMixer.GetFloat("volume", out originalVolume);
        audioMixer.GetFloat("MusicVolume", out originalMusicVolume);
        // Lấy sensitivity từ PlayerLook
        PlayerLook playerLook = FindObjectOfType<PlayerLook>();
        if (playerLook != null)
            originalSensitivity = playerLook.xSensitivity;

        // Khởi tạo giá trị tạm thời
        tempVolume = originalVolume;
        tempMusicVolume = originalMusicVolume;
        tempSensitivity = originalSensitivity;

        warningText.gameObject.SetActive(false);

        if (backButton) backButton.onClick.AddListener(OnBackToMenu);

        if (videoButton) videoButton.onClick.AddListener(ShowVideoPanel);
        if (audioButton) audioButton.onClick.AddListener(ShowAudioPanel);
        if (gameplayButton) gameplayButton.onClick.AddListener(ShowGameplayPanel);
    }

    public void OnVolumeChanged(float value)
    {
        tempVolume = value;
        SetDirty();
    }

    public void OnMusicVolumeChanged(float value)
    {
        tempMusicVolume = value;
        SetDirty();
    }

    public void OnSensitivityChanged(float value)
    {
        tempSensitivity = value;
        SetDirty();
    }

    private void SetDirty()
    {
        isDirty = true;
        warningText.text = "You not saving these setting!";
        warningText.gameObject.SetActive(true);
    }

    public void OnApply()
    {
        SetVolume(tempVolume);
        SetMusicVolume(tempMusicVolume);
        SetSensitivity(tempSensitivity);

        // Lưu lại giá trị mới
        originalVolume = tempVolume;
        originalMusicVolume = tempMusicVolume;
        originalSensitivity = tempSensitivity;

        isDirty = false;
        warningText.gameObject.SetActive(false);
    }

    public void OnCancel()
    {
        tempVolume = originalVolume;
        tempMusicVolume = originalMusicVolume;
        tempSensitivity = originalSensitivity;

        // Có thể cập nhật lại UI slider ở đây nếu cần

        isDirty = false;
        warningText.gameObject.SetActive(false);
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume", volume);
    }
    public void SetMusicVolume(float musicVolume)
    {
        audioMixer.SetFloat("MusicVolume", musicVolume);
    }
    public void SetSensitivity(float sensitivity)
    {
        PlayerLook playerLook = FindObjectOfType<PlayerLook>();
        if (playerLook != null)
        {
            playerLook.SetSensitivity(sensitivity);
        }
    }
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void OnBackToMenu()
    {
        

        if (settingPanel) settingPanel.SetActive(false);
        if (menuPanel) menuPanel.SetActive(true);
    }

    private void ShowVideoPanel()
    {
        if (videoPanel) videoPanel.SetActive(true);
        if (audioPanel) audioPanel.SetActive(false);
        if (gameplayPanel) gameplayPanel.SetActive(false);
    }

    private void ShowAudioPanel()
    {
        if (videoPanel) videoPanel.SetActive(false);
        if (audioPanel) audioPanel.SetActive(true);
        if (gameplayPanel) gameplayPanel.SetActive(false);
    }

    private void ShowGameplayPanel()
    {
        if (videoPanel) videoPanel.SetActive(false);
        if (audioPanel) audioPanel.SetActive(false);
        if (gameplayPanel) gameplayPanel.SetActive(true);
    }
}
