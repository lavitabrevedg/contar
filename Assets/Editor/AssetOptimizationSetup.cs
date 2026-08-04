using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

public static class AssetCleanupTool
{
    private const string MusicSourceSpriteName = "GUI_47";
    private const string SoundEffectSourceSpriteName = "GUI_48";
    private static readonly Regex GuidRegex = new Regex("guid: ([0-9a-f]{32})", RegexOptions.Compiled);

    [MenuItem("contar/Asset Cleanup/1. Migrate And Validate Before Deletion")]
    public static void MigrateAndValidateBeforeDeletion()
    {
        ExecuteWorkflow("PRE-DELETE", delegate
        {
            DeletePreDeleteFlag();
            MigrateSpritesAndCreateAtlases();
            ValidateProject(false);
            RunFunctionalValidation();
            BuildAndroidValidation("pre-delete");
            WritePreDeleteFlag();
        });
    }

    [MenuItem("contar/Asset Cleanup/2. Delete Approved Assets And Revalidate")]
    public static void DeleteApprovedAssetsAndRevalidate()
    {
        ExecuteWorkflow("POST-DELETE", delegate
        {
            RequirePreDeleteFlag();
            ValidateProject(false);
            DeleteApprovedAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateProject(true);
            RunFunctionalValidation();
            BuildAndroidValidation("post-delete");
            DeletePreDeleteFlag();
        });
    }

    [MenuItem("contar/Asset Cleanup/Validate Current State")]
    public static void ValidateCurrentState()
    {
        ExecuteWorkflow("VALIDATE", delegate
        {
            bool deletionCompleted = !AssetDatabase.IsValidFolder(AssetCleanupCatalog.LegacySourceFolder);
            ValidateProject(deletionCompleted);
            RunFunctionalValidation();
        });
    }

    [InitializeOnLoadMethod]
    private static void ProcessRequestedWorkflow()
    {
        string projectRoot = GetProjectRoot();
        string migrateRequestPath = Path.Combine(projectRoot, AssetCleanupCatalog.MigrateRequestPath);
        string deleteRequestPath = Path.Combine(projectRoot, AssetCleanupCatalog.DeleteRequestPath);

        if (File.Exists(migrateRequestPath))
        {
            File.Delete(migrateRequestPath);
            EditorApplication.delayCall += MigrateAndValidateBeforeDeletion;
            return;
        }

        if (File.Exists(deleteRequestPath))
        {
            File.Delete(deleteRequestPath);
            EditorApplication.delayCall += DeleteApprovedAssetsAndRevalidate;
        }
    }

    [DidReloadScripts]
    private static void ProcessRequestedWorkflowAfterReload()
    {
        EditorApplication.delayCall += ProcessRequestedWorkflow;
    }

    private static void ExecuteWorkflow(string phase, Action workflow)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before running asset cleanup.");

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        DateTime startedAt = DateTime.Now;
        AppendReport($"\n===== {phase} {startedAt:yyyy-MM-dd HH:mm:ss} =====");

