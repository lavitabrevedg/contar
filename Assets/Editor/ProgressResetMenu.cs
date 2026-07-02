using UnityEditor;
using UnityEngine;

public static class ProgressResetMenu
{
    [MenuItem("contar/Reset Progress To New User Defaults And Upload")]
    private static void ResetProgressToNewUserDefaultsAndUpload()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Reset Progress",
            "Reset local progress to new user defaults?\n\nTickets: 3\nCleared stages: none\nStart stage: Stage 1\n\nFirebase upload will be requested if Firebase is ready.",
            "Reset",
            "Cancel");

        if (!confirmed)
            return;

        StageProgressService progressService = Object.FindFirstObjectByType<StageProgressService>();
        GameObject temporaryProgressObject = null;
        if (progressService == null)
        {
            temporaryProgressObject = new GameObject("StageProgressService_ResetHelper");
            progressService = temporaryProgressObject.AddComponent<StageProgressService>();
        }

        progressService.ResetToNewUserDefaults();
        Debug.Log("[ProgressResetMenu] Progress reset to new user defaults. tickets=3, highestClearedStageIndex=-1, currentStageIndex=0");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ProgressResetMenu] Firebase upload skipped because Unity is not in Play Mode. Local progress was reset. Enter Play Mode after Firebase signs in, then run this menu again to upload.");
            DestroyTemporaryProgressObject(temporaryProgressObject);
            return;
        }

        Firebase_Manager firebaseManager = Firebase_Manager.Instance;
        if (firebaseManager == null)
            firebaseManager = Object.FindFirstObjectByType<Firebase_Manager>();

        if (firebaseManager == null)
        {
            Debug.LogWarning("[ProgressResetMenu] Firebase upload skipped because Firebase_Manager is not available. Enter Play Mode and sign in, then run this menu again to upload.");
            DestroyTemporaryProgressObject(temporaryProgressObject);
            return;
        }

        bool uploadRequested = firebaseManager.WriteData(progressService.CreateSnapshot());
        if (uploadRequested)
            Debug.Log("[ProgressResetMenu] Firebase upload requested.");
        else
            Debug.LogWarning("[ProgressResetMenu] Firebase upload was not requested. Check the Firebase_Manager warning above.");

        DestroyTemporaryProgressObject(temporaryProgressObject);
    }

    private static void DestroyTemporaryProgressObject(GameObject temporaryProgressObject)
    {
        if (temporaryProgressObject == null)
            return;

        Object.DestroyImmediate(temporaryProgressObject);
    }
}
