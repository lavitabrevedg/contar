using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class PrivacyPolicyLink : MonoBehaviour
{
    private const string KoreanLocaleCode = "ko";
    private const string KoreanPrivacyPolicyUrl = "https://lavitabrevedg.github.io/contar/privacy-policy/ko.html";
    private const string EnglishPrivacyPolicyUrl = "https://lavitabrevedg.github.io/contar/privacy-policy/en.html";

    public static void OpenDefaultPrivacyPolicy()
    {
        Application.OpenURL(GetLocalizedPrivacyPolicyUrl());
    }

    public void OpenPrivacyPolicy()
    {
        Application.OpenURL(GetLocalizedPrivacyPolicyUrl());
    }

    private static string GetLocalizedPrivacyPolicyUrl()
    {
        Locale selectedLocale = LocalizationSettings.SelectedLocale;
        if (selectedLocale != null && selectedLocale.Identifier.Code == KoreanLocaleCode)
            return KoreanPrivacyPolicyUrl;

        return EnglishPrivacyPolicyUrl;
    }
}
