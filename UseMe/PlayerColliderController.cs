﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NUMovementPlatformSyncMod
{
    public class PlayerColliderController : UdonSharpBehaviour
    {
        public int PlatformIndex { get; private set; }

        public bool shouldSyncPlayer = true;

        public void Setup(int index)
        {
            this.PlatformIndex = index;
        }
    }
}