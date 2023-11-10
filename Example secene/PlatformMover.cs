using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace NUMovementPlatformSyncMod.ExampleScene
{
    public class PlatformMover : UdonSharpBehaviour
    {
        [SerializeField] float transitionTime = 2;

        [SerializeField] Transform startPosition;
        [SerializeField] Transform endPosition;

        float completeTime;
        float offset;

        private void Start()
        {
            completeTime = transitionTime * 2;

            float currentIntervalServer = (float)Networking.GetServerTimeInSeconds() % completeTime;
            float currentIntervalLocal = Time.time % completeTime;
            offset = currentIntervalServer - currentIntervalLocal;
        }

        private void Update()
        {
            float lerpValue = ((Time.time + offset) % completeTime) / transitionTime;
            if (lerpValue > 1) lerpValue = 2 - lerpValue;

            transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition.position, endPosition.position, lerpValue),
                Quaternion.Lerp(startPosition.rotation, endPosition.rotation, lerpValue));
        }
    }
}