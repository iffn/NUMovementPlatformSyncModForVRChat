# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using NUMovementPlatformSyncMod;

public class ManualLinker : EditorWindow
{
    [MenuItem("Tools/iffnsStuff/MovementModLinker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ManualLinker));
    }

    void OnGUI()
    {
        if (GUILayout.Button("FindModAndAssignLinkers"))
        {
            AutoOnBuildLinker.FindModAndAssignLinkers();
        }
    }
}
#endif