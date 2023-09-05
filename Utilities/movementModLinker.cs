using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    public class MovementModLinker : UdonSharpBehaviour
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