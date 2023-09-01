
using Nessie.Udon.Movement;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class StationInformer : UdonSharpBehaviour
{
    [SerializeField] NUMovement linkedNuMovement;

    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (!player.isLocal) return;

        linkedNuMovement._ControllerDisable();
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        if (!player.isLocal) return;

        linkedNuMovement._ControllerEnable();
    }
}
