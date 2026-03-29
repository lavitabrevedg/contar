using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;
[CustomEditor(typeof(MapData))]

public class MapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapData map = (MapData)target;

        map.width = EditorGUILayout.IntField("Width", map.width);
        map.height = EditorGUILayout.IntField("Height", map.height);
        map.startMoveCount = EditorGUILayout.IntField("StartMoveCount", map.startMoveCount);

        EditorGUILayout.Space();

        if(GUILayout.Button("Apply Grid Size"))
        {
            map.tiles = new SerializedTile[map.width * map.height];
        }

        EditorGUILayout.Space();
    }
}
