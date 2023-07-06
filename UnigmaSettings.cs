using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnigmaSettings
{
    private static bool RTXEnabled = false; //User choice.
    
    public static bool GetIsRTXEnabled()
    {
        if (SystemInfo.supportsRayTracing && RTXEnabled)
        {
            return true;
        }
        return false;
    }
}
