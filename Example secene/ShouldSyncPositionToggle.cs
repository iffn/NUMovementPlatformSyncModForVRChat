using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod.ExampleScene
{
    public class ShouldSyncPositionToggle : UdonSharpBehaviour
    {
        [SerializeField] PlayerColliderController linkedColliderController;

        public override void Interact()
        {
            bool shouldSync = !linkedColliderController.shouldSyncPlayer;
            linkedColliderController.shouldSyncPlayer = shouldSync;

            InteractionText = shouldSync ? "Disable sync" : "Enable sync";
        }
    }
}