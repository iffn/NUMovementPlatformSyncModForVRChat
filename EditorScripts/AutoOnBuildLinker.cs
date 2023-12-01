# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using NUMovementPlatformSyncMod;
using VRC.SDKBase.Editor.BuildPipeline;
using UdonSharp;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Encodings;

namespace NUMovementPlatformSyncMod.EditorScripts
{
    public class AutoOnBuildLinker : Editor, IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder { get { return 0; } }

        static NUMovementSyncMod syncMod;


        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            FindModAndAssignLinkers();

            return true;
        }

        public static void FindModAndAssignLinkers()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            sw.Start();

            List<Transform> allTransforms = new List<Transform>();

            foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                allTransforms.Add(rootObject.transform);

                foreach (Transform child in rootObject.transform.GetComponentsInChildren<Transform>(true)) //Very important to add true to include inactive object
                {
                    allTransforms.Add(child);
                }
            }

            //Find mod
            if (syncMod == null)
            {
                foreach (Transform transform in allTransforms)
                {
                    if (transform.TryGetComponent(out NUMovementSyncMod mod))
                    {
                        syncMod = mod;
                        break;
                    }
                }
            }

            if (syncMod == null)
            {
                Debug.Log("Error: Sync mod not found");
                return;
            }

            int assignments = 0;

            List<Transform> boatPlatforms = new List<Transform>();
            List<PlayerColliderController> playerColliderControllers = new List<PlayerColliderController>();

            foreach (Transform transform in allTransforms)
            {
                if (transform.TryGetComponent(out NUMovementLinker normalLinker))
                {
                    normalLinker.LinkedNUMovmement = syncMod;
                    MarkAsModified(normalLinker); //Needed 
                    assignments++;
                }

                if (transform.TryGetComponent(out NUMovementSyncModLinker syncLinker))
                {
                    syncLinker.LinkedNUMovementSyncMod = syncMod;
                    MarkAsModified(syncLinker); //Needed 
                    assignments++;
                }

                if (transform.TryGetComponent(out PlayerColliderController linkedCollider))
                {
                    playerColliderControllers.Add(linkedCollider);
                }
            }


            //Assign platforms
            List<Transform> currentPlatformsAsList = new List<Transform>();

            syncMod.MovingTransforms = playerColliderControllers.ToArray();

            MarkAsModified(syncMod);

            sw.Stop();

            Debug.Log($"{nameof(AutoOnBuildLinker)} took {sw.Elapsed.TotalSeconds}s to complete");
            Debug.Log($"Transforms searched: {allTransforms.Count}");
            Debug.Log($"{nameof(assignments)} = {assignments}");
        }

        static void MarkAsModified(UdonSharpBehaviour target)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }
    }
}
#endif