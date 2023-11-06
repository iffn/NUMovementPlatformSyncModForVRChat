using Nessie.Udon.Movement;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    [RequireComponent(typeof(NUMovementLinker))]
    public class LinkedNUInteractTeleporter : UdonSharpBehaviour
    {
        NUMovement movement;
        [SerializeField] private Transform teleportTarget;

        private void Start()
        {
            movement = transform.GetComponent<NUMovementLinker>().LinkedNUMovmement;
        }

        public override void Interact()
        {
            movement._TeleportTo(teleportTarget.position, teleportTarget.rotation);
        }

        private void OnDrawGizmosSelected()
        {
            if (!teleportTarget) return;

            Gizmos.DrawLine(transform.position, teleportTarget.position);
        }
    }
}