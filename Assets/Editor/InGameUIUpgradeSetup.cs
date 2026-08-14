using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InGameUIUpgradeSetup
{
    private const string InGameScenePath = "Assets/Scenes/InGameScene.unity";
    private const string HintParticlePrefabPath =
        "Assets/Art/Effects/Hint/Generated/HintButtonGuidanceGlow.prefab";

    [MenuItem("Tools/Contar/Apply In-Game UI Upgrade")]
    public static void ApplyAll()
    {
        MusicMixerSetup.EnsureMusicMixer();

        Scene inGameScene = EditorSceneManager.OpenScene(InGameScenePath, OpenSceneMode.Single);
        if (!inGameScene.IsValid())
            throw new InvalidOperationException($"Failed to open {InGameScenePath}.");

        GameUIView gameUIView = UnityEngine.Object.FindFirstObjectByType<GameUIView>(FindObjectsInactive.Include);
        if (gameUIView == null)
            throw new InvalidOperationException("GameUIView is missing from InGameScene.");

        SerializedObject serializedView = new SerializedObject(gameUIView);
        SetupHintParticle(serializedView);
        SetupTutorialSpotlight(serializedView);
        ResizeSwipeHand(serializedView);
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(gameUIView);
        EditorSceneManager.MarkSceneDirty(inGameScene);
        EditorSceneManager.SaveScene(inGameScene);
        AssetDatabase.SaveAssets();
        Debug.Log("[InGameUIUpgradeSetup] Hint particles, tutorial spotlight, and swipe hand are configured.");
    }

    private static void SetupHintParticle(SerializedObject serializedView)
    {
        Button hintButton = GetRequiredReference<Button>(serializedView, "hintButton");
        HintButtonParticleEffect particleEffect = hintButton.GetComponent<HintButtonParticleEffect>();
        if (particleEffect == null)
            particleEffect = hintButton.gameObject.AddComponent<HintButtonParticleEffect>();

        SerializedObject serializedEffect = new SerializedObject(particleEffect);
        serializedEffect.FindProperty("particlePrefab").objectReferenceValue =
            LoadRequiredAsset<GameObject>(HintParticlePrefabPath);
        serializedEffect.FindProperty("particleScale").floatValue = 1f;
        serializedEffect.FindProperty("screenOffset").vector2Value = Vector2.zero;
        serializedEffect.FindProperty("distanceFromCamera").floatValue = 2f;
        serializedEffect.FindProperty("sortingOrder").intValue = 1000;
        serializedEffect.ApplyModifiedPropertiesWithoutUndo();
        serializedView.FindProperty("hintButtonParticleEffect").objectReferenceValue = particleEffect;
        EditorUtility.SetDirty(particleEffect);
    }

    private static void SetupTutorialSpotlight(SerializedObject serializedView)
    {
        GameObject tutorialDialog = GetRequiredReference<GameObject>(serializedView, "tutorialDialog");
        RectTransform tutorialDialogRect = tutorialDialog.transform as RectTransform;
        if (tutorialDialogRect == null)
            throw new InvalidOperationException("TutorialDialog must use RectTransform.");

        DestroyLegacyDimmer(tutorialDialogRect, "TutorialDimmerLeft");
        DestroyLegacyDimmer(tutorialDialogRect, "TutorialDimmerRight");
        DestroyLegacyDimmer(tutorialDialogRect, "TutorialDimmerTop");
        DestroyLegacyDimmer(tutorialDialogRect, "TutorialDimmerBottom");

        Transform existingSpotlight = tutorialDialogRect.Find("TutorialSpotlight");
        TutorialSpotlightGraphic tutorialSpotlight;
        if (existingSpotlight == null)
        {
            GameObject spotlightObject = new GameObject(
                "TutorialSpotlight",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TutorialSpotlightGraphic));
            spotlightObject.layer = tutorialDialog.layer;
            RectTransform spotlightRect = spotlightObject.GetComponent<RectTransform>();
            spotlightRect.SetParent(tutorialDialogRect, false);
            spotlightRect.anchorMin = Vector2.zero;
            spotlightRect.anchorMax = Vector2.one;
            spotlightRect.anchoredPosition = Vector2.zero;
            spotlightRect.sizeDelta = Vector2.zero;
            spotlightRect.SetAsFirstSibling();
            tutorialSpotlight = spotlightObject.GetComponent<TutorialSpotlightGraphic>();
        }
        else
        {
            existingSpotlight.gameObject.layer = tutorialDialog.layer;
            tutorialSpotlight = existingSpotlight.GetComponent<TutorialSpotlightGraphic>();
            if (tutorialSpotlight == null)
                tutorialSpotlight = existingSpotlight.gameObject.AddComponent<TutorialSpotlightGraphic>();

            existingSpotlight.SetAsFirstSibling();
        }

        tutorialSpotlight.color = new Color(0.04f, 0.02f, 0.1f, 0.72f);
        tutorialSpotlight.raycastTarget = false;
        serializedView.FindProperty("tutorialSpotlight").objectReferenceValue = tutorialSpotlight;
        EditorUtility.SetDirty(tutorialSpotlight);
    }

    private static void ResizeSwipeHand(SerializedObject serializedView)
    {
        Image swipeTutorialHandImage = GetRequiredReference<Image>(serializedView, "swipeTutorialHandImage");
        swipeTutorialHandImage.rectTransform.sizeDelta = new Vector2(260f, 260f);
        EditorUtility.SetDirty(swipeTutorialHandImage.rectTransform);
    }

    private static void DestroyLegacyDimmer(RectTransform tutorialDialogRect, string dimmerName)
    {
        Transform legacyDimmer = tutorialDialogRect.Find(dimmerName);
        if (legacyDimmer != null)
            UnityEngine.Object.DestroyImmediate(legacyDimmer.gameObject);
    }

    private static T GetRequiredReference<T>(SerializedObject serializedObject, string propertyName)
        where T : UnityEngine.Object
    {
        SerializedProperty serializedProperty = serializedObject.FindProperty(propertyName);
        if (serializedProperty == null)
            throw new InvalidOperationException($"Serialized property is missing: {propertyName}");

        T referencedObject = serializedProperty.objectReferenceValue as T;
        if (referencedObject == null)
            throw new InvalidOperationException($"Serialized reference is missing: {propertyName}");

        return referencedObject;
    }

    private static T LoadRequiredAsset<T>(string assetPath)
        where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
            throw new InvalidOperationException($"Required asset is missing: {assetPath}");

        return asset;
    }
}
