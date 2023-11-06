using Nessie.Udon.Movement;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    public class NUMovementLinker : UdonSharpBehaviour
    {
        //To be used since static variables do not exist in UdonSharp yet

        /*
        How to use:
        [RequireComponent(typeof(NUMovementLinker))]

        NUMovement linkedNUMovement;

        private void Start()
        {
            linkedNUMovement = transform.GetComponent<NUMovementLinker>().LinkedNUMovmement; //To be replced with static access when available in U#
        }
        */

        public NUMovement LinkedNUMovmement; //Assigned via Editor script
    }
}