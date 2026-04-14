using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapData))]
public class MapEditor : Editor
{
    private int _selectedX = -1;
    private int _selectedY = -1;

    public override void OnInspectorGUI()
    {
        MapData map = (MapData)target;

        EditorGUI.BeginChangeCheck();

        map.width = EditorGUILayout.IntField("Width", map.width);
        map.height = EditorGUILayout.IntField("Height", map.height);
        map.startMoveCount = EditorGUILayout.IntField("StartMoveCount", map.startMoveCount);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(map);

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Grid Size"))
        {
            map.ResetGrid();
            _selectedX = -1;
            _selectedY = -1;
            EditorUtility.SetDirty(map);
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
                bool isSelected = _selectedX == x && _selectedY == y;

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = isSelected ? Color.white : GetTileColor(tile.type);

                if (GUILayout.Button(GetTileLabel(tile.type), GUILayout.Width(50), GUILayout.Height(50)))
                {
                    if (isSelected)
                    {
                        // 이미 선택된 타일 클릭 시 타입 순환
                        tile.type = (TileType)(((int)tile.type + 1) % System.Enum.GetValues(typeof(TileType)).Length);
                        map.rows[y].values[x] = tile;
                        EditorUtility.SetDirty(map);
                    }
                    else
                    {
                        _selectedX = x;
                        _selectedY = y;
                    }
                }

                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawSelectedTileInfo(MapData map)
    {
        if (_selectedX < 0 || _selectedY < 0) return;
        if (map.rows[_selectedY] == null || map.rows[_selectedY].values == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Selected Tile ({_selectedX}, {_selectedY})", EditorStyles.boldLabel);

        SerializedTile tile = map.rows[_selectedY].values[_selectedX];

        EditorGUI.BeginChangeCheck();

        tile.type = (TileType)EditorGUILayout.EnumPopup("Type", tile.type);

        if (tile.type == TileType.Move || tile.type == TileType.NumberObstacle)
            tile.value = EditorGUILayout.IntField("Value", tile.value);

        if (tile.type == TileType.Exit)
            tile.exitCondition = (ExitCondition)EditorGUILayout.EnumPopup("Exit Condition", tile.exitCondition);

        if (EditorGUI.EndChangeCheck())
        {
            map.rows[_selectedY].values[_selectedX] = tile;
            EditorUtility.SetDirty(map);
        }
    }

    private string GetTileLabel(TileType type)
    {
        switch (type)
        {
            case TileType.Empty:          return "Empty";
            case TileType.Start:          return "Start";
            case TileType.Exit:           return "Exit";
            case TileType.Move:           return "Move";
            case TileType.NumberObstacle: return "Num";
            case TileType.Wall:           return "Wall";
            default:                      return "?";
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
