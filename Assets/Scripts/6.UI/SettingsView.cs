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
    [SerializeField] private float soundItemTweenDuration = 0.16f;
    [SerializeField] private float soundImageTweenDelay = 0.04f;
    [SerializeField] private float soundTextTweenDelay = 0.08f;

    public event Action OpenClicked;
    public event Action CloseClicked;
    public event Action SoundClicked;

    private Vector3 soundButtonImageBaseScale = Vector3.one;
    private Vector3 soundButtonTextBaseScale = Vector3.one;
    private float soundButtonImageBaseAlpha = 1f;
    private float soundButtonTextBaseAlpha = 1f;
    private bool hasSoundAnimationDefaults;

    private void Awake()
    {
        ResolveSoundButtonImage();
        CacheSoundAnimationDefaults();

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

        KillPanelTweens();
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

            PrepareSoundControlsForShow();
            panel.DOFade(1f, panelTweenDuration);
            panel.transform.DOScale(Vector3.one, panelTweenDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => panel.interactable = true);
            AnimateSoundControls();
            return;
        }

        KillSoundControlTweens();
        RestoreSoundControlVisuals();
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
            soundButtonText.text = "Sound";
    }

    private void ResolveSoundButtonImage()
    {
        if (soundButtonImage != null || soundButton == null)
            return;

        soundButtonImage = soundButton.targetGraphic as Image;
    }

    private void KillPanelTweens()
    {
        if (panel == null)
            return;

        panel.DOKill();
        panel.transform.DOKill();
        KillSoundControlTweens();
    }

    private void CacheSoundAnimationDefaults()
    {
        if (hasSoundAnimationDefaults)
            return;

        ResolveSoundButtonImage();

        if (soundButtonImage != null)
        {
            soundButtonImageBaseScale = soundButtonImage.transform.localScale;
            soundButtonImageBaseAlpha = soundButtonImage.color.a;
        }

        if (soundButtonText != null)
        {
            soundButtonTextBaseScale = soundButtonText.transform.localScale;
            soundButtonTextBaseAlpha = soundButtonText.color.a;
        }

        hasSoundAnimationDefaults = true;
    }

    private void PrepareSoundControlsForShow()
    {
        CacheSoundAnimationDefaults();
        KillSoundControlTweens();

        if (soundButtonImage != null)
        {
            Color imageColor = soundButtonImage.color;
            imageColor.a = 0f;
            soundButtonImage.color = imageColor;
            soundButtonImage.transform.localScale = soundButtonImageBaseScale * 0.9f;
        }

        if (soundButtonText != null)
        {
            Color textColor = soundButtonText.color;
            textColor.a = 0f;
            soundButtonText.color = textColor;
            soundButtonText.transform.localScale = soundButtonTextBaseScale * 0.9f;
        }
    }

    private void AnimateSoundControls()
    {
        if (soundButtonImage != null)
        {
            soundButtonImage.DOFade(soundButtonImageBaseAlpha, soundItemTweenDuration)
                .SetDelay(soundImageTweenDelay);
            soundButtonImage.transform.DOScale(soundButtonImageBaseScale, soundItemTweenDuration)
                .SetDelay(soundImageTweenDelay)
                .SetEase(Ease.OutBack);
        }

        if (soundButtonText != null)
        {
            soundButtonText.DOFade(soundButtonTextBaseAlpha, soundItemTweenDuration)
                .SetDelay(soundTextTweenDelay);
            soundButtonText.transform.DOScale(soundButtonTextBaseScale, soundItemTweenDuration)
                .SetDelay(soundTextTweenDelay)
                .SetEase(Ease.OutBack);
        }
    }

    private void RestoreSoundControlVisuals()
    {
        CacheSoundAnimationDefaults();

        if (soundButtonImage != null)
        {
            Color imageColor = soundButtonImage.color;
            imageColor.a = soundButtonImageBaseAlpha;
            soundButtonImage.color = imageColor;
            soundButtonImage.transform.localScale = soundButtonImageBaseScale;
        }

        if (soundButtonText != null)
        {
            Color textColor = soundButtonText.color;
            textColor.a = soundButtonTextBaseAlpha;
            soundButtonText.color = textColor;
            soundButtonText.transform.localScale = soundButtonTextBaseScale;
        }
    }

    private void KillSoundControlTweens()
    {
        if (soundButtonImage != null)
        {
            soundButtonImage.DOKill();
            soundButtonImage.transform.DOKill();
        }

        if (soundButtonText != null)
        {
            soundButtonText.DOKill();
            soundButtonText.transform.DOKill();
        }
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
