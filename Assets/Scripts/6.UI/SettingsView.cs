using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsView : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button resetProgressButton;
    [SerializeField] private TMP_Text soundButtonText;

    public event Action OpenClicked;
    public event Action CloseClicked;
    public event Action SoundClicked;
    public event Action ResetProgressClicked;

    private void OnEnable()
    {
        if (openButton != null)
            openButton.onClick.AddListener(NotifyOpenClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(NotifyCloseClicked);

        if (soundButton != null)
            soundButton.onClick.AddListener(NotifySoundClicked);

        if (resetProgressButton != null)
            resetProgressButton.onClick.AddListener(NotifyResetProgressClicked);
    }

    private void OnDisable()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(NotifyOpenClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(NotifyCloseClicked);

        if (soundButton != null)
            soundButton.onClick.RemoveListener(NotifySoundClicked);

        if (resetProgressButton != null)
            resetProgressButton.onClick.RemoveListener(NotifyResetProgressClicked);
    }

    public void SetPanelVisible(bool isVisible)
    {
        if (panel == null)
            return;

        panel.gameObject.SetActive(isVisible);
        panel.alpha = isVisible ? 1f : 0f;
        panel.interactable = isVisible;
        panel.blocksRaycasts = isVisible;
    }

    public void SetSoundEnabled(bool isEnabled)
    {
        if (soundButtonText != null)
            soundButtonText.text = isEnabled ? "사운드 켜짐" : "사운드 꺼짐";
    }

    private void NotifyOpenClicked()
    {
        OpenClicked?.Invoke();
    }

    private void NotifyCloseClicked()
    {
        CloseClicked?.Invoke();
    }

    private void NotifySoundClicked()
    {
        SoundClicked?.Invoke();
    }

    private void NotifyResetProgressClicked()
    {
        ResetProgressClicked?.Invoke();
    }
}
