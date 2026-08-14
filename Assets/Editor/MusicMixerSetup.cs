using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class MusicMixerSetup
{
    private const string MixerDirectory = "Assets/Resources/Audio";
    private const string MixerPath = MixerDirectory + "/MusicMixer.mixer";
    private const string MixerGroupName = "Music";
    private const float MusicGainDecibels = 3f;

    [MenuItem("Tools/Contar/Ensure Music Mixer")]
    public static void EnsureMusicMixer()
    {
        EnsureDirectory(MixerDirectory);

        AudioMixer musicMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        object mixerController = musicMixer;
        Type mixerControllerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
        if (mixerControllerType == null)
            throw new InvalidOperationException("Unity AudioMixerController type could not be found.");

        if (mixerController == null)
        {
            MethodInfo createMixerMethod = mixerControllerType.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (createMixerMethod == null)
                throw new InvalidOperationException("AudioMixer creation method could not be found.");

            mixerController = createMixerMethod.Invoke(null, new object[] { MixerPath });
            musicMixer = mixerController as AudioMixer;
        }

        if (musicMixer == null || mixerController == null)
            throw new InvalidOperationException("MusicMixer could not be created.");

        PropertyInfo masterGroupProperty = mixerControllerType.GetProperty(
            "masterGroup",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        PropertyInfo targetSnapshotProperty = mixerControllerType.GetProperty(
            "TargetSnapshot",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (masterGroupProperty == null || targetSnapshotProperty == null)
            throw new InvalidOperationException("MusicMixer master group or snapshot could not be found.");

        object masterGroup = masterGroupProperty.GetValue(mixerController);
        object targetSnapshot = targetSnapshotProperty.GetValue(mixerController);
        if (masterGroup == null || targetSnapshot == null)
            throw new InvalidOperationException("MusicMixer master group or snapshot is invalid.");

        UnityEngine.Object masterGroupObject = masterGroup as UnityEngine.Object;
        if (masterGroupObject == null)
            throw new InvalidOperationException("MusicMixer master group is not a Unity object.");

        masterGroupObject.name = MixerGroupName;
        Type masterGroupType = masterGroup.GetType();
        SetGroupVolume(masterGroupType, masterGroup, mixerController, targetSnapshot);
        EnsureLimiter(musicMixer, masterGroupType, masterGroup, mixerController, targetSnapshot);

        EditorUtility.SetDirty(musicMixer);
        EditorUtility.SetDirty(masterGroupObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MusicMixerSetup] Music mixer gain and peak limiter are configured.");
    }

    private static void SetGroupVolume(
        Type masterGroupType,
        object masterGroup,
        object mixerController,
        object targetSnapshot)
    {
        MethodInfo setVolumeMethod = masterGroupType.GetMethod(
            "SetValueForVolume",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setVolumeMethod == null)
            throw new InvalidOperationException("MusicMixer volume method could not be found.");

        setVolumeMethod.Invoke(
            masterGroup,
            new object[] { mixerController, targetSnapshot, MusicGainDecibels });
    }

    private static void EnsureLimiter(
        AudioMixer musicMixer,
        Type masterGroupType,
        object masterGroup,
        object mixerController,
        object targetSnapshot)
    {
        PropertyInfo effectsProperty = masterGroupType.GetProperty(
            "effects",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo insertEffectMethod = masterGroupType.GetMethod(
            "InsertEffect",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (effectsProperty == null || insertEffectMethod == null)
            throw new InvalidOperationException("MusicMixer effect methods could not be found.");

        Array effects = effectsProperty.GetValue(masterGroup) as Array;
        object compressorEffect = FindEffect(effects, "Compressor");
        if (compressorEffect == null)
        {
            Type effectType = Type.GetType("UnityEditor.Audio.AudioMixerEffectController, UnityEditor");
            if (effectType == null)
                throw new InvalidOperationException("AudioMixer effect type could not be found.");

            compressorEffect = Activator.CreateInstance(
                effectType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { "Compressor" },
                null);
            MethodInfo preallocateGuidsMethod = effectType.GetMethod(
                "PreallocateGUIDs",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (preallocateGuidsMethod == null)
                throw new InvalidOperationException("AudioMixer effect GUID allocation method could not be found.");

            preallocateGuidsMethod.Invoke(compressorEffect, null);
            UnityEngine.Object compressorObject = compressorEffect as UnityEngine.Object;
            if (compressorObject == null)
                throw new InvalidOperationException("AudioMixer compressor is not a Unity object.");

            AssetDatabase.AddObjectToAsset(compressorObject, musicMixer);
            int effectIndex = effects == null ? 0 : effects.Length;
            insertEffectMethod.Invoke(masterGroup, new object[] { compressorEffect, effectIndex });
            EditorUtility.SetDirty(compressorObject);
        }

        SetEffectParameter(compressorEffect, mixerController, targetSnapshot, "Threshold", -1f);
        SetEffectParameter(compressorEffect, mixerController, targetSnapshot, "Attack", 0.1f);
        SetEffectParameter(compressorEffect, mixerController, targetSnapshot, "Release", 50f);
        SetEffectParameter(compressorEffect, mixerController, targetSnapshot, "Make up gain", 0f);
    }

    private static object FindEffect(Array effects, string effectName)
    {
        if (effects == null)
            return null;

        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
        {
            object effect = effects.GetValue(effectIndex);
            if (effect == null)
                continue;

            PropertyInfo effectNameProperty = effect.GetType().GetProperty(
                "effectName",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            string currentEffectName = effectNameProperty == null
                ? string.Empty
                : effectNameProperty.GetValue(effect) as string;
            if (currentEffectName == effectName)
                return effect;
        }

        return null;
    }

    private static void SetEffectParameter(
        object effect,
        object mixerController,
        object targetSnapshot,
        string parameterName,
        float parameterValue)
    {
        MethodInfo setParameterMethod = effect.GetType().GetMethod(
            "SetValueForParameter",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setParameterMethod == null)
            throw new InvalidOperationException("MusicMixer effect parameter method could not be found.");

        setParameterMethod.Invoke(
            effect,
            new object[] { mixerController, targetSnapshot, parameterName, parameterValue });
    }

    private static void EnsureDirectory(string assetDirectory)
    {
        string[] pathParts = assetDirectory.Split('/');
        string currentPath = pathParts[0];
        for (int pathIndex = 1; pathIndex < pathParts.Length; pathIndex++)
        {
            string nextPath = currentPath + "/" + pathParts[pathIndex];
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, pathParts[pathIndex]);

            currentPath = nextPath;
        }
    }
}
