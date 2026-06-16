#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapData))]
public class MapEditor : Editor
{
    private int selectedX = -1;
    private int selectedY = -1;

    public override void OnInspectorGUI()
    {
        MapData map = (MapData)target;

        EditorGUI.BeginChangeCheck();

        map.width = EditorGUILayout.IntField("Width", map.width);
        map.height = EditorGUILayout.IntField("Height", map.height);
        map.startMoveCount = EditorGUILayout.IntField("StartMoveCount", map.startMoveCount);

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

        if (map.rows != null && map.rows.Length == map.height && map.width > 0 && map.height > 0)
        {
            DrawGrid(map);
            DrawSelectedTileInfo(map);
        }
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
}
#endif
