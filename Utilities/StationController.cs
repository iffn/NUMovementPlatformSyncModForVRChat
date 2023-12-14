using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Cyan.PlayerObjectPool;

namespace NUMovementPlatformSyncMod
{
    [RequireComponent(typeof(VRCStation))]
    //[RequireComponent(typeof(NUMovementSyncModLinker))] //Unable to use since CyanPlayerObjectPool doesn't handle it correctly
    public class StationController : CyanPlayerObjectPoolObject
    {
        /*
        Purpose:
        - Interact with CyanObjectPool to assign each player a station
        - Sync position of local player position and attached transform to other players
        - Position remote players smoothly
        - Link station of local player to NUMovementSyncMod
        
        Remaining issues:
        - Remote desktop players jitter in their rotation (Station turns smoothly)
        */

        //Synced values
        [UdonSynced] public int attachedTransformIndex = -1;
        [UdonSynced] Vector3 syncedLocalPlayerPosition = Vector3.zero;
        [UdonSynced] float syncedLocalPlayerHeadingDeg = 0;

        //Values to be assigned
        [SerializeField] NUMovementSyncModLinker NUMovementSyncModLinker;
        
        //Static values
        NUMovementSyncMod NUMovementSyncModLink;
        readonly float smoothTime = 0.068f;
        readonly float timeBetweenSerializations = 1f / 6f;

        //Static values
        VRCStation linkedStation;
        PlayerColliderController[] movingTransforms;
        VRCPlayerApi localPlayer;
        bool inVR;
        bool myStation = false;

        //Runtime values
        float smoothHeadingDeg = 0;
        float nextSerializationTime = 0f;
        public bool OnOwnerSetRan { get; private set; } = false;
        Transform groundTransform;
        int previouslyAttachedTransformIndex = -1;
        Vector3 previousPlayerPosition;
        Vector3 previousPlayerLinearVelocity;
        float previousPlayerAngularVelocity;

        //Funcitons
        void Setup()
        {
            NUMovementSyncModLink = NUMovementSyncModLinker.LinkedNUMovementSyncMod;
            linkedStation = transform.GetComponent<VRCStation>();
            inVR = Networking.LocalPlayer.IsUserInVR();
        }

        public string[] DebugText
        {
            get
            {
                string[] returnString = new string[]
                {
                    $"Debug of {nameof(StationController)} called {gameObject.name}",
                    $"{nameof(Owner)} = {Owner.displayName}",
                    $"{nameof(Owner)}.isLocal = {Owner.isLocal}",
                    $"{nameof(attachedTransformIndex)} = {attachedTransformIndex}",
                    $"{nameof(syncedLocalPlayerPosition)} = {syncedLocalPlayerPosition}",
                    $"transform.localPosition = {transform.localPosition}",
                    $"{nameof(syncedLocalPlayerHeadingDeg)} = {syncedLocalPlayerHeadingDeg}",
                    $"transform.localRotation.eulerAngles = {transform.localRotation.eulerAngles}",
                    $"{nameof(groundTransform)} = {groundTransform}",
                    $"{nameof(movingTransforms)}{((movingTransforms != null) ? ($".Length = {movingTransforms.Length}") : (" = null"))}",
                    $"{nameof(previouslyAttachedTransformIndex)} = {previouslyAttachedTransformIndex}",
                    $"{nameof(linkedStation)}{((linkedStation != null) ? ($".PlayerMobility = {linkedStation.PlayerMobility}") : ("= null"))}",
                    $"{nameof(NUMovementSyncModLink)} = {NUMovementSyncModLink}",
                    $"{nameof(OnOwnerSetRan)} = {OnOwnerSetRan}",
                };

                return returnString;
            }
        }

        //Events
        private void Start()
        {
            Setup();
        }

        //public override void PostLateUpdate()
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Home))
            {
                Debug.Log(DebugStateCollector.ConvertStringArrayToText(DebugText));
                //DebugStateCollector.WriteStringArrayToConsoleIndividually(DebugText);
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

                    smoothHeadingDeg = Mathf.SmoothDampAngle(smoothHeadingDeg, syncedLocalPlayerHeadingDeg, ref previousPlayerAngularVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

                    transform.rotation = Quaternion.Euler(0, smoothHeadingDeg + GetParentHeadingDeg(), 0);

                    //transform.localRotation = Quaternion.Euler(0, smoothHeadingDeg, 0);
                }
            }
        }

        float GetParentHeadingDeg()
        {
            //float parentHeading = transform.parent.rotation.eulerAngles.y;
            Vector3 parentForward = movingTransforms[attachedTransformIndex].transform.rotation * movingTransforms[attachedTransformIndex].localForwardDirection;
            return Mathf.Atan2(-parentForward.x, parentForward.z) * Mathf.Rad2Deg;
        }

        public void LocalPlayerAttachedToTransform(Transform newTransform, int index)
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

        public override void _OnOwnerSet()
        {
            Setup();

            Debug.Log("_OnOwnerSet received");

            movingTransforms = NUMovementSyncModLink.MovingTransforms;

            if (Owner.isLocal)
            {
                linkedStation.PlayerMobility = VRCStation.Mobility.Mobile;

                Debug.Log("_OnOwnerSet Attaching sent");
                NUMovementSyncModLink.AttachStation(this, linkedStation);

                myStation = true;
            }
            else
            {
                //linkedStation.PlayerMobility = VRCStation.Mobility.Immobilize;
                myStation = false;
            }

            OnOwnerSetRan = true;
        }

        public override void _OnCleanup()
        {
            linkedStation.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
        }

        //VRChat functions
        public override void OnPreSerialization()
        {
            nextSerializationTime = Time.timeSinceLevelLoad + timeBetweenSerializations;

            if (!OnOwnerSetRan) return;

            if (attachedTransformIndex != -1)
            {
                syncedLocalPlayerPosition = transform.localPosition;
                syncedLocalPlayerHeadingDeg = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation.eulerAngles.y - GetParentHeadingDeg();
                
                //syncedLocalPlayerHeadingDeg = transform.localRotation.eulerAngles.y;
            }
        }
        
        public override void OnDeserialization()
        {
            if (!OnOwnerSetRan) return;

            if (previouslyAttachedTransformIndex != attachedTransformIndex)
            {
                previouslyAttachedTransformIndex = attachedTransformIndex;

                if (attachedTransformIndex == -1)
                {
                    linkedStation.PlayerMobility = VRCStation.Mobility.Mobile;
                }
                else
                {
                    transform.parent = movingTransforms[attachedTransformIndex].transform;
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