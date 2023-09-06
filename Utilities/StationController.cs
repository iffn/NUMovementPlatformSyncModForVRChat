//#define enableSmoothTimeControl
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    public class StationController : UdonSharpBehaviour
    {
        [SerializeField] VRCStation linkedStation;

        [UdonSynced] public int attachedTransformIndex = -1;
        [UdonSynced] Vector3 syncedLocalPlayerPosition = Vector3.zero;
        [UdonSynced] float syncedLocalPlayerHeading = 0;

        public Transform GroundTransform { set; private get; }

        Transform[] movingTransforms;

        int previouslyAttachedTransformIndex = -1;

        Vector3 previousPlayerPosition;
        Vector3 previousPlayerLinearVelocity;
        float previousPlayerAngularVelocity;

        //CyanPlayerObjectPool stuff
        public VRCPlayerApi Owner;

        NUMovementSyncMod NUMovementSyncModLink;

        bool setupComplete = false;

        bool onPlatform = false;

        float smoothTime = 0.068f;

        readonly float timeBetweenSerializations = 1f / 6f;
        float nextSerializationTime = 0f;
        public bool serialize = false;

        float smoothHeading = 0;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                Debug.Log($" ");
                Debug.Log($"Debug of {nameof(StationController)}");
                Debug.Log($"{nameof(attachedTransformIndex)} = {attachedTransformIndex}");
                Debug.Log($"{nameof(syncedLocalPlayerPosition)} = {syncedLocalPlayerPosition}");
                Debug.Log($"transform.localPosition = {transform.localPosition}");
                Debug.Log($"{nameof(syncedLocalPlayerHeading)} = {syncedLocalPlayerHeading}");
                Debug.Log($"transform.localRotation.eulerAngles = {transform.localRotation.eulerAngles}");
                Debug.Log($"{nameof(GroundTransform)} = {GroundTransform}");
                Debug.Log($"{nameof(movingTransforms)}.Length = {movingTransforms.Length}");
                Debug.Log($"{nameof(previouslyAttachedTransformIndex)} = {previouslyAttachedTransformIndex}");
                Debug.Log($"{nameof(Owner)}.isLocal = {Owner.isLocal}");
                if (linkedStation) Debug.Log($"{nameof(linkedStation)}.PlayerMobility = {linkedStation.PlayerMobility}");
                Debug.Log($"{nameof(NUMovementSyncModLink)} = {NUMovementSyncModLink}");
                Debug.Log($"{nameof(setupComplete)} = {setupComplete}");
                //Debug.Log($"{nameof()} = {}");
            }

#if enableSmoothTimeControl
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                smoothTime *= 1.1f;
                Debug.Log($"{nameof(smoothTime)} now set to {smoothTime}");
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                smoothTime /= 1.1f;
                Debug.Log($"{nameof(smoothTime)} now set to {smoothTime}");
            }
#endif

            if (onPlatform)
            {
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, syncedLocalPlayerPosition, ref previousPlayerLinearVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

                smoothHeading = Mathf.SmoothDampAngle(smoothHeading, syncedLocalPlayerHeading, ref previousPlayerAngularVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

                transform.localRotation = Quaternion.Euler(0, smoothHeading , 0);

                /*
                //ToDo: Fix level with horizon
                Quaternion localHeading = Quaternion.Euler(0, Mathf.SmoothDampAngle(transform.localRotation.eulerAngles.y, syncedLocalPlayerHeading, ref previousPlayerAngularVelocity, 0.04f, Mathf.Infinity, Time.deltaTime), 0);
                transform.LookAt(transform.parent.TransformDirection(localHeading * Vector3.forward),Vector3.up);
                */
            }

            if (serialize && Time.timeSinceLevelLoad > nextSerializationTime)
            {
                serialize = false;
                RequestSerialization();
            }
        }

        public void _OnOwnerSet()
        {
            MovementModLinker linker = transform.parent.parent.GetComponent<MovementModLinker>();

            NUMovementSyncModLink = linker.LinkedMovementMod;

            movingTransforms = NUMovementSyncModLink.MovingTransforms;

            if (Owner.isLocal)
            {
                linkedStation.PlayerMobility = VRCStation.Mobility.Mobile;

                NUMovementSyncModLink.AttachStation(this, linkedStation);
            }
            else
            {
                //linkedStation.PlayerMobility = VRCStation.Mobility.Immobilize;
            }

            setupComplete = true;
        }

        public void _OnCleanup()
        {
            linkedStation.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
        }

        //VRChat functions
        public override void OnPreSerialization()
        {
            nextSerializationTime = Time.timeSinceLevelLoad + timeBetweenSerializations;

            if (!setupComplete) return;

            if (attachedTransformIndex != -1)
            {
                syncedLocalPlayerPosition = GroundTransform.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position);
                syncedLocalPlayerHeading = (Quaternion.Inverse(GroundTransform.rotation) * Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation).eulerAngles.y;
            }
        }

        public override void OnDeserialization()
        {
            if (!setupComplete) return;

            if (previouslyAttachedTransformIndex != attachedTransformIndex)
            {
                previouslyAttachedTransformIndex = attachedTransformIndex;

                if (attachedTransformIndex == -1)
                {
                    linkedStation.PlayerMobility = VRCStation.Mobility.Mobile;
                    onPlatform = false;
                }
                else
                {
                    onPlatform = true;
                    linkedStation.transform.parent = movingTransforms[attachedTransformIndex];
                    linkedStation.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
                    previousPlayerLinearVelocity = Vector3.zero;
                    previousPlayerAngularVelocity = 0;
                }
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            
        }
    }
}