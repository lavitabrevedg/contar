using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ProgressFeatureSetup
{
    private const string StagesFolder = "Assets/Scripts/2.Data/Stages";
    private const string ResourcesFolder = "Assets/Resources";
    private const string CatalogPath = "Assets/Resources/StageCatalog.asset";

    public static void SyncStageCatalog()
    {
        EnsureResourcesFolder();

        StageCatalog catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<StageCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        MapData[] stages = LoadStages();
        catalog.SetStages(stages);

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ProgressFeatureSetup] StageCatalog synced. count={stages.Length}");
    }

    [MenuItem("contar/Sync Stage Catalog")]
    private static void SyncStageCatalogFromMenu()
    {
        SyncStageCatalog();
    }

    private static void EnsureResourcesFolder()
    {
        if (AssetDatabase.IsValidFolder(ResourcesFolder))
            return;

        AssetDatabase.CreateFolder("Assets", "Resources");
    }

    private static MapData[] LoadStages()
    {
        string[] guids = AssetDatabase.FindAssets("t:MapData", new[] { StagesFolder });
        List<MapData> stages = new List<MapData>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            MapData mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (mapData == null) continue;
            if (!mapData.name.StartsWith("Stage_", StringComparison.OrdinalIgnoreCase)) continue;

            stages.Add(mapData);
        }

        stages.Sort(CompareStageNames);
        return stages.ToArray();
    }

    private static int CompareStageNames(MapData left, MapData right)
    {
        return string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
    }
}
