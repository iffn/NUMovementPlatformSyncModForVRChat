
using Nessie.Udon.Movement;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class iffnsNuMovementMod : NUMovement
{
    //Variable definitions
    VRCStation linkedStation;
    [SerializeField] Transform[] movingTransforms;

    public Transform[] MovingTransforms
    {
        get
        {
            return movingTransforms;
        }
    }

    int attachedTransformIndex = -1;

    StationController linkedStationController;
    Transform previouslyAttachedTransform;

    public void AttachStation(StationController linkedStationController, VRCStation linkedStation)
    {
        this.linkedStationController = linkedStationController;
        this.linkedStation = linkedStation;
    }

    //Unity funcions
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Home))
        {
            Debug.Log($" ");
            Debug.Log($"Debug of {nameof(iffnsNuMovementMod)}");
            Debug.Log($"{nameof(linkedStation)} = {linkedStation}");
            Debug.Log($"{nameof(movingTransforms)}.Length = {movingTransforms.Length}");
            Debug.Log($"{nameof(linkedStationController)} = {linkedStationController}");
            Debug.Log($"{nameof(previouslyAttachedTransform)} = {previouslyAttachedTransform}");
            //Debug.Log($"{nameof()} = {}");
        }

        if (linkedStationController == null) return;

        bool serialize = false;

        if (previouslyAttachedTransform != GroundTransform)
        {
            previouslyAttachedTransform = GroundTransform;

            linkedStationController.GroundTransform = GroundTransform;

            serialize = true;

            attachedTransformIndex = -1;

            if (GroundTransform != null)
            {
                for (int i = 0; i < movingTransforms.Length; i++)
                {
                    if (movingTransforms[i] == GroundTransform)
                    {
                        attachedTransformIndex = i;
                        break;
                    }
                }
            }

            linkedStationController.attachedTransformIndex = attachedTransformIndex;
        }

        if (serialize || attachedTransformIndex != -1) linkedStationController.serialize = true;
    }

    // Nu movement stuff
    /*
    public new Transform GroundTransform
    {
        get
        {
            return base.GroundTransform;
        }
    }
    */

    protected override void ApplyToPlayer()
    {
        base.ApplyToPlayer();

        if (attachedTransformIndex != -1 && linkedStation)
        {
            linkedStation.transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());
            linkedStation.UseStation(Networking.LocalPlayer);
        }
    }


    /*
    float lastDeserializationTime = 0;
    float lastDeserializationDeltaTime = 0;
    const float deserializationTimeThreshold = 1;
    Vector3 lastSycnedLocalPositionValue = Vector3.zero;
    Vector3 lastLocalPositionValue = Vector3.zero;
    Vector3 LocalPlayerPosition = Vector3.zero;
    Vector3 localPositionSpeed = Vector3.zero;
    float LocalPlayerHeading = 0;
    float lastSyncedLocalPlayerHeading = 0;
    float lastLocalHeadingValue = 0;
    float localHeadingSpeed = 0;
    int previousDimensionID = -1;
    Transform attachedDimensionTransform;

    void Deserialize()
    {
        //Time
        lastDeserializationDeltaTime = Time.time - lastDeserializationTime - Time.deltaTime;
        if (lastDeserializationDeltaTime > deserializationTimeThreshold) lastDeserializationDeltaTime = deserializationTimeThreshold;

        lastDeserializationTime = Time.time;

        //Position
        lastSycnedLocalPositionValue = SyncedLocalPlayerPosition; //Sync check
        lastLocalPositionValue = LocalPlayerPosition;
        localPositionSpeed = (SyncedLocalPlayerPosition - lastLocalPositionValue) / lastDeserializationDeltaTime;

        //Rotation
        if (LocalPlayerHeading > 360) LocalPlayerHeading -= 360;
        else if (LocalPlayerHeading < 0) LocalPlayerHeading += 360;

        lastSyncedLocalPlayerHeading = SyncedLocalPlayerHeading;
        lastLocalHeadingValue = LocalPlayerHeading;

        float headingOffset = SyncedLocalPlayerHeading - lastLocalHeadingValue;

        if (headingOffset > 180)
        {
            headingOffset -= 360;
        }
        else if (headingOffset < -180)
        {
            headingOffset += 360;
        }

        localHeadingSpeed = headingOffset / lastDeserializationDeltaTime;
    }
    */
}
