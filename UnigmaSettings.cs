using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnigmaSettings
{
    private static bool RTXEnabled = false; //User choice.
    private static bool RayTracingOn = true;

    public static bool GetIsRTXEnabled()
    {
        if (SystemInfo.supportsRayTracing && RTXEnabled && GetIsRayTracingEnabled())
        {
            return true;
            
        }
        return false;
    }

    public static bool GetIsRayTracingEnabled()
    {
        if (SystemInfo.supportsComputeShaders && RayTracingOn)
        {
            return true;
        }
        return false;
    }

}
