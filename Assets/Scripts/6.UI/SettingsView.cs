using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsView : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Image soundButtonImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private TMP_Text soundButtonText;
    [SerializeField] private bool hidePanelOnAwake = true;
    [SerializeField] private float panelTweenDuration = 0.18f;

    public event Action OpenClicked;
    public event Action CloseClicked;
    public event Action SoundClicked;

    private void Awake()
    {
        ResolveSoundButtonImage();

        if (hidePanelOnAwake)
            SetPanelVisible(false);
    }

    private void OnEnable()
    {
        if (openButton != null)
            openButton.onClick.AddListener(NotifyOpenClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(NotifyCloseClicked);

        if (soundButton != null)
            soundButton.onClick.AddListener(NotifySoundClicked);
    }

    private void OnDisable()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(NotifyOpenClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(NotifyCloseClicked);

        if (soundButton != null)
            soundButton.onClick.RemoveListener(NotifySoundClicked);
    }

    public void SetPanelVisible(bool isVisible)
    {
        if (panel == null)
            return;

        panel.DOKill();
        panel.transform.DOKill();

        if (isVisible)
        {
            panel.gameObject.SetActive(true);
            panel.alpha = 0f;
            panel.transform.localScale = Vector3.one * 0.96f;
            panel.interactable = false;
            panel.blocksRaycasts = true;

            panel.DOFade(1f, panelTweenDuration);
            panel.transform.DOScale(Vector3.one, panelTweenDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => panel.interactable = true);
            return;
        }

        panel.interactable = false;
        panel.blocksRaycasts = false;

        panel.DOFade(0f, panelTweenDuration);
        panel.transform.DOScale(Vector3.one * 0.98f, panelTweenDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => panel.gameObject.SetActive(false));
    }

    public void SetSoundEnabled(bool isEnabled)
    {
        ResolveSoundButtonImage();

        if (soundButtonImage != null)
        {
            Sprite soundSprite = isEnabled ? soundOnSprite : soundOffSprite;
            if (soundSprite != null)
                soundButtonImage.sprite = soundSprite;
        }

        if (soundButtonText != null)
            soundButtonText.text = isEnabled ? "Sound On" : "Sound Off";
    }

    private void ResolveSoundButtonImage()
    {
        if (soundButtonImage != null || soundButton == null)
            return;

        soundButtonImage = soundButton.targetGraphic as Image;
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
}
