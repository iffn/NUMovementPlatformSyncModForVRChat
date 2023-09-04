using Nessie.Udon.Movement;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    public class NUMovementSyncMod : NUMovement
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

        //Unity functions
        private void Update()
        {
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
        protected override void ApplyToPlayer()
        {
            base.ApplyToPlayer();

            if (attachedTransformIndex != -1 && linkedStation)
            {
                linkedStation.transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation);
                linkedStation.UseStation(Networking.LocalPlayer);
            }
        }
        /*
        public new Transform GroundTransform
        {
            get
            {
                return base.GroundTransform;
            }
        }
        */
    }
}