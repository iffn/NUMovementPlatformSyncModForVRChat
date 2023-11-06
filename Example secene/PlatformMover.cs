using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace NUMovementPlatformSyncMod.ExampleScene
{
    public class PlatformMover : UdonSharpBehaviour
    {
        double transitionTime = 2;

        [SerializeField] Transform startPosition;
        [SerializeField] Transform endPosition;

        private void Update()
        {
            float lerpValue = (float)(Networking.GetServerTimeInSeconds() % (transitionTime * 2) / transitionTime);
            if (lerpValue > 1) lerpValue = 2 - lerpValue;

            transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition.position, endPosition.position, lerpValue),
                Quaternion.Lerp(startPosition.rotation, endPosition.rotation, lerpValue));
        }
    }
}