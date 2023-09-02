
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using static System.TimeZoneInfo;

public class PlatformMover : UdonSharpBehaviour
{
    double transitionTime = 2;

    [SerializeField] Transform startPosition;
    [SerializeField] Transform endPosition;

    private void Update()
    {
        float lerpValue = (float)(Networking.GetServerTimeInSeconds() % (transitionTime * 2) / transitionTime);
        if (lerpValue > 1) lerpValue = 2 - lerpValue;

        transform.position = Vector3.Lerp(a: startPosition.position, b: endPosition.position, t: lerpValue);
    }
}
