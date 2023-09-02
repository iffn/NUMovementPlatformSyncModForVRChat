using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsNUMovementPlatformSyncMod
{
    public class movementModLinker : UdonSharpBehaviour
    {
        [SerializeField] NUMovementSyncMod linkedMovementMod;

        public NUMovementSyncMod LinkedMovementMod
        {
            get
            {
                return linkedMovementMod;
            }
        }
    }
}