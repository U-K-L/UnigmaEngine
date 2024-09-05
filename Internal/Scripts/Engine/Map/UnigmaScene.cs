using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnigmaEngine
{
    public class UnigmaScene : MonoBehaviour
    {
        public Transform PivotPoint; //Point that is center for camera.
        public string sceneName;
        public bool loaded = false;
        public UnigmaGameObject[] unigmaGameObjects;

        //Space Time attributes.
        public Vector3 SpaceTimeBoxSize;
        public int SpaceTimeResolution;
        public float Temperature;

        //Graphical Settings.
        public UnigmaSceneGraphicalSettings daySettings;
        public UnigmaSceneGraphicalSettings nightSettings;

        [HideInInspector]
        public UnigmaSceneGraphicalSettings currentGraphicalSettings;

        public void LaunchScene()
        {
            loaded = true;
        }

        public void LoadScene()
        {
            UpdateGraphicalSettings();
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
                for (uint i = 0; i < uObjs.Length; i++)
                {
                    uObjs[i].id = i;
                    unigmaGameObjects[i] = uObjs[i];
                    unigmaGameObjects[i].unigmaGameObject.objectId = i;
                }
            }
        }

        void UpdateGraphicalSettings()
        {
            currentGraphicalSettings = daySettings;
        }
    }
}
