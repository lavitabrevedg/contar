#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class StageDataImporter : Editor
{
    private const string JsonPath = "Assets/Data/Stages/stages.json";
    private const string OutputPath = "Assets/Data/Stages";
    private const string DataFolder = "Data";
    private const string OutputFolder = "Stages";

    [MenuItem("contar/Import Stages from JSON")]
    public static void ImportStages()
    {
        string fullPath = Path.GetFullPath(JsonPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[StageDataImporter] stages.json not found: {fullPath}");
            return;
        }

        string json = File.ReadAllText(fullPath);
        StageCollection collection = JsonUtility.FromJson<StageCollection>(json);

        if (collection?.stages == null)
        {
            Debug.LogError("[StageDataImporter] stages.json 파싱 실패");
            return;
        }

        EnsureOutputFolder();

        int created = 0;
        int updated = 0;

        foreach (StageJson stageJson in collection.stages)
        {
            string assetPath = $"{OutputPath}/{stageJson.name}.asset";
            MapData map = AssetDatabase.LoadAssetAtPath<MapData>(assetPath);
            bool isNew = map == null;

            if (isNew)
                map = ScriptableObject.CreateInstance<MapData>();

            ApplyStageJson(map, stageJson);

            if (isNew)
            {
                AssetDatabase.CreateAsset(map, assetPath);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(map);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"{created}개 생성, {updated}개 업데이트 완료.";
        Debug.Log($"[StageDataImporter] {message}");

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("스테이지 임포트 완료", message, "확인");
    }

    private static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", DataFolder);

        if (!AssetDatabase.IsValidFolder(OutputPath))
            AssetDatabase.CreateFolder("Assets/Data", OutputFolder);
    }

    private static void ApplyStageJson(MapData map, StageJson stageJson)
    {
        map.width = stageJson.width;
        map.height = stageJson.height;
        map.startMoveCount = stageJson.startMoveCount;
        map.rows = new Wrapper<SerializedTile>[stageJson.height];

        for (int jsonRow = 0; jsonRow < stageJson.height; jsonRow++)
        {
            int mapRow = stageJson.height - 1 - jsonRow;
            map.rows[mapRow] = new Wrapper<SerializedTile>();
            map.rows[mapRow].values = new SerializedTile[stageJson.width];

            string[] cells = jsonRow < stageJson.rows.Length
                ? stageJson.rows[jsonRow].Split(',')
                : new string[0];

            for (int x = 0; x < stageJson.width; x++)
                map.rows[mapRow].values[x] = ParseTile(x < cells.Length ? cells[x].Trim() : "E");
        }
    }

    private static SerializedTile ParseTile(string token)
    {
        SerializedTile tile = default;
        if (string.IsNullOrEmpty(token) || token == "E")
            return tile;

        string[] parts = token.Split(':');
        string typeText = parts[0];

        switch (typeText)
        {
            case "S":
                tile.type = TileType.Start;
                break;
            case "X":
                tile.type = TileType.Exit;
                if (parts.Length > 1)
                {
                    if (parts[1] == "O") tile.exitCondition = ExitCondition.OddOnly;
                    else if (parts[1] == "V") tile.exitCondition = ExitCondition.EvenOnly;
                }
                break;
            case "M":
                tile.type = TileType.Move;
                if (parts.Length > 1 && int.TryParse(parts[1], out int moveValue))
                    tile.value = moveValue;
                break;
            case "N":
                tile.type = TileType.NumberObstacle;
                if (parts.Length > 1 && int.TryParse(parts[1], out int numberObstacleValue))
                    tile.value = numberObstacleValue;
                break;
            case "W":
                tile.type = TileType.Wall;
                break;
            default:
                Debug.LogWarning($"[StageDataImporter] 알 수 없는 타일 토큰: '{token}'");
                break;
        }

        return tile;
    }
}

[System.Serializable]
class StageCollection
{
    public StageJson[] stages;
}

[System.Serializable]
class StageJson
{
    public string name;
    public int width;
    public int height;
    public int startMoveCount;
    public string[] rows;
}
#endif
