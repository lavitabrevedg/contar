#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class StageSolver
{
    private const string StagesFolder = "Assets/Data/Stages";

    private struct StageAssetInfo
    {
        public string name;
        public int number;
        public MapData map;
    }

    [MenuItem("contar/Validate All Stages")]
    public static void ValidateAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:MapData", new[] { StagesFolder });
        List<StageAssetInfo> stageAssets = new List<StageAssetInfo>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string stageName = Path.GetFileNameWithoutExtension(path);
            if (!stageName.StartsWith("Stage_", StringComparison.Ordinal))
                continue;

            int stageNumber;
            TryParseStageNumber(stageName, out stageNumber);
            stageAssets.Add(new StageAssetInfo
            {
                name = stageName,
                number = stageNumber,
                map = AssetDatabase.LoadAssetAtPath<MapData>(path)
            });
        }

        stageAssets.Sort(CompareStageAssetInfo);

        int okCount = 0;
        int failCount = 0;
        int errorCount = 0;
        List<string> stageLines = new List<string>();

        foreach (StageAssetInfo stageAssetInfo in stageAssets)
        {
            PuzzleSolveResult solveResult = PuzzleSolver.SolveInitial(stageAssetInfo.map);
            if (solveResult.HasStructureError)
            {
                errorCount++;
                stageLines.Add($"{stageAssetInfo.name}: ERROR, {solveResult.ErrorMessage}");
            }
            else if (solveResult.IsSolvable)
            {
                okCount++;
                stageLines.Add(
                    $"{stageAssetInfo.name}: OK, inputs={solveResult.Route.Count}, " +
                    $"remaining={solveResult.RemainingMoves}, states={solveResult.StatesExplored}");
            }
            else
            {
                failCount++;
                stageLines.Add($"{stageAssetInfo.name}: FAIL, states={solveResult.StatesExplored}");
            }
        }

        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine("===== Stage Validation Result =====");
        logBuilder.AppendLine(
            $"Total: {stageAssets.Count} | OK: {okCount} | FAIL: {failCount} | ERROR: {errorCount}");
        logBuilder.AppendLine();
        logBuilder.AppendLine("[All Stages]");

        foreach (string stageLine in stageLines)
            logBuilder.AppendLine(stageLine);

        Debug.Log(logBuilder.ToString());
    }

    private static bool TryParseStageNumber(string stageName, out int stageNumber)
    {
        stageNumber = int.MaxValue;
        string numberText = stageName.Substring("Stage_".Length);
        int parsedStageNumber;
        if (!int.TryParse(numberText, out parsedStageNumber))
            return false;

        stageNumber = parsedStageNumber;
        return true;
    }

    private static int CompareStageAssetInfo(StageAssetInfo leftStage, StageAssetInfo rightStage)
    {
        int numberCompare = leftStage.number.CompareTo(rightStage.number);
        if (numberCompare != 0)
            return numberCompare;

        return string.CompareOrdinal(leftStage.name, rightStage.name);
    }
}
#endif
