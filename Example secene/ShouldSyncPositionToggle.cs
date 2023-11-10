using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod.ExampleScene
{
    public class ShouldSyncPositionToggle : UdonSharpBehaviour
    {
        [SerializeField] PlayerColliderController linkedColliderController;

        private void Start()
        {
            bool shouldSync = linkedColliderController.shouldSyncPlayer;
            InteractionText = shouldSync ? "Disable sync" : "Enable sync";
        }

        public override void Interact()
        {
            bool shouldSync = !linkedColliderController.shouldSyncPlayer;
            linkedColliderController.shouldSyncPlayer = shouldSync;

            InteractionText = shouldSync ? "Disable sync" : "Enable sync";
        }
    }
}