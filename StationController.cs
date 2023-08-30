
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

    Vector3 syncedPlayerRotationEuler = Vector3.zero;

    public Transform GroundTransform { set; private get; }

    Transform[] movingTransforms;

    int previouslyAttachedTransformIndex = -1;

    Vector3 previousPlayerPosition;
    Vector3 previousPlayerLinearVelocity;
    Vector3 previousPlayerAngularVelocityEuler;

    //CyanPlayerObjectPool stuff
    public VRCPlayerApi Owner;

    iffnsNuMovementMod iffnsNuMovementModLink;

    bool setupComplete = false;

    bool inStation = false;

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

    private void FixedUpdate()
    {
        if (inStation)
        {
            transform.position = Vector3.SmoothDamp(transform.position, syncedLocalPlayerPosition, ref previousPlayerLinearVelocity, 0.04f, Mathf.Infinity, Time.fixedDeltaTime);
            transform.rotation = Quaternion.Euler(Vector3.SmoothDamp(transform.rotation.eulerAngles, syncedPlayerRotationEuler, ref previousPlayerAngularVelocityEuler, 0.04f, Mathf.Infinity, Time.fixedDeltaTime));
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

        syncedPlayerRotationEuler = new Vector3(0, syncedLocalPlayerHeading, 0);
        /*
        if (attachedTransformIndex != -1)
        {
            //Sync position - ToDo: Make smooth
            linkedStation.transform.localPosition = syncedLocalPlayerPosition;
            linkedStation.transform.localRotation = Quaternion.Euler(0, syncedLocalPlayerHeading, 0);
        }
        */
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        previousPlayerLinearVelocity = player.GetVelocity();
        previousPlayerAngularVelocityEuler = Vector3.zero;
        transform.position = player.GetPosition();
        transform.rotation = player.GetRotation();
        linkedStation.PlayerMobility = VRCStation.Mobility.Immobilize;

        inStation = true;
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        linkedStation.PlayerMobility = VRCStation.Mobility.Mobile;
        inStation = false;
    }
}
