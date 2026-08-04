#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapData))]
public class MapEditor : Editor
{
    private const string DefaultStageFolder = "Assets/Data/Stages";

    private int selectedX = -1;
    private int selectedY = -1;
    private MapData sourceMap;

    public override void OnInspectorGUI()
    {
        MapData map = (MapData)target;

        EditorGUI.BeginChangeCheck();

        map.width = EditorGUILayout.IntField("Width", map.width);
        map.height = EditorGUILayout.IntField("Height", map.height);
        map.startMoveCount = EditorGUILayout.IntField("StartMoveCount", map.startMoveCount);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Grid Size"))
        {
            map.ResetGrid();
            selectedX = -1;
            selectedY = -1;
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
        }

        EditorGUILayout.Space();
        DrawMapTools(map);
        EditorGUILayout.Space();

        if (map.rows != null && map.rows.Length == map.height && map.width > 0 && map.height > 0)
        {
            DrawGrid(map);
            DrawSelectedTileInfo(map);
        }
        else
        {
            EditorGUILayout.HelpBox("Grid size and row data do not match. Use Apply Grid Size or load another MapData.", MessageType.Warning);
        }
    }

    private void DrawMapTools(MapData map)
    {
        DrawValidation(map);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Load / Save", EditorStyles.boldLabel);
        sourceMap = (MapData)EditorGUILayout.ObjectField("Load From MapData", sourceMap, typeof(MapData), false);

        EditorGUI.BeginDisabledGroup(sourceMap == null || sourceMap == map);
        if (GUILayout.Button("Load From Selected MapData"))
            LoadFromMap(map, sourceMap);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Save Current MapData Copy"))
            SaveCopy(map);
    }

