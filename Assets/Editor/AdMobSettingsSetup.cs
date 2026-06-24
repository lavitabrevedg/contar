using UnityEditor;
using UnityEngine;

public static class AdMobSettingsSetup
{
    private const string SettingsPath = "Assets/Resources/AdMobSettings.asset";

    [MenuItem("contar/Ensure AdMob Settings")]
    public static void EnsureDefaultAsset()
    {
        AdMobSettings existingSettings = AssetDatabase.LoadAssetAtPath<AdMobSettings>(SettingsPath);
        if (existingSettings != null)
        {
            Debug.Log($"[AdMobSettingsSetup] AdMob settings already exist: {SettingsPath}");
            return;
        }

        AdMobSettings settings = ScriptableObject.CreateInstance<AdMobSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AdMobSettingsSetup] Created AdMob settings: {SettingsPath}");
    }
}
