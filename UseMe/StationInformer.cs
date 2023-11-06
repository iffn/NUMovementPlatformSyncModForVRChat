using Nessie.Udon.Movement;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    [RequireComponent(typeof(NUMovementLinker))]
    public class StationInformer : UdonSharpBehaviour
    {
        /*
        Purpose:
        - Toggle the NUMovement teleport when entering and exiting a station by informing it

        Assumption:
        - When hopping from one station to the other. OnStationExit is called before OnStationEnter

        Limitation:
        - Does not work with avatar stations
        */

        NUMovement linkedNUMovement;

        private void Start()
        {
            linkedNUMovement = transform.GetComponent<NUMovementLinker>().LinkedNUMovmement; //To be replced with static access when available in U#
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (!player.isLocal) return;

            linkedNUMovement._ControllerDisable();
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!player.isLocal) return;

            linkedNUMovement._ControllerEnable();
        }
    }
}