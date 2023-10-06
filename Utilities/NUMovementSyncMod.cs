using Nessie.Udon.Movement;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    public enum PlatformState
    {
        Grounded,
        Immobilized,
        InAir,
        Disabled //In station or Swimming
    }

    public class NUMovementSyncMod : NUMovement
    {
        /*
        Remaining issues:
        - VR: Playspace sometimes rotates relative to the platform the player is standing on when turning their head
        */

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

        public PlatformState currentPlatformState = PlatformState.InAir;

        public int attachedTransformIndex = -1;

        StationController linkedStationController;
        Transform previouslyAttachedTransform;

        public void AttachStation(StationController linkedStationController, VRCStation linkedStation)
        {
            this.linkedStationController = linkedStationController;
            this.linkedStation = linkedStation;
        }

        [SerializeField] float airTimeBeforePlatformDetach = 2;
        float detachTime;

        public void DetachFromStationWhenInAirEvent()
        {
            if (Time.time < detachTime)
            {
                return; //Ignore old event
            }

            if (currentPlatformState != PlatformState.InAir)
            {
                return; //Ignore if no longer in the air
            }

            linkedStationController.LocalPlayerDetachedFromTransform();
            attachedTransformIndex = -1;
        }

        //Unity functions
        private void Update()
        {
            /*
            if (Input.GetKeyDown(KeyCode.Home))
            {
                Debug.Log($" ");
                Debug.Log($"Debug of {nameof(NUMovementSyncMod)}");
                Debug.Log($"{nameof(linkedStation)} = {linkedStation}");
                Debug.Log($"{nameof(movingTransforms)}.Length = {movingTransforms.Length}");
                Debug.Log($"{nameof(linkedStationController)} = {linkedStationController}");
                Debug.Log($"{nameof(previouslyAttachedTransform)} = {previouslyAttachedTransform}");
                //Debug.Log($"{nameof()} = {}");
            }
            */

            if (currentPlatformState == PlatformState.Disabled || currentPlatformState == PlatformState.Immobilized) return;

            if (linkedStationController == null) return;

            if (previouslyAttachedTransform != GroundTransform)
            {
                OnGroundChange();

                previouslyAttachedTransform = GroundTransform;
            }
        }

        void OnGroundChange()
        {
            if (GroundTransform == null)
            {
                currentPlatformState = PlatformState.InAir;

                if(attachedTransformIndex >= 0)
                {
                    Debug.Log("Now waiting for detach");

                    detachTime = Time.time + airTimeBeforePlatformDetach;
                    SendCustomEventDelayedSeconds(nameof(DetachFromStationWhenInAirEvent), airTimeBeforePlatformDetach);
                }
            }
            else
            {
                currentPlatformState = PlatformState.Grounded;
                HandleAttachedTransform();
            }
        }

        void HandleAttachedTransform()
        {
            for (int i = 0; i < MovingTransforms.Length; i++)
            {
                if (MovingTransforms[i] == GroundTransform)
                {
                    if (attachedTransformIndex >= 0)
                    {
                        linkedStationController.LocalPlayerSwitchedTransform(GroundTransform, i);
                    }
                    else
                    {
                        linkedStationController.LocalPlayerAttachedToTransform(GroundTransform, i);
                    }

                    attachedTransformIndex = i;

                    return;
                }
            }

            //if not found
            if (attachedTransformIndex < 0) return; //Ignore if already not attached

            linkedStationController.LocalPlayerDetachedFromTransform();

            attachedTransformIndex = -1;
        }

        // Nu movement stuff
        public override void _SetCanMove(bool canMove)
        {
            base._SetCanMove(canMove);

            if (canMove)
            {
                currentPlatformState = PlatformState.Immobilized;
            }
            else
            {
                currentPlatformState = PlatformState.InAir;
            }
        }

        public override void _ControllerEnable()
        {
            currentPlatformState = PlatformState.InAir;

            base._ControllerEnable();
        }

        public override void _ControllerDisable()
        {
            currentPlatformState = PlatformState.Disabled;

            base._ControllerDisable();
        }

        protected override void ApplyToPlayer()
        {
            base.ApplyToPlayer();

            if (attachedTransformIndex != -1 && linkedStation)
            {
                linkedStation.transform.SetPositionAndRotation(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).position, Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).rotation); //Avatar root currently defines the player position
                linkedStation.UseStation(Networking.LocalPlayer);
            }
        }
    }
}