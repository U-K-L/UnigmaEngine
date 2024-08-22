using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaScene : MonoBehaviour
{
    public Transform PivotPoint; //Point that is center for camera.
    public string sceneName;
    public bool loaded = false;
    public UnigmaGameObject[] unigmaGameObjects;

    public void LaunchScene()
    {
        loaded = true;
    }

    public void LoadScene()
    {
        CreateGameObjectBuffers();
    }

    public void UnloadScene()
    {
        loaded = false;
    }

    public void CreateGameObjectBuffers()
    {
        if (loaded == false)
        {
            //Find all Unigma Game Objects parented under this.
            UnigmaGameObject[] uObjs = this.GetComponentsInChildren<UnigmaGameObject>();
            unigmaGameObjects = new UnigmaGameObject[uObjs.Length];

            //Assign each their IDs and perform checks.
            for (int i = 0; i < uObjs.Length; i++)
            {
                uObjs[i].id = i;
                unigmaGameObjects[i] = uObjs[i];
            }
        }
    }
}