        try
        {
            workflow();
            TimeSpan elapsed = DateTime.Now - startedAt;
            AppendReport($"{phase} PASSED in {elapsed.TotalSeconds:F1} seconds.");
            Debug.Log($"[AssetCleanup] {phase} passed in {elapsed.TotalSeconds:F1} seconds.");
        }
        catch (Exception exception)
        {
            DeletePreDeleteFlag();
            AppendReport($"{phase} FAILED: {exception}");
            Debug.LogException(exception);
            throw;
        }
    }

    private static void MigrateSpritesAndCreateAtlases()
    {
        EnsureFolder(AssetCleanupCatalog.CommonUiFolder);
        EnsureFolder(AssetCleanupCatalog.LobbyUiFolder);
        EnsureFolder(AssetCleanupCatalog.LobbyStandaloneFolder);
        EnsureFolder(AssetCleanupCatalog.InGameUiFolder);
        EnsureFolder(AssetCleanupCatalog.AtlasFolder);

        Dictionary<Sprite, Sprite> replacements = ExtractLegacySettingSprites();
        ReplacePrefabSpriteReferences(replacements);

        foreach (KeyValuePair<string, string> movePath in AssetCleanupCatalog.MovePaths)
            MoveAssetPreservingGuid(movePath.Key, movePath.Value);

        CreateOrUpdateAtlas(
            AssetCleanupCatalog.CommonUiAtlasPath,
            AssetCleanupCatalog.CommonUiSpritePaths,
            AssetCleanupCatalog.GetUiFormat());
        CreateOrUpdateAtlas(
            AssetCleanupCatalog.LobbyUiAtlasPath,
            AssetCleanupCatalog.LobbyUiSpritePaths,
            AssetCleanupCatalog.GetUiFormat());
        CreateOrUpdateAtlas(
            AssetCleanupCatalog.InGameUiAtlasPath,
            AssetCleanupCatalog.InGameUiSpritePaths,
            AssetCleanupCatalog.GetUiFormat());
        CreateOrUpdateAtlas(
            AssetCleanupCatalog.BoardAtlasPath,
            AssetCleanupCatalog.BoardSpritePaths,
            AssetCleanupCatalog.GetBoardFormat());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        PackAtlasesForAndroid();
        AppendReport("Sprite migration and atlas creation completed without deleting source assets.");
    }

    private static Dictionary<Sprite, Sprite> ExtractLegacySettingSprites()
    {
        Dictionary<Sprite, Sprite> replacements = new Dictionary<Sprite, Sprite>();

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(AssetCleanupCatalog.LegacyGuiPath) != null)
        {
            ExtractSubSprite(
                AssetCleanupCatalog.LegacyGuiPath,
                MusicSourceSpriteName,
                AssetCleanupCatalog.CommonUiFolder + "/MusicIcon.png",
                replacements);
            ExtractSubSprite(
                AssetCleanupCatalog.LegacyGuiPath,
                SoundEffectSourceSpriteName,
                AssetCleanupCatalog.CommonUiFolder + "/SoundEffectIcon.png",
                replacements);
        }

        Sprite sourceVibration = AssetDatabase.LoadAssetAtPath<Sprite>(AssetCleanupCatalog.LegacyVibrationPath);
        string vibrationOutputPath = AssetCleanupCatalog.CommonUiFolder + "/VibrationIcon.png";
        Sprite replacementVibration = AssetDatabase.LoadAssetAtPath<Sprite>(vibrationOutputPath);

        if (sourceVibration != null && replacementVibration == null)
        {
            string sourceAbsolutePath = ToAbsolutePath(AssetCleanupCatalog.LegacyVibrationPath);
            string outputAbsolutePath = ToAbsolutePath(vibrationOutputPath);
            File.Copy(sourceAbsolutePath, outputAbsolutePath, false);
            AssetDatabase.ImportAsset(vibrationOutputPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureSingleSprite(vibrationOutputPath, sourceVibration.pixelsPerUnit, 256);
            replacementVibration = AssetDatabase.LoadAssetAtPath<Sprite>(vibrationOutputPath);
        }

        if (sourceVibration != null && replacementVibration != null)
            replacements[sourceVibration] = replacementVibration;

        return replacements;
    }

    private static void ExtractSubSprite(
        string sourceTexturePath,
        string sourceSpriteName,
        string outputPath,
        Dictionary<Sprite, Sprite> replacements)
    {
        Sprite sourceSprite = FindSprite(sourceTexturePath, sourceSpriteName);
        if (sourceSprite == null)
            throw new InvalidOperationException($"Missing source sprite: {sourceSpriteName}");

        Sprite replacementSprite = AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
        if (replacementSprite == null)
        {
            TextureImporter sourceImporter = AssetImporter.GetAtPath(sourceTexturePath) as TextureImporter;
            if (sourceImporter == null)
                throw new InvalidOperationException($"Missing texture importer: {sourceTexturePath}");

            bool wasReadable = sourceImporter.isReadable;
            sourceImporter.isReadable = true;
            sourceImporter.SaveAndReimport();

            try
            {
                Rect sourceRect = sourceSprite.textureRect;
                int width = Mathf.RoundToInt(sourceRect.width);
                int height = Mathf.RoundToInt(sourceRect.height);
                Texture2D croppedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] pixels = sourceSprite.texture.GetPixels(
                    Mathf.RoundToInt(sourceRect.x),
                    Mathf.RoundToInt(sourceRect.y),
                    width,
                    height);
                croppedTexture.SetPixels(pixels);
                croppedTexture.Apply(false, false);
                File.WriteAllBytes(ToAbsolutePath(outputPath), croppedTexture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(croppedTexture);
            }
            finally
            {
                sourceImporter.isReadable = wasReadable;
                sourceImporter.SaveAndReimport();
            }

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureSingleSprite(outputPath, sourceSprite.pixelsPerUnit, 256);
            replacementSprite = AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
        }

        if (replacementSprite == null)
            throw new InvalidOperationException($"Could not create replacement sprite: {outputPath}");

        replacements[sourceSprite] = replacementSprite;
    }

    private static Sprite FindSprite(string texturePath, string spriteName)
    {
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        for (int assetIndex = 0; assetIndex < subAssets.Length; assetIndex++)
        {
            Sprite sprite = subAssets[assetIndex] as Sprite;
            if (sprite != null && sprite.name == spriteName)
                return sprite;
        }

        return null;
    }

    private static void ConfigureSingleSprite(string texturePath, float pixelsPerUnit, int androidMaxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Missing texture importer: {texturePath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;

        TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings("Android");
        platformSettings.overridden = true;
        platformSettings.maxTextureSize = androidMaxSize;
        platformSettings.format = AssetCleanupCatalog.GetUiFormat();
        platformSettings.compressionQuality = 50;
        platformSettings.crunchedCompression = false;
        importer.SetPlatformTextureSettings(platformSettings);
        importer.SaveAndReimport();
    }

    private static void ReplacePrefabSpriteReferences(Dictionary<Sprite, Sprite> replacements)
    {
        if (replacements.Count == 0)
            return;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(AssetCleanupCatalog.SettingsPrefabPath);
        bool changed = false;

        try
        {
            Image[] images = prefabRoot.GetComponentsInChildren<Image>(true);
            for (int imageIndex = 0; imageIndex < images.Length; imageIndex++)
            {
                Sprite replacementSprite;
                if (!replacements.TryGetValue(images[imageIndex].sprite, out replacementSprite))
                    continue;

                images[imageIndex].sprite = replacementSprite;
                EditorUtility.SetDirty(images[imageIndex]);
                changed = true;
            }

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, AssetCleanupCatalog.SettingsPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        if (!changed)
            AppendReport("SettingsPanel already uses the extracted setting sprites.");
    }

    private static void MoveAssetPreservingGuid(string sourcePath, string destinationPath)
    {
        UnityEngine.Object destinationAsset = AssetDatabase.LoadMainAssetAtPath(destinationPath);
        UnityEngine.Object sourceAsset = AssetDatabase.LoadMainAssetAtPath(sourcePath);

        if (sourceAsset == null && destinationAsset != null)
            return;

        if (sourceAsset == null)
            throw new InvalidOperationException($"Missing source asset: {sourcePath}");

        if (destinationAsset != null)
            throw new InvalidOperationException($"Destination already exists: {destinationPath}");

        string moveError = AssetDatabase.MoveAsset(sourcePath, destinationPath);
        if (!string.IsNullOrEmpty(moveError))
            throw new InvalidOperationException($"Could not move {sourcePath} to {destinationPath}: {moveError}");
    }

    private static void CreateOrUpdateAtlas(string atlasPath, string[] spritePaths, TextureImporterFormat format)
    {
        SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (spriteAtlas == null)
        {
            spriteAtlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(spriteAtlas, atlasPath);
        }

        UnityEngine.Object[] existingPackables = spriteAtlas.GetPackables();
        if (existingPackables.Length > 0)
            SpriteAtlasExtensions.Remove(spriteAtlas, existingPackables);

        List<UnityEngine.Object> sprites = new List<UnityEngine.Object>();
        for (int spriteIndex = 0; spriteIndex < spritePaths.Length; spriteIndex++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[spriteIndex]);
            if (sprite == null)
                throw new InvalidOperationException($"Missing atlas sprite: {spritePaths[spriteIndex]}");

            sprites.Add(sprite);
        }

        SpriteAtlasExtensions.Add(spriteAtlas, sprites.ToArray());
        spriteAtlas.SetIncludeInBuild(true);

        SpriteAtlasPackingSettings packingSettings = spriteAtlas.GetPackingSettings();
        packingSettings.enableRotation = false;
        packingSettings.enableTightPacking = false;
        packingSettings.padding = 4;
        spriteAtlas.SetPackingSettings(packingSettings);

        SpriteAtlasTextureSettings textureSettings = spriteAtlas.GetTextureSettings();
        textureSettings.readable = false;
        textureSettings.generateMipMaps = false;
        spriteAtlas.SetTextureSettings(textureSettings);

        TextureImporterPlatformSettings platformSettings = spriteAtlas.GetPlatformSettings("Android");
        platformSettings.overridden = true;
        platformSettings.maxTextureSize = 1024;
        platformSettings.format = format;
        platformSettings.compressionQuality = 50;
        platformSettings.crunchedCompression = false;
        spriteAtlas.SetPlatformSettings(platformSettings);
        EditorUtility.SetDirty(spriteAtlas);
    }

    private static void PackAtlasesForAndroid()
    {
        List<SpriteAtlas> atlases = new List<SpriteAtlas>();
        for (int atlasIndex = 0; atlasIndex < AssetCleanupCatalog.AtlasPaths.Length; atlasIndex++)
        {
            SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetCleanupCatalog.AtlasPaths[atlasIndex]);
            if (spriteAtlas == null)
                throw new InvalidOperationException($"Missing atlas: {AssetCleanupCatalog.AtlasPaths[atlasIndex]}");

            atlases.Add(spriteAtlas);
        }

        SpriteAtlasUtility.PackAtlases(atlases.ToArray(), BuildTarget.Android, false);
    }

    private static void ValidateProject(bool deletionCompleted)
    {
        ValidateRequiredAssets();
        ValidateAtlases();
        ValidateCandidateReferences();
        ValidateSerializedGuids();
        ValidateMissingScripts();

        if (deletionCompleted)
        {
            if (AssetDatabase.IsValidFolder(AssetCleanupCatalog.LegacySourceFolder))
                throw new InvalidOperationException("Legacy 2D Casual UI folder still exists after deletion.");

            for (int fontIndex = 0; fontIndex < AssetCleanupCatalog.FontDeletionPaths.Length; fontIndex++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(AssetCleanupCatalog.FontDeletionPaths[fontIndex]) != null)
                    throw new InvalidOperationException($"Deleted font still exists: {AssetCleanupCatalog.FontDeletionPaths[fontIndex]}");
            }
        }

        AppendAssetInventory(deletionCompleted);
    }

    private static void ValidateRequiredAssets()
    {
        ValidateSpritePaths(AssetCleanupCatalog.CommonUiSpritePaths);
        ValidateSpritePaths(AssetCleanupCatalog.LobbyUiSpritePaths);
        ValidateSpritePaths(AssetCleanupCatalog.InGameUiSpritePaths);
        ValidateSpritePaths(AssetCleanupCatalog.BoardSpritePaths);

        for (int pathIndex = 0; pathIndex < AssetCleanupCatalog.RequiredStandalonePaths.Length; pathIndex++)
        {
            if (AssetDatabase.LoadMainAssetAtPath(AssetCleanupCatalog.RequiredStandalonePaths[pathIndex]) == null)
                throw new InvalidOperationException($"Missing required standalone asset: {AssetCleanupCatalog.RequiredStandalonePaths[pathIndex]}");
        }
    }

    private static void ValidateSpritePaths(string[] spritePaths)
    {
        for (int pathIndex = 0; pathIndex < spritePaths.Length; pathIndex++)
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[pathIndex]) == null)
                throw new InvalidOperationException($"Missing required sprite: {spritePaths[pathIndex]}");
        }
    }

    private static void ValidateAtlases()
    {
        Dictionary<string, string> spriteOwners = new Dictionary<string, string>();
        ValidateAtlas(AssetCleanupCatalog.CommonUiAtlasPath, AssetCleanupCatalog.CommonUiSpritePaths, spriteOwners);
        ValidateAtlas(AssetCleanupCatalog.LobbyUiAtlasPath, AssetCleanupCatalog.LobbyUiSpritePaths, spriteOwners);
        ValidateAtlas(AssetCleanupCatalog.InGameUiAtlasPath, AssetCleanupCatalog.InGameUiSpritePaths, spriteOwners);
        ValidateAtlas(AssetCleanupCatalog.BoardAtlasPath, AssetCleanupCatalog.BoardSpritePaths, spriteOwners);
        PackAtlasesForAndroid();
    }

    private static void ValidateAtlas(
        string atlasPath,
        string[] expectedSpritePaths,
        Dictionary<string, string> spriteOwners)
    {
        SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (spriteAtlas == null)
            throw new InvalidOperationException($"Missing atlas: {atlasPath}");

        UnityEngine.Object[] packables = spriteAtlas.GetPackables();
        if (packables.Length != expectedSpritePaths.Length)
            throw new InvalidOperationException(
                $"Atlas packable count mismatch: {atlasPath}, expected={expectedSpritePaths.Length}, actual={packables.Length}");

        HashSet<string> actualPaths = new HashSet<string>();
        for (int packableIndex = 0; packableIndex < packables.Length; packableIndex++)
        {
            string spritePath = AssetDatabase.GetAssetPath(packables[packableIndex]);
            actualPaths.Add(spritePath);

            string ownerAtlas;
            if (spriteOwners.TryGetValue(spritePath, out ownerAtlas))
                throw new InvalidOperationException($"Sprite is packed by both {ownerAtlas} and {atlasPath}: {spritePath}");

            spriteOwners[spritePath] = atlasPath;
        }

        for (int expectedIndex = 0; expectedIndex < expectedSpritePaths.Length; expectedIndex++)
        {
            if (!actualPaths.Contains(expectedSpritePaths[expectedIndex]))
                throw new InvalidOperationException($"Atlas is missing {expectedSpritePaths[expectedIndex]}: {atlasPath}");
        }
    }

    private static void ValidateCandidateReferences()
    {
        List<string> candidatePaths = GetExistingDeletionCandidates();
        Dictionary<string, string> candidateGuids = new Dictionary<string, string>();

        for (int candidateIndex = 0; candidateIndex < candidatePaths.Count; candidateIndex++)
        {
            string candidateGuid = AssetDatabase.AssetPathToGUID(candidatePaths[candidateIndex]);
            if (!string.IsNullOrEmpty(candidateGuid))
                candidateGuids[candidateGuid] = candidatePaths[candidateIndex];
        }

        if (candidateGuids.Count == 0)
            return;

        string[] serializedPaths = GetSerializedProjectPaths();
        List<string> references = new List<string>();

        for (int serializedIndex = 0; serializedIndex < serializedPaths.Length; serializedIndex++)
        {
            string serializedPath = serializedPaths[serializedIndex];
            if (IsDeletionCandidate(serializedPath))
                continue;

            string absolutePath = ToAbsolutePath(serializedPath);
            if (!File.Exists(absolutePath))
                continue;

            string contents = File.ReadAllText(absolutePath);
            foreach (KeyValuePair<string, string> candidateGuid in candidateGuids)
            {
                if (contents.Contains(candidateGuid.Key))
                    references.Add($"{candidateGuid.Value} <- {serializedPath}");
            }
        }

        if (references.Count > 0)
            throw new InvalidOperationException("Deletion candidates are still referenced:\n" + string.Join("\n", references));
    }

    private static void ValidateSerializedGuids()
    {
        string[] serializedPaths = GetSerializedProjectPaths();
        List<string> missingGuids = new List<string>();

        for (int pathIndex = 0; pathIndex < serializedPaths.Length; pathIndex++)
        {
            if (!IsOwnedSerializedAsset(serializedPaths[pathIndex]))
                continue;

            string absolutePath = ToAbsolutePath(serializedPaths[pathIndex]);
            if (!File.Exists(absolutePath))
                continue;

            string contents = File.ReadAllText(absolutePath);
            MatchCollection matches = GuidRegex.Matches(contents);
            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                string guid = matches[matchIndex].Groups[1].Value;
                if (guid.StartsWith("0000000000000000", StringComparison.Ordinal))
                    continue;

                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    missingGuids.Add($"{serializedPaths[pathIndex]} -> {guid}");
            }
        }

        if (missingGuids.Count > 0)
            throw new InvalidOperationException("Serialized assets contain unresolved GUIDs:\n" + string.Join("\n", missingGuids));
    }

    private static bool IsOwnedSerializedAsset(string assetPath)
    {
        string[] ownedRoots =
        {
            "Assets/Scenes/",
            "Assets/PreFab/",
            "Assets/Data/",
            "Assets/Resources/",
            "Assets/Localization/",
            "Assets/Art/",
            "Assets/AddressableAssetsData/"
        };

        for (int rootIndex = 0; rootIndex < ownedRoots.Length; rootIndex++)
        {
            if (assetPath.StartsWith(ownedRoots[rootIndex], StringComparison.Ordinal))
                return true;
        }

        return assetPath == "Assets/English (en).asset" || assetPath == "Assets/Korean (ko).asset";
    }

    private static void ValidateMissingScripts()
    {
        for (int sceneIndex = 0; sceneIndex < AssetCleanupCatalog.ScenePaths.Length; sceneIndex++)
        {
            Scene scene = EditorSceneManager.OpenScene(AssetCleanupCatalog.ScenePaths[sceneIndex], OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(roots[rootIndex]);
                if (missingCount > 0)
                    throw new InvalidOperationException($"Missing scripts in {AssetCleanupCatalog.ScenePaths[sceneIndex]}: {missingCount}");
            }
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PreFab" });
        for (int prefabIndex = 0; prefabIndex < prefabGuids.Length; prefabIndex++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[prefabIndex]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
            if (missingCount > 0)
                throw new InvalidOperationException($"Missing scripts in {prefabPath}: {missingCount}");
        }
    }

    private static void RunFunctionalValidation()
    {
        ValidateAllStagesOrThrow();
        ProgressFeatureSmokeTest.Run();
        Debug.Log("[AssetCleanup] Stage solver and progress smoke tests passed.");
    }

    private static void ValidateAllStagesOrThrow()
    {
        string[] stageGuids = AssetDatabase.FindAssets("t:MapData", new[] { "Assets/Data/Stages" });
        if (stageGuids.Length != 13)
            throw new InvalidOperationException($"Expected 13 stages, found {stageGuids.Length}.");

        for (int stageIndex = 0; stageIndex < stageGuids.Length; stageIndex++)
        {
            string stagePath = AssetDatabase.GUIDToAssetPath(stageGuids[stageIndex]);
            MapData mapData = AssetDatabase.LoadAssetAtPath<MapData>(stagePath);
            PuzzleSolveResult solveResult = PuzzleSolver.SolveInitial(mapData);

            if (solveResult.HasStructureError)
                throw new InvalidOperationException($"Stage structure error in {stagePath}: {solveResult.ErrorMessage}");

            if (!solveResult.IsSolvable)
                throw new InvalidOperationException($"Unsolvable stage: {stagePath}");
        }
    }

    private static void BuildAndroidValidation(string label)
    {
        List<string> scenePaths = new List<string>();
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        for (int sceneIndex = 0; sceneIndex < buildScenes.Length; sceneIndex++)
        {
            if (buildScenes[sceneIndex].enabled)
                scenePaths.Add(buildScenes[sceneIndex].path);
        }

        if (scenePaths.Count == 0)
            throw new InvalidOperationException("No enabled build scenes were found.");

        string outputFolder = Path.Combine(GetProjectRoot(), "Temp", "AssetCleanupValidation");
        Directory.CreateDirectory(outputFolder);
        string outputPath = Path.Combine(outputFolder, $"Contar-{label}.apk");

        bool previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;

        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenePaths.ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | BuildOptions.CompressWithLz4
            };

            BuildReport buildReport = BuildPipeline.BuildPlayer(buildOptions);
            if (buildReport.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Android validation build failed: {buildReport.summary.result}, errors={buildReport.summary.totalErrors}");

            AppendReport(
                $"Android {label} build passed: {buildReport.summary.totalSize / (1024f * 1024f):F2} MB, " +
                $"warnings={buildReport.summary.totalWarnings}.");
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
        }
    }

    private static void DeleteApprovedAssets()
    {
        List<string> candidatePaths = GetExistingDeletionCandidates();
        AppendReport($"Deleting {candidatePaths.Count} approved assets/folders after successful pre-delete validation.");

        if (AssetDatabase.IsValidFolder(AssetCleanupCatalog.LegacySourceFolder))
        {
            if (!AssetDatabase.DeleteAsset(AssetCleanupCatalog.LegacySourceFolder))
                throw new InvalidOperationException($"Could not delete {AssetCleanupCatalog.LegacySourceFolder}");
        }

        DeleteAssetIfPresent(AssetCleanupCatalog.OldAtlasPath);

        for (int fontIndex = 0; fontIndex < AssetCleanupCatalog.FontDeletionPaths.Length; fontIndex++)
            DeleteAssetIfPresent(AssetCleanupCatalog.FontDeletionPaths[fontIndex]);

        DeleteEmptyFolder("Assets/Art/UI/ContarUI/Sprites");
        DeleteEmptyFolder("Assets/Art/UI/ContarUI");
        DeleteEmptyFolder("Assets/Art/UI/Controls");
    }

    private static void DeleteAssetIfPresent(string assetPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            return;

        if (!AssetDatabase.DeleteAsset(assetPath))
            throw new InvalidOperationException($"Could not delete approved asset: {assetPath}");
    }

    private static void DeleteEmptyFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] remainingGuids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
        if (remainingGuids.Length == 0)
            AssetDatabase.DeleteAsset(folderPath);
    }

    private static List<string> GetExistingDeletionCandidates()
    {
        List<string> candidatePaths = new List<string>();

        if (AssetDatabase.IsValidFolder(AssetCleanupCatalog.LegacySourceFolder))
        {
            string[] legacyGuids = AssetDatabase.FindAssets(string.Empty, new[] { AssetCleanupCatalog.LegacySourceFolder });
            for (int guidIndex = 0; guidIndex < legacyGuids.Length; guidIndex++)
            {
                string legacyPath = AssetDatabase.GUIDToAssetPath(legacyGuids[guidIndex]);
                if (!AssetDatabase.IsValidFolder(legacyPath))
                    candidatePaths.Add(legacyPath);
            }
        }

        if (AssetDatabase.LoadMainAssetAtPath(AssetCleanupCatalog.OldAtlasPath) != null)
            candidatePaths.Add(AssetCleanupCatalog.OldAtlasPath);

        for (int fontIndex = 0; fontIndex < AssetCleanupCatalog.FontDeletionPaths.Length; fontIndex++)
        {
            if (AssetDatabase.LoadMainAssetAtPath(AssetCleanupCatalog.FontDeletionPaths[fontIndex]) != null)
                candidatePaths.Add(AssetCleanupCatalog.FontDeletionPaths[fontIndex]);
        }

        return candidatePaths;
    }

    private static bool IsDeletionCandidate(string assetPath)
    {
        if (assetPath.StartsWith(AssetCleanupCatalog.LegacySourceFolder + "/", StringComparison.Ordinal))
            return true;

        if (assetPath == AssetCleanupCatalog.OldAtlasPath)
            return true;

        for (int fontIndex = 0; fontIndex < AssetCleanupCatalog.FontDeletionPaths.Length; fontIndex++)
        {
            if (assetPath == AssetCleanupCatalog.FontDeletionPaths[fontIndex])
                return true;
        }

        return false;
    }

    private static string[] GetSerializedProjectPaths()
    {
        string[] assetGuids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" });
        List<string> serializedPaths = new List<string>();
        HashSet<string> supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".unity", ".prefab", ".asset", ".mat", ".spriteatlas", ".controller", ".anim", ".overrideController",
            ".playable", ".renderTexture", ".inputactions"
        };

        for (int guidIndex = 0; guidIndex < assetGuids.Length; guidIndex++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[guidIndex]);
            string extension = Path.GetExtension(assetPath);
            if (supportedExtensions.Contains(extension))
                serializedPaths.Add(assetPath);
        }

        return serializedPaths.ToArray();
    }

    private static void AppendAssetInventory(bool deletionCompleted)
    {
        List<string> candidatePaths = GetExistingDeletionCandidates();
        StringBuilder inventory = new StringBuilder();
        inventory.AppendLine($"Validation state: deletionCompleted={deletionCompleted}");
        inventory.AppendLine($"Remaining deletion candidates: {candidatePaths.Count}");

        for (int candidateIndex = 0; candidateIndex < candidatePaths.Count; candidateIndex++)
        {
            string absolutePath = ToAbsolutePath(candidatePaths[candidateIndex]);
            if (!File.Exists(absolutePath))
                continue;

            FileInfo fileInfo = new FileInfo(absolutePath);
            inventory.AppendLine(
                $"  {candidatePaths[candidateIndex]} | {fileInfo.Length / 1024f:F1} KB | SHA256={ComputeSha256(absolutePath)}");
        }

        AppendReport(inventory.ToString().TrimEnd());
    }

    private static string ComputeSha256(string filePath)
    {
        using (SHA256 sha256 = SHA256.Create())
        using (FileStream fileStream = File.OpenRead(filePath))
        {
            byte[] hash = sha256.ComputeHash(fileStream);
            StringBuilder hashText = new StringBuilder(hash.Length * 2);
            for (int byteIndex = 0; byteIndex < hash.Length; byteIndex++)
                hashText.Append(hash[byteIndex].ToString("x2"));

            return hashText.ToString();
        }
    }

    private static void WritePreDeleteFlag()
    {
        string flagPath = Path.Combine(GetProjectRoot(), AssetCleanupCatalog.PreDeleteFlagPath);
        File.WriteAllText(flagPath, DateTime.UtcNow.ToString("O"));
        AppendReport("Pre-delete gate opened after all validation steps passed.");
    }

    private static void RequirePreDeleteFlag()
    {
        string flagPath = Path.Combine(GetProjectRoot(), AssetCleanupCatalog.PreDeleteFlagPath);
        if (!File.Exists(flagPath))
            throw new InvalidOperationException("Pre-delete validation has not passed. Run step 1 first.");
    }

    private static void DeletePreDeleteFlag()
    {
        string flagPath = Path.Combine(GetProjectRoot(), AssetCleanupCatalog.PreDeleteFlagPath);
        if (File.Exists(flagPath))
            File.Delete(flagPath);
    }

    private static void AppendReport(string message)
    {
        string reportPath = Path.Combine(GetProjectRoot(), AssetCleanupCatalog.ReportPath);
        string reportFolder = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(reportFolder))
            Directory.CreateDirectory(reportFolder);

        File.AppendAllText(reportPath, message + Environment.NewLine, Encoding.UTF8);
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int partIndex = 1; partIndex < parts.Length; partIndex++)
        {
            string nextPath = currentPath + "/" + parts[partIndex];
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[partIndex]);

            currentPath = nextPath;
        }
    }

    private static string ToAbsolutePath(string assetPath)
    {
        return Path.Combine(GetProjectRoot(), assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string GetProjectRoot()
    {
        DirectoryInfo assetsDirectory = Directory.GetParent(Application.dataPath);
        if (assetsDirectory == null)
            throw new InvalidOperationException("Could not resolve project root.");

        return assetsDirectory.FullName;
    }
}
