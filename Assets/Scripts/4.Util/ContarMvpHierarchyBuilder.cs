#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ContarMvpHierarchyBuilder
{
    private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
    private const string InGameScenePath = "Assets/Scenes/InGameScene.unity";

    public static void Build()
    {
        BuildLobbyScene();
        BuildInGameScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[ContarMvpHierarchyBuilder] MVP hierarchy build complete.");
    }

    [MenuItem("contar/Build MVP Hierarchy")]
    public static void BuildFromMenu()
    {
        Build();
    }

    private static void BuildLobbyScene()
    {
        EditorSceneManager.OpenScene(LobbyScenePath);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[ContarMvpHierarchyBuilder] Lobby Canvas not found.");
            return;
        }

        GameObject systemsObject = EnsureRootObject("_Systems");
        SettingsService settingsService = EnsureComponent<SettingsService>(systemsObject);
        StageProgressService progressService = EnsureComponent<StageProgressService>(systemsObject);
        AudioService audioService = EnsureComponent<AudioService>(systemsObject);

        GameObject continueButtonObject = FindSceneObject("OpenMapButt");
        Button continueButton = continueButtonObject == null ? null : EnsureButton(continueButtonObject);
        if (continueButton != null)
            ClearButtonCalls(continueButton);

        GameObject currentStageObject = FindSceneObject("CurrentLevel");
        TMP_Text currentStageText = currentStageObject == null ? null : currentStageObject.GetComponent<TMP_Text>();
        if (currentStageText != null)
            currentStageText.text = "이어하기";

        GameObject optionButtonObject = FindSceneObject("OptionButt");
        Button optionButton = optionButtonObject == null ? null : EnsureButton(optionButtonObject);
        GameObject centerObject = FindSceneObject("Center");
        Transform center = centerObject == null ? canvas.transform : centerObject.transform;

        Button stageSelectButton = CreateButton(center, "StageSelectButton", "스테이지 선택", new Vector2(0f, 0f), new Vector2(1000f, 180f), out TMP_Text stageSelectButtonText);
        CanvasGroup stageSelectPanel = CreateStageSelectPanel(canvas.transform, out Button closeStageSelectButton, out Button[] stageButtons, out TMP_Text[] stageButtonTexts);

        StageSelectView stageSelectView = EnsureComponent<StageSelectView>(canvas.gameObject);
        StageSelectPresenter stageSelectPresenter = EnsureComponent<StageSelectPresenter>(canvas.gameObject);
        AssignObject(stageSelectView, "panel", stageSelectPanel);
        AssignObject(stageSelectView, "continueButton", continueButton);
        AssignObject(stageSelectView, "openStageSelectButton", stageSelectButton);
        AssignObject(stageSelectView, "closeStageSelectButton", closeStageSelectButton);
        AssignObject(stageSelectView, "currentStageText", currentStageText);
        AssignObjectArray(stageSelectView, "stageButtons", stageButtons);
        AssignObjectArray(stageSelectView, "stageButtonTexts", stageButtonTexts);
        AssignObject(stageSelectPresenter, "view", stageSelectView);
        AssignObject(stageSelectPresenter, "progressService", progressService);
        AssignObject(stageSelectPresenter, "audioService", audioService);

        CanvasGroup settingsPanel = CreateSettingsPanel(canvas.transform, out Button closeSettingsButton, out Button soundButton, out Button resetButton, out TMP_Text soundText);
        SettingsView settingsView = EnsureComponent<SettingsView>(canvas.gameObject);
        SettingsPresenter settingsPresenter = EnsureComponent<SettingsPresenter>(canvas.gameObject);
        AssignObject(settingsView, "panel", settingsPanel);
        AssignObject(settingsView, "openButton", optionButton);
        AssignObject(settingsView, "closeButton", closeSettingsButton);
        AssignObject(settingsView, "soundButton", soundButton);
        AssignObject(settingsView, "resetProgressButton", resetButton);
        AssignObject(settingsView, "soundButtonText", soundText);
        AssignObject(settingsPresenter, "view", settingsView);
        AssignObject(settingsPresenter, "settingsService", settingsService);
        AssignObject(settingsPresenter, "progressService", progressService);
        AssignObject(settingsPresenter, "audioService", audioService);

        SetGameObjectActive(stageSelectPanel.gameObject, false);
        SetGameObjectActive(settingsPanel.gameObject, false);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
    }

    private static void BuildInGameScene()
    {
        EditorSceneManager.OpenScene(InGameScenePath);

        GameObject systemsObject = FindSceneObject("_Systems");
        if (systemsObject == null)
            systemsObject = EnsureRootObject("_Systems");

        SettingsService settingsService = EnsureComponent<SettingsService>(systemsObject);
        StageProgressService progressService = EnsureComponent<StageProgressService>(systemsObject);
        AudioService audioService = EnsureComponent<AudioService>(systemsObject);

        GameObject popupCanvasObject = FindSceneObject("Canvas_Popup");
        Canvas popupCanvas = popupCanvasObject == null ? null : popupCanvasObject.GetComponent<Canvas>();

        if (popupCanvas != null)
        {
            GameObject optionButtonObject = FindSceneObject("OptionButt");
            Button optionButton = optionButtonObject == null ? null : EnsureButton(optionButtonObject);
            CanvasGroup settingsPanel = CreateSettingsPanel(popupCanvas.transform, out Button closeSettingsButton, out Button soundButton, out Button resetButton, out TMP_Text soundText);
            SettingsView settingsView = EnsureComponent<SettingsView>(popupCanvas.gameObject);
            SettingsPresenter settingsPresenter = EnsureComponent<SettingsPresenter>(popupCanvas.gameObject);
            AssignObject(settingsView, "panel", settingsPanel);
            AssignObject(settingsView, "openButton", optionButton);
            AssignObject(settingsView, "closeButton", closeSettingsButton);
            AssignObject(settingsView, "soundButton", soundButton);
            AssignObject(settingsView, "resetProgressButton", resetButton);
            AssignObject(settingsView, "soundButtonText", soundText);
            AssignObject(settingsPresenter, "view", settingsView);
            AssignObject(settingsPresenter, "settingsService", settingsService);
            AssignObject(settingsPresenter, "progressService", progressService);
            AssignObject(settingsPresenter, "audioService", audioService);
            SetGameObjectActive(settingsPanel.gameObject, false);
        }

        GameUIView gameUIView = Object.FindFirstObjectByType<GameUIView>();
        if (gameUIView != null)
        {
            GameObject clearPanelObject = FindSceneObject("StageClearPanel");
            GameObject failPanelObject = FindSceneObject("FailClearPanel");
            Transform clearPanel = clearPanelObject == null ? null : clearPanelObject.transform;
            Transform failPanel = failPanelObject == null ? null : failPanelObject.transform;

            if (clearPanel != null)
            {
                GameObject clearStageObject = FindSceneObject("ClearText");
                TMP_Text clearStageText = clearStageObject == null ? null : clearStageObject.GetComponent<TMP_Text>();
                TMP_Text clearMoveText = CreateText(clearPanel, "ClearMoveText", "남은 이동 0", new Vector2(0f, 270f), new Vector2(720f, 90f), 58f);
                TMP_Text rewardText = CreateText(clearPanel, "RewardText", "스킵권 +1", new Vector2(0f, 155f), new Vector2(720f, 90f), 52f);
                Button lobbyButton = CreateButton(clearPanel, "ClearLobbyButton", "로비로", new Vector2(12f, -695f), new Vector2(900f, 170f), out TMP_Text lobbyLabel);
                AssignObject(gameUIView, "clearStageText", clearStageText);
                AssignObject(gameUIView, "clearMoveText", clearMoveText);
                AssignObject(gameUIView, "rewardText", rewardText);
                AssignObject(gameUIView, "lobbyButton", lobbyButton);
            }

            if (failPanel != null)
            {
                TMP_Text failureInfoText = CreateText(failPanel, "FailureInfoText", "실패 0/3", new Vector2(0f, 270f), new Vector2(720f, 90f), 52f);
                AssignObject(gameUIView, "failureInfoText", failureInfoText);
            }
        }

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            AssignObject(gameManager, "progressService", progressService);
            AssignObject(gameManager, "audioService", audioService);
        }

        GameUIPresenter gameUIPresenter = Object.FindFirstObjectByType<GameUIPresenter>();
        if (gameUIPresenter != null)
            AssignObject(gameUIPresenter, "progressService", progressService);

        GameFlowController gameFlowController = Object.FindFirstObjectByType<GameFlowController>();
        if (gameFlowController != null)
            AssignObject(gameFlowController, "progressService", progressService);

        EditorSceneManager.MarkSceneDirty(systemsObject.scene);
        EditorSceneManager.SaveScene(systemsObject.scene);
    }

    private static CanvasGroup CreateStageSelectPanel(Transform parent, out Button closeButton, out Button[] stageButtons, out TMP_Text[] stageButtonTexts)
    {
        GameObject panelObject = CreatePanelObject(parent, "StageSelectPanel", new Vector2(0f, 0f), new Vector2(1080f, 1500f), new Color(0.07f, 0.08f, 0.1f, 0.96f));
        CreateText(panelObject.transform, "Title", "스테이지 선택", new Vector2(0f, 610f), new Vector2(820f, 110f), 62f);
        closeButton = CreateButton(panelObject.transform, "CloseButton", "닫기", new Vector2(0f, -610f), new Vector2(820f, 120f), out TMP_Text closeText);

        int stageButtonCount = 12;
        stageButtons = new Button[stageButtonCount];
        stageButtonTexts = new TMP_Text[stageButtonCount];

        for (int i = 0; i < stageButtonCount; i++)
        {
            int column = i % 3;
            int row = i / 3;
            float x = -300f + column * 300f;
            float y = 390f - row * 190f;
            stageButtons[i] = CreateButton(panelObject.transform, $"StageButton_{i + 1:00}", $"Stage {i + 1}", new Vector2(x, y), new Vector2(250f, 130f), out TMP_Text labelText);
            stageButtonTexts[i] = labelText;
        }

        return EnsureComponent<CanvasGroup>(panelObject);
    }

    private static CanvasGroup CreateSettingsPanel(Transform parent, out Button closeButton, out Button soundButton, out Button resetButton, out TMP_Text soundText)
    {
        GameObject panelObject = CreatePanelObject(parent, "SettingsPanel", new Vector2(0f, 0f), new Vector2(900f, 760f), new Color(0.07f, 0.08f, 0.1f, 0.96f));
        CreateText(panelObject.transform, "Title", "설정", new Vector2(0f, 255f), new Vector2(720f, 110f), 66f);
        soundButton = CreateButton(panelObject.transform, "SoundButton", "사운드 켜짐", new Vector2(0f, 80f), new Vector2(700f, 120f), out soundText);
        resetButton = CreateButton(panelObject.transform, "ResetProgressButton", "진행 초기화", new Vector2(0f, -75f), new Vector2(700f, 120f), out TMP_Text resetText);
        closeButton = CreateButton(panelObject.transform, "CloseButton", "닫기", new Vector2(0f, -230f), new Vector2(700f, 120f), out TMP_Text closeText);
        return EnsureComponent<CanvasGroup>(panelObject);
    }

    private static GameObject CreatePanelObject(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject oldObject = FindDirectChild(parent, name);
        if (oldObject != null)
            Object.DestroyImmediate(oldObject);

        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = 5;
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        return panelObject;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 sizeDelta, out TMP_Text labelText)
    {
        GameObject buttonObject = CreatePanelObject(parent, name, anchoredPosition, sizeDelta, new Color(0.92f, 0.92f, 0.92f, 1f));
        Button button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.transition = Selectable.Transition.ColorTint;

        labelText = CreateText(buttonObject.transform, "Label", label, Vector2.zero, new Vector2(sizeDelta.x - 32f, sizeDelta.y - 18f), 42f);
        labelText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize)
    {
        GameObject oldObject = FindDirectChild(parent, name);
        if (oldObject != null)
            Object.DestroyImmediate(oldObject);

        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        TMP_Text textComponent = textObject.GetComponent<TMP_Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        return textComponent;
    }

    private static Button EnsureButton(GameObject targetObject)
    {
        Button button = targetObject.GetComponent<Button>();
        if (button == null)
            button = targetObject.AddComponent<Button>();

        if (button.targetGraphic == null)
            button.targetGraphic = targetObject.GetComponent<Graphic>();

        return button;
    }

    private static T EnsureComponent<T>(GameObject targetObject) where T : Component
    {
        T component = targetObject.GetComponent<T>();
        if (component == null)
            component = targetObject.AddComponent<T>();

        return component;
    }

    private static GameObject EnsureRootObject(string objectName)
    {
        GameObject rootObject = FindSceneObject(objectName);
        if (rootObject == null)
            rootObject = new GameObject(objectName);

        return rootObject;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform targetTransform = transforms[i];
            if (targetTransform != null && targetTransform.name == objectName)
                return targetTransform.gameObject;
        }

        return null;
    }

    private static GameObject FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child == null ? null : child.gameObject;
    }

    private static void ClearButtonCalls(Button button)
    {
        if (button == null)
            return;

        button.onClick = new Button.ButtonClickedEvent();
    }

    private static void AssignObject(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignObjectArray(Object target, string propertyName, Object[] values)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.arraySize = values == null ? 0 : values.Length;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetGameObjectActive(GameObject targetObject, bool isActive)
    {
        if (targetObject != null)
            targetObject.SetActive(isActive);
    }
}
#endif
