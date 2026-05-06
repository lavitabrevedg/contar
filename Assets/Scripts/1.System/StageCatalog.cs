using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StageCatalog", menuName = "contar/Stage Catalog")]
public class StageCatalog : ScriptableObject
{
    [SerializeField] private MapData[] stages;

    public int StageCount => stages == null ? 0 : stages.Length;

    public bool TryGetStage(int index, out MapData mapData)
    {
        mapData = null;

        if (stages == null) return false;
        if (index < 0 || index >= stages.Length) return false;

        mapData = stages[index];
        return mapData != null;
    }

    public int IndexOf(MapData mapData)
    {
        if (stages == null || mapData == null)
            return -1;

        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] == mapData)
                return i;
        }

        return -1;
    }

    public void SetStages(MapData[] stageList)
    {
        stages = stageList == null ? Array.Empty<MapData>() : stageList;
    }
}
