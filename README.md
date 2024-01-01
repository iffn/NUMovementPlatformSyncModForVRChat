# iffnsNUMovementPlatformSyncModForVRChat
A system that syncs the player position relative to the station they're walking on.
 
## Requirements
Requires NUMovement by Nestorboy and CyanPlayerObjectPool by CyanPlayerObjectPool

https://github.com/Nestorboy/NUMovement

https://github.com/CyanLaser/CyanPlayerObjectPool

## How to use
- Make sure NUMovement and CyanPlayerObjectPool are added to Unity project (See Requrements, added in Packages)
- Put the MovementSytem Prefab into the Scene
- Add the PlayerColliderController script to all moving colliders where the player can stand on and make sure ShouldSyncPlayer is correctly set.
- Add the StationInformer script to each Station where people can sit on
- Link all components using the custom tool: Top of Unity window -> Tools -> iffnsStuff -> MovementModLinker. (Not yet automatic on build.)

## How it works
NUMovement teleports the player each frame. Due to the sync delay in VRChat, the remote players would appear behind the platform.
The system works by:
- Assigning a station set to Mobile to each player using Cyan's object pool
- If the local player is on a platform
  - After the teleport, moving the station to the player and put the player into the station (Each teleport kicks the player out of the station, so they always need to be put back.)
  - Syncing the current collider index as well as the local position and rotation.
- If the remote player is on a platform:
  - Setting the local position and rotation of the remote players relative to their assigned
- If the remote player is not on a platform
  - Set the station to mobile and use the VRChat sync

## Current bugs:
- Interpolation between updates not smooth (caused by VRChat, no workaround found so far)
- The large VRChat menu cannot be opened while on a platform (VRChat limitation, added ability to disable the sync temprarily)
- Entry and exit transition not smooth synced (Test again)
