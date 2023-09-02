
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class movementModLinker : UdonSharpBehaviour
{
    [SerializeField] iffnsNuMovementMod linkedMovementMod;

    public iffnsNuMovementMod LinkedMovementMod
    {
        get
        {
            return linkedMovementMod;
        }
    }
}
