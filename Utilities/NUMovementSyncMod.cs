﻿using Nessie.Udon.Movement;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

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
        Transform previouslyAttachedTransform;
        PlayerColliderController attachedPlayerCollider;
        PlayerColliderController previouslyAttachedPlayerCollider;
        bool isInVR;
        VRCPlayerApi.TrackingDataType teleportTrackingDataType;
        float detachTime;
        bool revertTurn = true;

        Quaternion initialLocalPlayspaceRotation;
        Vector3 initialLocalPlayspaceDirection;

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
                    $"{nameof(movingTransforms)} length = {((movingTransforms != null) ? movingTransforms.Length.ToString() : "null")}",
                    $"{nameof(LinkedStationController)} = {(LinkedStationController == null ? "null" : LinkedStationController.name)}",
                    $"{nameof(previouslyAttachedTransform)} = {(previouslyAttachedTransform == null ? "null" : previouslyAttachedTransform.name)}",
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
            if (Input.GetKeyDown(KeyCode.Home))
            {
                Debug.Log(DebugStateCollector.ConvertStringArrayToText(DebugText));
            }

            if (currentPlatformState == PlatformState.Disabled || currentPlatformState == PlatformState.Immobilized) return;

            if (LinkedStationController == null) return;

            if (previouslyAttachedTransform != GroundTransform)
            {
                OnGroundChange();

                previouslyAttachedTransform = GroundTransform;
                previouslyAttachedPlayerCollider = attachedPlayerCollider;
            }
        }

        void OnGroundChange()
        {
            if (GroundTransform == null)
            {
                //If now in air
                currentPlatformState = PlatformState.InAir;

                if (attachedPlayerCollider != null) //Don't detach immediately to maintain smooth sync when jumping on a platform;
                {
                    detachTime = Time.time + airTimeBeforePlatformDetach;
                    SendCustomEventDelayedSeconds(nameof(DetachFromStationWhenInAirEvent), airTimeBeforePlatformDetach);
                }
            }
            else
            {
                //If now grounded
                currentPlatformState = PlatformState.Grounded;

                PlayerColliderController previouslyAttachedColliderController = attachedPlayerCollider;

                attachedPlayerCollider = GroundTransform.GetComponent<PlayerColliderController>();

                if (attachedPlayerCollider)
                {
                    //If now attached to synced station
                    LinkedStationController.LocalPlayerAttachedToTransform(attachedPlayerCollider);

                    if (previouslyAttachedPlayerCollider == null)
                    {
                        ResetInitialLocalPlayspaceDirection();
                    }
                }
                else
                {
                    //If not not attached to sync station: Notify of disconnect
                    if (previouslyAttachedColliderController)
                        LinkedStationController.LocalPlayerAttachedToTransform(null);
                }
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

            LinkedStationController.LocalPlayerAttachedToTransform(null);
            attachedPlayerCollider = null;
        }

        void ResetInitialLocalPlayspaceDirection()
        {
            if (attachedPlayerCollider == null) return;

            initialLocalPlayspaceRotation = Quaternion.Inverse(attachedPlayerCollider.transform.rotation) * LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation;
            initialLocalPlayspaceDirection = Quaternion.Inverse(attachedPlayerCollider.transform.rotation) * LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation * attachedPlayerCollider.localForwardDirection;
            initialLocalPlayspaceDirection.y = 0;
        }

        //VRChat functions
        public override void InputLookHorizontal(float value, UdonInputEventArgs args)
        {
            base.InputLookHorizontal(value, args);

            if (Mathf.Abs(value) > 0.1f)
            {
                revertTurn = false;
            }
            else
            {
                revertTurn = true;
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

            //Sync stuff
            if (attachedPlayerCollider && attachedPlayerCollider.shouldSyncPlayer)
            {
                if (isInVR)
                {
                    if (revertTurn)
                    {
                        //Fix playspace rotation bug
                        Vector3 currentLocalPlayspaceForwardDirection = Quaternion.Inverse(attachedPlayerCollider.transform.rotation) * LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation * attachedPlayerCollider.localForwardDirection;
                        float currentHeadingRad = Mathf.Atan2(currentLocalPlayspaceForwardDirection.z, currentLocalPlayspaceForwardDirection.x);
                        float oldHeadingRad = Mathf.Atan2(initialLocalPlayspaceDirection.z, initialLocalPlayspaceDirection.x);
                        float headingOffsetRad = currentHeadingRad - oldHeadingRad;
                        //Ensure that heading stays between -180° and +180°
                        if (headingOffsetRad > Mathf.PI) headingOffsetRad -= 2 * Mathf.PI;
                        if (headingOffsetRad < -Mathf.PI) headingOffsetRad += 2 * Mathf.PI;

                        Quaternion rotationOffset = Quaternion.Euler(0, headingOffsetRad * Mathf.Rad2Deg, 0);

                        LocalPlayer.TeleportTo(LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).position, rotationOffset * LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).rotation);
                    }
                    else
                    {
                        ResetInitialLocalPlayspaceDirection();
                    }
                }

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