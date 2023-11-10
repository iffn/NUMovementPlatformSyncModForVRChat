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
        Purpose:
        - Implement NUMovement (which teleports the player) with the correct world interfaces
            - Stations
            - Disable NUVovement (For example for custom swimming system)
        - Detect the attached transform and inform the station controller
        - Put local player into the station after each teleport if the attached transform recommends it

        Remaining issues:
        - VR: Playspace sometimes rotates relative to the platform the player is standing on when turning their head
        */

        //Variable definitions
        [SerializeField] float airTimeBeforePlatformDetach = 2;
        [SerializeField] PlayerColliderController[] movingTransforms;

        public PlayerColliderController[] MovingTransforms
        {
            get
            {
                return movingTransforms;
            }
            set
            {
                movingTransforms = value;
            }
        }

        public StationController LinkedStationController { get; private set; }
        VRCStation linkedStation;
        PlatformState currentPlatformState = PlatformState.InAir;
        int attachedTransformIndex = -1;
        Transform previouslyAttachedTransform;
        PlayerColliderController attachedPlayerCollider;
        bool isInVR;
        VRCPlayerApi.TrackingDataType teleportTrackingDataType;
        float detachTime;

        //Setup and debug
        public void AttachStation(StationController linkedStationController, VRCStation linkedStation)
        {
            Debug.Log("Station attached");

            this.LinkedStationController = linkedStationController;
            this.linkedStation = linkedStation;
        }

        public string[] DebugText
        {
            get
            {
                string[] returnString = new string[]
                {
                    $"Debug of {nameof(NUMovementPlatformSyncMod)}",
                    $"{nameof(linkedStation)} = {(linkedStation == null ? "null" : linkedStation.name)}",
                    $"{nameof(movingTransforms)} = {((movingTransforms != null) ? ".length +" + movingTransforms.Length.ToString() : "null")}",
                    $"{nameof(LinkedStationController)} = {(LinkedStationController == null ? "null" : LinkedStationController.name)}",
                    $"{nameof(previouslyAttachedTransform)} = {(previouslyAttachedTransform == null ? "null" : previouslyAttachedTransform.name)}",
                    $"{nameof(attachedTransformIndex)} = {attachedTransformIndex}",
                    $"{nameof(currentPlatformState)} = {currentPlatformState}", //ToCheck: enum to string in U#
                };
                
                return returnString;
            }
        }

        /*
        private void Start() //Use protected override void ControllerStart() instead
        {
            
        }
        */

        //Runtime
        private void Update()
        {
            if (currentPlatformState == PlatformState.Disabled || currentPlatformState == PlatformState.Immobilized) return;

            if (LinkedStationController == null) return;

            if (previouslyAttachedTransform != GroundTransform)
            {
                previouslyAttachedTransform = GroundTransform;

                if (GroundTransform) attachedPlayerCollider = GroundTransform.GetComponent<PlayerColliderController>();
                else GroundTransform = null;

                OnGroundChange();
            }
        }

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

            LinkedStationController.LocalPlayerDetachedFromTransform();
            attachedTransformIndex = -1;
        }

        void OnGroundChange()
        {
            if (GroundTransform == null)
            {
                currentPlatformState = PlatformState.InAir;

                if(attachedTransformIndex >= 0)
                {
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
            if (attachedPlayerCollider)
            {
                attachedTransformIndex = attachedPlayerCollider.PlatformIndex;
            }
            else
            {
                if (attachedTransformIndex < 0) return; //Ignore if already not attached
                LinkedStationController.LocalPlayerDetachedFromTransform();
                attachedTransformIndex = -1;
            }
        }

        // NUMovement stuff
        protected override void ControllerStart()
        {
            base.ControllerStart();

            isInVR = Networking.LocalPlayer.IsUserInVR();
            teleportTrackingDataType = isInVR ? VRCPlayerApi.TrackingDataType.AvatarRoot : VRCPlayerApi.TrackingDataType.Origin;

            for (int i = 0; i < movingTransforms.Length; i++)
            {
                movingTransforms[i].Setup(i);
            }
        }

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

            if (attachedPlayerCollider && attachedPlayerCollider.shouldSyncPlayer)
            {
                linkedStation.transform.SetPositionAndRotation(
                    Networking.LocalPlayer.GetTrackingData(teleportTrackingDataType).position,
                    Networking.LocalPlayer.GetTrackingData(teleportTrackingDataType).rotation);

                linkedStation.UseStation(Networking.LocalPlayer);
            }
        }

        //Implement friction
        protected override void ApplyHit(ControllerColliderHit hit)
        {
            Vector3 normal = hit.normal;
            Vector3 point = hit.point;

            Vector3 position = transform.position;
            Vector3 center = Controller.center;

            float capOffset = Controller.height * 0.5f - Controller.radius;
            Vector3 bottomCapCenter = position + ControllerDown * capOffset + center;
            Vector3 topCapCenter = position + ControllerUp * capOffset + center;

            if (Vector3.Dot(Velocity.normalized, normal) < 0f)
            {
                if (Vector3.Dot(point - bottomCapCenter, ControllerDown) >= 0f)
                {
                    IsGrounded = true;
                    GroundUp = normal;
                    if (hit.transform)
                    {
                        GroundTransform = hit.transform;
                        RelativeGroundPosition = GroundTransform.InverseTransformPoint(position);
                        GroundRotation = GroundTransform.rotation;
                    }

                    //PhysicMaterial colliderMaterial = hit.collider.material;
                    PhysicMaterial colliderMaterial = hit.collider.material;

                    float normalLimit;

                    if (colliderMaterial.name.Length == 0)
                    {
                        normalLimit = 0.707f; //45°
                    }
                    else
                    {
                        /*
                        Friction formulas:
                        ------------------
                        α = acos(Ny)
                            = atan(µ)
                        
                        Ny = cos(α)
                            = cos(atan(µ))
                        
                        µ = tan(α)
                            = tan(acos(Ny)

                        α = slope angle
                        Ny = Normalized normal y
                        µ = Friction coefficient

                        */

                        normalLimit = Mathf.Cos(Mathf.Atan(colliderMaterial.staticFriction));
                    }

                    if (normal.normalized.y < normalLimit)
                    {
                        IsSteep = true;
                    }
                    else
                    {
                        IsWalkable = true;
                        if (!jumpAuto)
                        {
                            HoldJump = false; // Consume.
                        }
                    }
                }
                else if (Vector3.Dot(point - topCapCenter, ControllerUp) >= 0f)
                {
                    IsJumping = false;
                }

                if (IsWalkable)
                {
                    Velocity = HoldMove || HoldJump ? Vector3.ProjectOnPlane(Velocity, -GravityDirection) : Vector3.zero;
                }
                else
                {
                    Velocity = Vector3.ProjectOnPlane(Velocity, normal);
                }
            }

            Color debugColor = IsGrounded ? (!IsSteep ? Color.green : Color.yellow) : Color.red;
            DrawLine(bottomCapCenter, point, debugColor);
            DrawCircle(point, normal, 0.25f, debugColor);
        }
    }
}