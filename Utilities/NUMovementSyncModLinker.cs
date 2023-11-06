using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    public class NUMovementSyncModLinker : UdonSharpBehaviour
    {
        //To be used since static variables do not exist in UdonSharp yet

        /*
        How to use:
        [RequireComponent(typeof(NUMovementSyncModLinker))]

        NUMovementSyncModLinker linkedNUMovement;

        private void Start()
        {
            linkedNUMovement = transform.GetComponent<NUMovementSyncModLinker>().LinkedNUMovmement;
        }
        */

        [HideInInspector] public NUMovementSyncMod LinkedNUMovementSyncMod; //Assigned via Editor script
    }
}