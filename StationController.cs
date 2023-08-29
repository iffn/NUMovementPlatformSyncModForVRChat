
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class StationController : UdonSharpBehaviour
{
    [SerializeField] VRCStation linkedStation;

    [UdonSynced] public int attachedTransformIndex = -1;
    [UdonSynced] Vector3 syncedLocalPlayerPosition = Vector3.zero;
    [UdonSynced] float syncedLocalPlayerHeading = 0;

    public Transform GroundTransform { set; private get; }

    Transform[] movingTransforms;

    int previouslyAttachedTransformIndex = -1;


    //CyanPlayerObjectPool stuff
    public VRCPlayerApi Owner;

    iffnsNuMovementMod iffnsNuMovementModLink;

    bool setupComplete = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Home))
        {
            Debug.Log($" ");
            Debug.Log($"Debug of {nameof(StationController)}");
            Debug.Log($"{nameof(attachedTransformIndex)} = {attachedTransformIndex}");
            Debug.Log($"{nameof(syncedLocalPlayerPosition)} = {syncedLocalPlayerPosition}");
            Debug.Log($"{nameof(syncedLocalPlayerHeading)} = {syncedLocalPlayerHeading}");
            Debug.Log($"{nameof(GroundTransform)} = {GroundTransform}");
            Debug.Log($"{nameof(movingTransforms)}.Length = {movingTransforms.Length}");
            Debug.Log($"{nameof(previouslyAttachedTransformIndex)} = {previouslyAttachedTransformIndex}");
            Debug.Log($"{nameof(Owner)}.isLocal = {Owner.isLocal}");
            if (linkedStation) Debug.Log($"{nameof(linkedStation)}.PlayerMobility = {linkedStation.PlayerMobility}");
            Debug.Log($"{nameof(iffnsNuMovementModLink)} = {iffnsNuMovementModLink}");
            Debug.Log($"{nameof(setupComplete)} = {setupComplete}");
            //Debug.Log($"{nameof()} = {}");
        }
    }

    public void _OnOwnerSet()
    {
        movementModLinker linker = transform.parent.GetComponent<movementModLinker>();

        iffnsNuMovementModLink = linker.LinkedMovementMod;

        movingTransforms = iffnsNuMovementModLink.MovingTransforms;

        if (Owner.isLocal)
        {
            linkedStation.PlayerMobility = VRCStation.Mobility.Mobile;

            iffnsNuMovementModLink.AttachStation(this, linkedStation);
        }
        else
        {
            linkedStation.PlayerMobility = VRCStation.Mobility.Immobilize;
        }

        setupComplete = true;
    }

    public void _OnCleanup()
    {
        linkedStation.PlayerMobility = VRCStation.Mobility.Immobilize;
    }

    //VRChat functions
    public override void OnPreSerialization()
    {
        if(!setupComplete) return;

        if (attachedTransformIndex != -1)
        {
            syncedLocalPlayerPosition = GroundTransform.InverseTransformPoint(Networking.LocalPlayer.GetPosition());
            syncedLocalPlayerHeading = (GroundTransform.rotation * Networking.LocalPlayer.GetRotation()).eulerAngles.y;
        }
    }

    public override void OnDeserialization()
    {
        if(!setupComplete) return;
        
        if (previouslyAttachedTransformIndex != attachedTransformIndex)
        {
            previouslyAttachedTransformIndex = attachedTransformIndex;

            if (attachedTransformIndex == -1)
            {
                
            }
            else
            {
                linkedStation.transform.parent = movingTransforms[attachedTransformIndex];
            }

        }

        if (attachedTransformIndex != -1)
        {
            //Sync position - ToDo: Make smooth

            linkedStation.transform.localPosition = syncedLocalPlayerPosition;
            linkedStation.transform.localRotation = Quaternion.Euler(0, syncedLocalPlayerHeading, 0);
        }
    }
}
