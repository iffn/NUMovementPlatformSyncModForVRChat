
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DebugStateCollector : UdonSharpBehaviour
{
    public static string ConvertStringArrayToText(string[] stringArray)
    {
        string returnString = "";

        foreach(string str in stringArray)
        {
            returnString += str + "\n";
        }

        return returnString;
    }
    public static void WriteStringArrayToConsoleIndividually(string[] stringArray)
    {
        foreach (string str in stringArray)
        {
            Debug.Log(str);
        }
    }

    void Start()
    {
        
    }
}
