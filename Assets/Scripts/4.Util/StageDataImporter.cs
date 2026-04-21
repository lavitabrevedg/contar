using UnityEngine;
using UnityEditor;
using System.IO;

public class StageDataImporter : Editor
{
    private const string JsonPath    = "Assets/Scripts/2.Data/Stages/stages.json";
    private const string OutputPath  = "Assets/Scripts/2.Data/Stages";
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

        if (!AssetDatabase.IsValidFolder(OutputPath))
            AssetDatabase.CreateFolder("Assets/Scripts/2.Data", OutputFolder);

        int created = 0, updated = 0;

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

        string msg = $"{created}개 생성, {updated}개 업데이트 완료.";
        Debug.Log($"[StageDataImporter] {msg}");
        EditorUtility.DisplayDialog("스테이지 임포트 완료", msg, "확인");
    }

    private static void ApplyStageJson(MapData map, StageJson stageJson)
    {
        map.width          = stageJson.width;
        map.height         = stageJson.height;
        map.startMoveCount = stageJson.startMoveCount;
        map.rows           = new Wrapper<SerializedTile>[stageJson.height];

        for (int jsonRow = 0; jsonRow < stageJson.height; jsonRow++)
        {
            // JSON rows[0]이 맵 상단 → MapData rows[height-1]에 대응
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

    // 타일 토큰 형식: E / S / W / X / X:O / X:V / M:값 / N:값
    private static SerializedTile ParseTile(string token)
    {
        SerializedTile tile = default;
        if (string.IsNullOrEmpty(token) || token == "E")
            return tile;

        string[] parts   = token.Split(':');
        string   typeStr = parts[0];

        switch (typeStr)
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
                if (parts.Length > 1 && int.TryParse(parts[1], out int mv))
                    tile.value = mv;
                break;
            case "N":
                tile.type = TileType.NumberObstacle;
                if (parts.Length > 1 && int.TryParse(parts[1], out int nv))
                    tile.value = nv;
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
    public string   name;
    public int      width;
    public int      height;
    public int      startMoveCount;
    public string[] rows;
}
