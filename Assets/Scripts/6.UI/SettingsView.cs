using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SettingsView : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [FormerlySerializedAs("soundButton")]
    [SerializeField] private Button soundEffectButton;
    [FormerlySerializedAs("soundButtonImage")]
    [SerializeField] private Image soundEffectButtonImage;
    [FormerlySerializedAs("soundOnSprite")]
    [FormerlySerializedAs("soundEffectOnSprite")]
    [SerializeField] private Sprite onSprite;
    [FormerlySerializedAs("soundOffSprite")]
    [FormerlySerializedAs("soundEffectOffSprite")]
    [SerializeField] private Sprite offSprite;
    [FormerlySerializedAs("soundButtonText")]
    [SerializeField] private TMP_Text soundEffectButtonText;
    [SerializeField] private Button musicButton;
    [SerializeField] private Image musicButtonImage;
    [SerializeField] private TMP_Text musicButtonText;
    [SerializeField] private Button vibrationButton;
    [SerializeField] private Image vibrationButtonImage;
    [SerializeField] private TMP_Text vibrationButtonText;
    [SerializeField] private bool hidePanelOnAwake = true;
    [SerializeField] private float panelTweenDuration = 0.18f;
    [FormerlySerializedAs("soundItemTweenDuration")]
    [SerializeField] private float settingItemTweenDuration = 0.16f;
    [FormerlySerializedAs("soundImageTweenDelay")]
    [SerializeField] private float firstImageTweenDelay = 0.04f;
    [FormerlySerializedAs("soundTextTweenDelay")]
    [SerializeField] private float firstTextTweenDelay = 0.08f;
    [SerializeField] private float settingRowTweenInterval = 0.06f;

    public event Action OpenClicked;
    public event Action CloseClicked;
    public event Action SoundEffectClicked;
    public event Action MusicClicked;
    public event Action VibrationClicked;

    private Image[] settingButtonImages;
    private TMP_Text[] settingButtonTexts;
    private Vector3[] settingButtonImageBaseScales;
    private Vector3[] settingButtonTextBaseScales;
    private float[] settingButtonImageBaseAlphas;
    private float[] settingButtonTextBaseAlphas;
    private bool hasSettingAnimationDefaults;

    private void Awake()
    {
        ResolveSoundEffectButtonImage();
        ResolveMusicButtonImage();
        ResolveVibrationButtonImage();
        CacheSettingAnimationDefaults();

        if (hidePanelOnAwake)
            SetPanelVisible(false);
    }

    private void OnEnable()
    {
        if (openButton != null)
            openButton.onClick.AddListener(NotifyOpenClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(NotifyCloseClicked);

        if (soundEffectButton != null)
            soundEffectButton.onClick.AddListener(NotifySoundEffectClicked);

        if (musicButton != null)
            musicButton.onClick.AddListener(NotifyMusicClicked);

        if (vibrationButton != null)
            vibrationButton.onClick.AddListener(NotifyVibrationClicked);
    }

    private void OnDisable()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(NotifyOpenClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(NotifyCloseClicked);

        if (soundEffectButton != null)
            soundEffectButton.onClick.RemoveListener(NotifySoundEffectClicked);

        if (musicButton != null)
            musicButton.onClick.RemoveListener(NotifyMusicClicked);

        if (vibrationButton != null)
            vibrationButton.onClick.RemoveListener(NotifyVibrationClicked);

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

            PrepareSettingControlsForShow();
            panel.DOFade(1f, panelTweenDuration);
            panel.transform.DOScale(Vector3.one, panelTweenDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => panel.interactable = true);
            AnimateSettingControls();
            return;
        }

        KillSettingControlTweens();
        RestoreSettingControlVisuals();
        panel.interactable = false;
        panel.blocksRaycasts = false;

        panel.DOFade(0f, panelTweenDuration);
        panel.transform.DOScale(Vector3.one * 0.98f, panelTweenDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => panel.gameObject.SetActive(false));
    }

    public void SetSoundEffectEnabled(bool isEnabled)
    {
        ResolveSoundEffectButtonImage();

        if (soundEffectButtonImage != null)
        {
            Sprite soundSprite = isEnabled ? onSprite : offSprite;
            if (soundSprite != null)
                soundEffectButtonImage.sprite = soundSprite;
        }
    }

    public void SetMusicEnabled(bool isEnabled)
    {
        ResolveMusicButtonImage();

        if (musicButtonImage == null)
            return;

        Sprite musicSprite = isEnabled ? onSprite : offSprite;
        if (musicSprite != null)
            musicButtonImage.sprite = musicSprite;
    }

    public void SetVibrationEnabled(bool isEnabled)
    {
        ResolveVibrationButtonImage();

        if (vibrationButtonImage == null)
            return;

        Sprite vibrationSprite = isEnabled ? onSprite : offSprite;
        if (vibrationSprite != null)
            vibrationButtonImage.sprite = vibrationSprite;
    }

    private void ResolveSoundEffectButtonImage()
    {
        if (soundEffectButtonImage != null || soundEffectButton == null)
            return;

        soundEffectButtonImage = soundEffectButton.targetGraphic as Image;
    }

    private void ResolveMusicButtonImage()
    {
        if (musicButtonImage != null || musicButton == null)
            return;

        musicButtonImage = musicButton.targetGraphic as Image;
    }

    private void ResolveVibrationButtonImage()
    {
        if (vibrationButtonImage != null || vibrationButton == null)
            return;

        vibrationButtonImage = vibrationButton.targetGraphic as Image;
    }

    private void KillPanelTweens()
    {
        if (panel == null)
            return;

        panel.DOKill();
        panel.transform.DOKill();
        KillSettingControlTweens();
    }

    private void BuildSettingAnimationTargets()
    {
        settingButtonImages = new Image[]
        {
            musicButtonImage,
            vibrationButtonImage,
            soundEffectButtonImage
        };
        settingButtonTexts = new TMP_Text[]
        {
            musicButtonText,
            vibrationButtonText,
            soundEffectButtonText
        };
        settingButtonImageBaseScales = new Vector3[settingButtonImages.Length];
        settingButtonTextBaseScales = new Vector3[settingButtonTexts.Length];
        settingButtonImageBaseAlphas = new float[settingButtonImages.Length];
        settingButtonTextBaseAlphas = new float[settingButtonTexts.Length];
    }

    private void CacheSettingAnimationDefaults()
    {
        if (hasSettingAnimationDefaults)
            return;

        ResolveSoundEffectButtonImage();
        ResolveMusicButtonImage();
        ResolveVibrationButtonImage();
        BuildSettingAnimationTargets();

        for (int index = 0; index < settingButtonImages.Length; index++)
        {
            Image buttonImage = settingButtonImages[index];
            TMP_Text buttonText = settingButtonTexts[index];

            if (buttonImage != null)
            {
                settingButtonImageBaseScales[index] = buttonImage.transform.localScale;
                settingButtonImageBaseAlphas[index] = buttonImage.color.a;
            }

            if (buttonText != null)
            {
                settingButtonTextBaseScales[index] = buttonText.transform.localScale;
                settingButtonTextBaseAlphas[index] = buttonText.color.a;
            }
        }

        hasSettingAnimationDefaults = true;
    }

    private void PrepareSettingControlsForShow()
    {
        CacheSettingAnimationDefaults();
        KillSettingControlTweens();

        for (int index = 0; index < settingButtonImages.Length; index++)
        {
            Image buttonImage = settingButtonImages[index];
            TMP_Text buttonText = settingButtonTexts[index];

            if (buttonImage != null)
            {
                Color imageColor = buttonImage.color;
                imageColor.a = 0f;
                buttonImage.color = imageColor;
                buttonImage.transform.localScale = settingButtonImageBaseScales[index] * 0.9f;
            }

            if (buttonText != null)
            {
                Color textColor = buttonText.color;
                textColor.a = 0f;
                buttonText.color = textColor;
                buttonText.transform.localScale = settingButtonTextBaseScales[index] * 0.9f;
            }
        }
    }

    private void AnimateSettingControls()
    {
        for (int index = 0; index < settingButtonImages.Length; index++)
        {
            Image buttonImage = settingButtonImages[index];
            TMP_Text buttonText = settingButtonTexts[index];
            float imageDelay = firstImageTweenDelay + settingRowTweenInterval * index;
            float textDelay = firstTextTweenDelay + settingRowTweenInterval * index;

            if (buttonImage != null)
            {
                buttonImage.DOFade(settingButtonImageBaseAlphas[index], settingItemTweenDuration)
                    .SetDelay(imageDelay)
                    .SetUpdate(true);
                buttonImage.transform.DOScale(settingButtonImageBaseScales[index], settingItemTweenDuration)
                    .SetDelay(imageDelay)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }

            if (buttonText != null)
            {
                buttonText.DOFade(settingButtonTextBaseAlphas[index], settingItemTweenDuration)
                    .SetDelay(textDelay)
                    .SetUpdate(true);
                buttonText.transform.DOScale(settingButtonTextBaseScales[index], settingItemTweenDuration)
                    .SetDelay(textDelay)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
        }
    }

    private void RestoreSettingControlVisuals()
    {
        CacheSettingAnimationDefaults();

        for (int index = 0; index < settingButtonImages.Length; index++)
        {
            Image buttonImage = settingButtonImages[index];
            TMP_Text buttonText = settingButtonTexts[index];

            if (buttonImage != null)
            {
                Color imageColor = buttonImage.color;
                imageColor.a = settingButtonImageBaseAlphas[index];
                buttonImage.color = imageColor;
                buttonImage.transform.localScale = settingButtonImageBaseScales[index];
            }

            if (buttonText != null)
            {
                Color textColor = buttonText.color;
                textColor.a = settingButtonTextBaseAlphas[index];
                buttonText.color = textColor;
                buttonText.transform.localScale = settingButtonTextBaseScales[index];
            }
        }
    }

    private void KillSettingControlTweens()
    {
        if (settingButtonImages == null || settingButtonTexts == null)
            return;

        for (int index = 0; index < settingButtonImages.Length; index++)
        {
            Image buttonImage = settingButtonImages[index];
            TMP_Text buttonText = settingButtonTexts[index];

            if (buttonImage != null)
            {
                buttonImage.DOKill();
                buttonImage.transform.DOKill();
            }

            if (buttonText != null)
            {
                buttonText.DOKill();
                buttonText.transform.DOKill();
            }
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

    private void NotifySoundEffectClicked()
    {
        SoundEffectClicked?.Invoke();
    }

    private void NotifyMusicClicked()
    {
        MusicClicked?.Invoke();
    }

    private void NotifyVibrationClicked()
    {
        VibrationClicked?.Invoke();
    }
}
