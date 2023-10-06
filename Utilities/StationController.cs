using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    [RequireComponent(typeof(VRCStation))]
    public class StationController : UdonSharpBehaviour
    {
        /*
        Remaining issues:
        - Remote desktop players jitter in their rotation (Station turns smoothly)
        */

        VRCStation linkedStation;

        [UdonSynced] public int attachedTransformIndex = -1;
        [UdonSynced] Vector3 syncedLocalPlayerPosition = Vector3.zero;
        [UdonSynced] float syncedLocalPlayerHeading = 0;

        Transform groundTransform;

        Transform[] movingTransforms;

        int previouslyAttachedTransformIndex = -1;

        Vector3 previousPlayerPosition;
        Vector3 previousPlayerLinearVelocity;
        float previousPlayerAngularVelocity;

        //CyanPlayerObjectPool stuff
        public VRCPlayerApi Owner;

        NUMovementSyncMod NUMovementSyncModLink;

        bool setupComplete = false;

        readonly float smoothTime = 0.068f;

        readonly float timeBetweenSerializations = 1f / 6f;
        float nextSerializationTime = 0f;
        public bool serialize = false;

        bool myStation = false;

        float smoothHeading = 0;

        private void Start()
        {
            linkedStation = transform.GetComponent<VRCStation>();
        }

        //public override void PostLateUpdate()
        void Update()
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
                Debug.Log($"{nameof(groundTransform)} = {groundTransform}");
                Debug.Log($"{nameof(movingTransforms)}.Length = {movingTransforms.Length}");
                Debug.Log($"{nameof(previouslyAttachedTransformIndex)} = {previouslyAttachedTransformIndex}");
                Debug.Log($"{nameof(Owner)}.isLocal = {Owner.isLocal}");
                if (linkedStation) Debug.Log($"{nameof(linkedStation)}.PlayerMobility = {linkedStation.PlayerMobility}");
                Debug.Log($"{nameof(NUMovementSyncModLink)} = {NUMovementSyncModLink}");
                Debug.Log($"{nameof(setupComplete)} = {setupComplete}");
                //Debug.Log($"{nameof()} = {}");
            }

            if (myStation)
            {
                if(attachedTransformIndex >= 0)
                {
                    if(Time.timeSinceLevelLoad > nextSerializationTime)
                    {
                        RequestSerialization();
                    }
                }
            }
            else
            {
                if (attachedTransformIndex >= 0)
                {
                    transform.localPosition = Vector3.SmoothDamp(transform.localPosition, syncedLocalPlayerPosition, ref previousPlayerLinearVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

                    smoothHeading = Mathf.SmoothDampAngle(smoothHeading, syncedLocalPlayerHeading, ref previousPlayerAngularVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

                    transform.localRotation = Quaternion.Euler(0, smoothHeading, 0);
                }
            }
        }

        public void LocalPlayerAttachedToTransform(Transform newTransform, int index)
        {
            attachedTransformIndex = index;
            groundTransform = newTransform;
            transform.parent = newTransform;
        }

        public void LocalPlayerSwitchedTransform(Transform newTransform, int index)
        {
            attachedTransformIndex = index;
            groundTransform = newTransform;
            transform.parent = newTransform;
        }


        public void LocalPlayerDetachedFromTransform()
        {
            attachedTransformIndex = -1;
            groundTransform = null;
            
            RequestSerialization();
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

                myStation = true;
            }
            else
            {
                //linkedStation.PlayerMobility = VRCStation.Mobility.Immobilize;
                myStation = false;
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
                syncedLocalPlayerPosition = transform.localPosition;
                syncedLocalPlayerHeading = transform.localRotation.eulerAngles.y;
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
                }
                else
                {
                    transform.parent = movingTransforms[attachedTransformIndex];
                    transform.SetPositionAndRotation(Owner.GetPosition(), Owner.GetRotation());

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