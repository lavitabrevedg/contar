using System;
using System.Collections.Generic;

[Serializable]
public sealed class OptimizationProfileData
{
    public string label;
    public string measuredAtUtc;
    public string unityVersion;
    public string activeBuildTarget;
    public int frameSampleCount;
    public double mainThreadAverageMs;
    public double mainThreadP95Ms;
    public double mainThreadP99Ms;
    public long gcAllocatedAverageBytes;
    public long gcAllocatedPeakBytes;
    public long totalUsedMemoryBytes;
    public long totalReservedMemoryBytes;
    public long drawCallsAverage;
    public long batchesAverage;
    public int mapGenerationIterations;
    public double mapGenerationAverageMs;
    public double mapGenerationP95Ms;
    public long mapGenerationAverageManagedBytes;
    public int solverStageCount;
    public int solverIterationsPerStage;
    public double solverAverageMs;
    public long solverAverageManagedBytes;
    public int solverMaximumStatesExplored;
    public long fontAssetFileBytes;
    public long fontAtlasRuntimeBytes;
    public int fontAtlasWidth;
    public int fontAtlasHeight;
    public int fontCharacterCount;
    public long apkFileBytes;
    public ulong buildReportBytes;
    public double buildDurationSeconds;
    public string buildResult;
    public string errorMessage;
}

public static class OptimizationStatistics
{
    public static double AverageMilliseconds(List<long> nanosecondSamples)
    {
        if (nanosecondSamples == null || nanosecondSamples.Count == 0)
            return 0d;

        double totalNanoseconds = 0d;
        for (int sampleIndex = 0; sampleIndex < nanosecondSamples.Count; sampleIndex++)
            totalNanoseconds += nanosecondSamples[sampleIndex];

        return totalNanoseconds / nanosecondSamples.Count / 1000000d;
    }

    public static double PercentileMilliseconds(List<long> nanosecondSamples, double percentile)
    {
        if (nanosecondSamples == null || nanosecondSamples.Count == 0)
            return 0d;

        long[] sortedSamples = nanosecondSamples.ToArray();
        Array.Sort(sortedSamples);
        int percentileIndex = (int)Math.Ceiling(percentile * sortedSamples.Length) - 1;
        percentileIndex = Math.Max(0, Math.Min(percentileIndex, sortedSamples.Length - 1));
        return sortedSamples[percentileIndex] / 1000000d;
    }

    public static long Average(List<long> samples)
    {
        if (samples == null || samples.Count == 0)
            return 0L;

        decimal total = 0m;
        for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
            total += samples[sampleIndex];

        return (long)(total / samples.Count);
    }

    public static long Maximum(List<long> samples)
    {
        if (samples == null || samples.Count == 0)
            return 0L;

        long maximum = samples[0];
        for (int sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
        {
            if (samples[sampleIndex] > maximum)
                maximum = samples[sampleIndex];
        }

        return maximum;
    }
}