    private void DrawValidation(MapData map)
    {
        if (TryValidateMap(map, out string validationMessage))
        {
            EditorGUILayout.HelpBox(validationMessage, MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
    }

    private void LoadFromMap(MapData targetMap, MapData sourceMapData)
    {
        if (targetMap == null || sourceMapData == null)
            return;

        Undo.RecordObject(targetMap, "Load MapData");
        CopyMapData(sourceMapData, targetMap);
        selectedX = -1;
        selectedY = -1;
        EditorUtility.SetDirty(targetMap);
        AssetDatabase.SaveAssetIfDirty(targetMap);
    }

    private void SaveCopy(MapData map)
    {
        if (map == null)
            return;

        if (!TryValidateMap(map, out string validationMessage))
        {
            bool shouldContinue = EditorUtility.DisplayDialog(
                "MapData Validation",
                $"{validationMessage}\n\nSave a copy anyway?",
                "Save",
                "Cancel");

            if (!shouldContinue)
                return;
        }

        string defaultFileName = $"{map.name}_Copy";
        string selectedPath = EditorUtility.SaveFilePanelInProject(
            "Save MapData Copy",
            defaultFileName,
            "asset",
            "Choose where to save the copied MapData.",
            DefaultStageFolder);

        if (string.IsNullOrWhiteSpace(selectedPath))
            return;

        MapData mapCopy = CreateInstance<MapData>();
        CopyMapData(map, mapCopy);
        AssetDatabase.CreateAsset(mapCopy, selectedPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = mapCopy;
    }

    private void DrawGrid(MapData map)
    {
        for (int y = map.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < map.width; x++)
            {
                if (map.rows[y] == null || map.rows[y].values == null) continue;

                SerializedTile tile = map.rows[y].values[x];
                bool isSelected = selectedX == x && selectedY == y;

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = isSelected ? Color.magenta : GetTileColor(tile.type);

                if (GUILayout.Button(GetTileLabel(tile), GUILayout.Width(50), GUILayout.Height(50)))
                {
                    if (isSelected)
                    {
                        tile.type = (TileType)(((int)tile.type + 1) % System.Enum.GetValues(typeof(TileType)).Length);
                        map.rows[y].values[x] = tile;
                        EditorUtility.SetDirty(map);
                        AssetDatabase.SaveAssetIfDirty(map);
                    }
                    else
                    {
                        selectedX = x;
                        selectedY = y;
                    }
                }

                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawSelectedTileInfo(MapData map)
    {
        if (selectedX < 0 || selectedY < 0) return;
        if (map.rows[selectedY] == null || map.rows[selectedY].values == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Selected Tile ({selectedX}, {selectedY})", EditorStyles.boldLabel);

        SerializedTile tile = map.rows[selectedY].values[selectedX];

        EditorGUI.BeginChangeCheck();

        tile.type = (TileType)EditorGUILayout.EnumPopup("Type", tile.type);

        if (tile.type == TileType.Move || tile.type == TileType.NumberObstacle)
            tile.value = EditorGUILayout.IntField("Value", tile.value);

        if (tile.type == TileType.Exit)
            tile.exitCondition = (ExitCondition)EditorGUILayout.EnumPopup("Exit Condition", tile.exitCondition);

        if (EditorGUI.EndChangeCheck())
        {
            map.rows[selectedY].values[selectedX] = tile;
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
        }
    }

    private string GetTileLabel(SerializedTile tile)
    {
        switch (tile.type)
        {
            case TileType.Empty:          return "Empty";
            case TileType.Start:          return "Start";
            case TileType.Exit:           return GetExitLabel(tile.exitCondition);
            case TileType.Move:           return $"M:{FormatMoveValue(tile.value)}";
            case TileType.NumberObstacle: return $"N:{tile.value}";
            case TileType.Wall:           return "Wall";
            default:                      return "?";
        }
    }

    private string FormatMoveValue(int value)
    {
        if (value > 0)
            return $"+{value}";

        return value.ToString();
    }

    private string GetExitLabel(ExitCondition condition)
    {
        switch (condition)
        {
            case ExitCondition.OddOnly:
                return "X:O";
            case ExitCondition.EvenOnly:
                return "X:V";
            default:
                return "Exit";
        }
    }

    private Color GetTileColor(TileType type)
    {
        switch (type)
        {
            case TileType.Empty:          return Color.white;
            case TileType.Start:          return Color.green;
            case TileType.Exit:           return Color.cyan;
            case TileType.Move:           return Color.yellow;
            case TileType.NumberObstacle: return Color.red;
            case TileType.Wall:           return Color.gray;
            default:                      return Color.white;
        }
    }

    private bool TryValidateMap(MapData map, out string validationMessage)
    {
        if (map == null)
        {
            validationMessage = "MapData is missing.";
            return false;
        }

        if (map.width <= 0 || map.height <= 0)
        {
            validationMessage = "Width and Height must be greater than 0.";
            return false;
        }

        if (map.rows == null || map.rows.Length != map.height)
        {
            validationMessage = "Row count does not match Height.";
            return false;
        }

        int startCount = 0;
        int exitCount = 0;

        for (int y = 0; y < map.height; y++)
        {
            if (map.rows[y] == null || map.rows[y].values == null || map.rows[y].values.Length != map.width)
            {
                validationMessage = $"Row {y} tile count does not match Width.";
                return false;
            }

            for (int x = 0; x < map.width; x++)
            {
                SerializedTile tile = map.rows[y].values[x];
                if (tile.type == TileType.Start)
                    startCount++;

                if (tile.type == TileType.Exit)
                    exitCount++;
            }
        }

        if (startCount <= 0)
        {
            validationMessage = "Start tile is missing.";
            return false;
        }

        if (exitCount <= 0)
        {
            validationMessage = "Exit tile is missing.";
            return false;
        }

        validationMessage = $"Map is valid. Start={startCount}, Exit={exitCount}";
        return true;
    }

    private void CopyMapData(MapData sourceMapData, MapData targetMap)
    {
        targetMap.width = sourceMapData.width;
        targetMap.height = sourceMapData.height;
        targetMap.startMoveCount = sourceMapData.startMoveCount;

        targetMap.rows = new Wrapper<SerializedTile>[targetMap.height];
        for (int y = 0; y < targetMap.height; y++)
        {
            targetMap.rows[y] = new Wrapper<SerializedTile>();
            targetMap.rows[y].values = new SerializedTile[targetMap.width];

            if (sourceMapData.rows == null ||
                y >= sourceMapData.rows.Length ||
                sourceMapData.rows[y] == null ||
                sourceMapData.rows[y].values == null)
                continue;

            int copyWidth = Mathf.Min(targetMap.width, sourceMapData.rows[y].values.Length);
            for (int x = 0; x < copyWidth; x++)
                targetMap.rows[y].values[x] = sourceMapData.rows[y].values[x];
        }
    }
}
#endif
