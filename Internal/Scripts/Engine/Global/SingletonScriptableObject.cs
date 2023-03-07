using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
{
    private static T _instance;
    public static T GetInstance(string path)
    {
        if (_instance == null)
        {
            _instance = Resources.Load<T>(path + typeof(T).Name) as T;
            if (_instance == null)
            {
                Debug.LogError("SingletonScriptableObject<" + typeof(T).Name + "> is not found.");
            }
        }
        return _instance;
    }
}
