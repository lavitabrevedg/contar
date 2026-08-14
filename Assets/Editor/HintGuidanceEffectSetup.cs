using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HintGuidanceEffectSetup
{
    private const string InGameScenePath = "Assets/Scenes/InGameScene.unity";
    private const string EffectFolderPath = "Assets/Art/Effects/Hint/Generated";
    private const string GlowTexturePath = EffectFolderPath + "/HintGlowMote.png";
    private const string PathTexturePath = EffectFolderPath + "/HintPathLight.png";
    private const string SourceMaterialPath = "Assets/Art/Effects/Hint/Circles_Additive.mat";
    private const string GlowMaterialPath = EffectFolderPath + "/HintGlowMote_Additive.mat";
    private const string PathMaterialPath = EffectFolderPath + "/HintPathLight_Additive.mat";
    private const string ButtonPrefabPath = EffectFolderPath + "/HintButtonGuidanceGlow.prefab";
    private const string RoutePrefabPath = EffectFolderPath + "/HintRouteGuidanceLight.prefab";

    [MenuItem("Tools/Contar/Apply Hint Guidance Effects")]
    public static void ApplyAll()
    {
        ConfigureTexture(GlowTexturePath);
        ConfigureTexture(PathTexturePath);

        Texture2D glowTexture = LoadRequiredAsset<Texture2D>(GlowTexturePath);
        Texture2D pathTexture = LoadRequiredAsset<Texture2D>(PathTexturePath);
        Material sourceMaterial = LoadRequiredAsset<Material>(SourceMaterialPath);
        Material glowMaterial = CreateOrUpdateMaterial(
            sourceMaterial,
            glowTexture,
            GlowMaterialPath,
            "HintGlowMote_Additive");
        Material pathMaterial = CreateOrUpdateMaterial(
            sourceMaterial,
            pathTexture,
            PathMaterialPath,
            "HintPathLight_Additive");

        GameObject buttonPrefab = CreateButtonEffectPrefab(glowMaterial);
        GameObject routePrefab = CreateRouteEffectPrefab(pathMaterial);
        ConfigureScene(buttonPrefab, routePrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HintGuidanceEffectSetup] Generated sprites and guidance particle prefabs are configured.");
    }

    private static void ConfigureTexture(string texturePath)
    {
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (textureImporter == null)
            throw new InvalidOperationException($"Texture importer is missing: {texturePath}");

        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Single;
        textureImporter.alphaIsTransparency = true;
        textureImporter.mipmapEnabled = false;
        textureImporter.wrapMode = TextureWrapMode.Clamp;
        textureImporter.filterMode = FilterMode.Bilinear;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.maxTextureSize = 256;
        textureImporter.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial(
        Material sourceMaterial,
        Texture2D texture,
        string materialPath,
        string materialName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(sourceMaterial);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            EditorUtility.CopySerialized(sourceMaterial, material);
        }

        material.name = materialName;
        material.mainTexture = texture;
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateButtonEffectPrefab(Material material)
    {
        GameObject effectObject = new GameObject("HintButtonGuidanceGlow");
        try
        {
            ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
            ConfigureButtonParticleSystem(particleSystem, material);
            return PrefabUtility.SaveAsPrefabAsset(effectObject, ButtonPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(effectObject);
        }
    }

    private static GameObject CreateRouteEffectPrefab(Material material)
    {
        GameObject effectObject = new GameObject("HintRouteGuidanceLight");
        try
        {
            ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
            ConfigureRouteParticleSystem(particleSystem, material);
            return PrefabUtility.SaveAsPrefabAsset(effectObject, RoutePrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(effectObject);
        }
    }

    private static void ConfigureButtonParticleSystem(ParticleSystem particleSystem, Material material)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = 0.9f;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = true;
        main.useUnscaledTime = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 0.95f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.19f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.88f, 0.42f, 0.72f),
            new Color(1f, 0.98f, 0.78f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 6;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.48f;
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.radial = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateGuidanceGradient(0.78f);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.22f, 1.15f),
                new Keyframe(0.55f, 0.9f),
                new Keyframe(1f, 0.35f)));

        ParticleSystem.NoiseModule noise = particleSystem.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = 0.025f;
        noise.frequency = 0.7f;
        noise.scrollSpeed = 0.25f;

        ConfigureRenderer(particleSystem, material, ParticleSystemRenderSpace.View);
    }

    private static void ConfigureRouteParticleSystem(ParticleSystem particleSystem, Material material)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = 1.4f;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.15f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.58f, 0.72f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.22f, 0.8f),
            new Color(1f, 0.98f, 0.72f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 4;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0.7f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = false;

        ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateGuidanceGradient(0.82f);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.72f),
                new Keyframe(0.32f, 1f),
                new Keyframe(1f, 0.82f)));

        ConfigureRenderer(particleSystem, material, ParticleSystemRenderSpace.Local);
    }

    private static ParticleSystem.MinMaxGradient CreateGuidanceGradient(float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.76f, 0.18f), 0f),
                new GradientColorKey(new Color(1f, 0.98f, 0.74f), 0.45f),
                new GradientColorKey(new Color(1f, 0.72f, 0.12f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.22f),
                new GradientAlphaKey(peakAlpha * 0.8f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void ConfigureRenderer(
        ParticleSystem particleSystem,
        Material material,
        ParticleSystemRenderSpace alignment)
    {
        ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.alignment = alignment;
        particleRenderer.material = material;
        particleRenderer.sortMode = ParticleSystemSortMode.Distance;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
    }

    private static void ConfigureScene(GameObject buttonPrefab, GameObject routePrefab)
    {
        Scene inGameScene = EditorSceneManager.OpenScene(InGameScenePath, OpenSceneMode.Single);
        if (!inGameScene.IsValid())
            throw new InvalidOperationException($"Failed to open {InGameScenePath}.");

        GameUIView gameUIView = UnityEngine.Object.FindFirstObjectByType<GameUIView>(FindObjectsInactive.Include);
        MapGenerator mapGenerator = UnityEngine.Object.FindFirstObjectByType<MapGenerator>(FindObjectsInactive.Include);
        if (gameUIView == null || mapGenerator == null)
            throw new InvalidOperationException("Hint guidance scene references are missing.");

        SerializedObject serializedView = new SerializedObject(gameUIView);
        HintButtonParticleEffect buttonEffect = GetRequiredReference<HintButtonParticleEffect>(
            serializedView,
            "hintButtonParticleEffect");
        SerializedObject serializedButtonEffect = new SerializedObject(buttonEffect);
        serializedButtonEffect.FindProperty("particlePrefab").objectReferenceValue = buttonPrefab;
        serializedButtonEffect.FindProperty("particleScale").floatValue = 1f;
        serializedButtonEffect.FindProperty("screenOffset").vector2Value = Vector2.zero;
        serializedButtonEffect.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serializedMapGenerator = new SerializedObject(mapGenerator);
        serializedMapGenerator.FindProperty("hintEffectPrefab").objectReferenceValue = routePrefab;
        serializedMapGenerator.FindProperty("hintEffectScale").floatValue = 1f;
        serializedMapGenerator.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(buttonEffect);
        EditorUtility.SetDirty(mapGenerator);
        EditorSceneManager.MarkSceneDirty(inGameScene);
        EditorSceneManager.SaveScene(inGameScene);
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
