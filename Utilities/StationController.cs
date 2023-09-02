using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsNUMovementPlatformSyncMod
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

        bool inStation = false;

        float smoothTime = 0.1f;

        readonly float timeBetweenSerializations = 1f / 6f;
        float nextSerializationTime = 0f;
        public bool serialize = false;

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

            if (inStation)
            {
                transform.localPosition = syncedLocalPlayerPosition;
                //transform.localPosition = Vector3.SmoothDamp(transform.localPosition, syncedLocalPlayerPosition, ref previousPlayerLinearVelocity, 0.13f, Mathf.Infinity, Time.deltaTime);
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, syncedLocalPlayerPosition, ref previousPlayerLinearVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
                transform.localRotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(transform.localRotation.eulerAngles.y, syncedLocalPlayerHeading, ref previousPlayerAngularVelocity, smoothTime, Mathf.Infinity, Time.deltaTime), 0);

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
            movementModLinker linker = transform.parent.GetComponent<movementModLinker>();

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
            linkedStation.PlayerMobility = VRCStation.Mobility.Immobilize;
        }

        //VRChat functions
        public override void OnPreSerialization()
        {
            nextSerializationTime = Time.timeSinceLevelLoad + timeBetweenSerializations;

            if (!setupComplete) return;

            if (attachedTransformIndex != -1)
            {
                syncedLocalPlayerPosition = GroundTransform.InverseTransformPoint(Networking.LocalPlayer.GetPosition());
                syncedLocalPlayerHeading = (Quaternion.Inverse(GroundTransform.rotation) * Networking.LocalPlayer.GetRotation()).eulerAngles.y;
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

                }
                else
                {
                    linkedStation.transform.parent = movingTransforms[attachedTransformIndex];
                }
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player.isLocal) return;

            previousPlayerLinearVelocity = player.GetVelocity();
            previousPlayerAngularVelocity = 0;
            transform.SetPositionAndRotation(player.GetPosition(), player.GetRotation());
            linkedStation.PlayerMobility = VRCStation.Mobility.Immobilize;

            inStation = true;
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (player.isLocal) return;

            linkedStation.PlayerMobility = VRCStation.Mobility.Mobile;
            inStation = false;
        }
    }
}