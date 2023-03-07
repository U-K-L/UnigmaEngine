using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SceneThreadHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        //LogData();
    }

    void LogData()
    {
        string[] keys = DetectionThreadManager.AllThreads.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            //Debug.Log(DetectionThreadManager.AllThreads[keys[i]]);
        }

        foreach (ConeDectionThread detector in DetectionThreadManager.threads)
        {
            //Debug.Log(detector.entities);
            string[] conekeys = detector.data.Keys.ToArray();
            for (int i = 0; i < conekeys.Length; i++)
            {
                string key = conekeys[i];
                //Debug.Log(key);
                //Debug.Log(detector.data[key].hits);
                //Debug.Log(detector.coneDetectors[key].name);
                //Debug.Log(detector.coneDetectors[key].data.transform);
                //Debug.Log(detector.coneDetectors[key].data.endPoint);
                detector.coneDetectors[key].data.objectsInView = detector.data[key].objectsInView;
                for (int j = 0; j < detector.data[key].objectsInView.Count; j++)
                {

                    foreach (RaycastHit obj in detector.data[key].objectsInView)
                    {
                        Debug.Log(obj.transform.name + " In View");
                    }
                }
            }
        }
    }
}
