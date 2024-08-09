using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UnigmaSettings
{
    private static bool RTXEnabled = false; //User choice.
    private static bool RayTracingOn = false; //User choice.

    public enum QualityPreset
    {
        High,
        Mid,
        Low
    }

    public static QualityPreset QualityPresets;

    public static bool SetRaytracing()
    {
        if (SystemInfo.supportsRayTracing)
        {
            RTXEnabled = true;
            RayTracingOn = true;
            return true;

        }
        RayTracingOn = true;
        return false;
    }


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

    public static string CurrentPreset()
    {
        Debug.Log("The current string being processed: " + Enum.GetName(typeof(QualityPreset), QualityPresets));
        return Enum.GetName(typeof(QualityPreset), QualityPresets);
    }

    public static void Initialize()
    {
        ScreenResolution();
        FrameRateLock();
    }

    static void FrameRateLock()
    {
        Application.targetFrameRate = 30;
    }

    static void ScreenResolution()
    {

        if (EditorApplication.isPlaying)
        {
            if (Screen.width == 640)
                QualityPresets = QualityPreset.Low;
            if (Screen.width == 1024)
                QualityPresets = QualityPreset.Mid;
            if (Screen.width == 2048)
                QualityPresets = QualityPreset.High;
        }

        //If in a build.
        if (QualityPresets == QualityPreset.Low)
            Screen.SetResolution(640, 640, true);
        if (QualityPresets == QualityPreset.Mid)
            Screen.SetResolution(1024, 1024, true);
        if (QualityPresets == QualityPreset.High)
            Screen.SetResolution(2048, 2048, true);

    }

}
