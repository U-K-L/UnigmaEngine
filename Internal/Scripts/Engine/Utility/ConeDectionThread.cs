using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using UnityEditor;
using System.Linq;

public class ConeDectionThread
{
    public bool running = false;
    public Thread thread;
    public bool objInPeriph;
    public Vector3 endPoint;
    public Ray ray;
    public float radius;
    public RaycastHit[] hits;
    public HashSet<RaycastHit> objectsInView;
    public HashSet<GameObject> GameObjectsInReach;
    public bool objInView;
    public Transform transform;
    public float touchRadius;
    public ConeDetectionMultithreaded detector;
    public Dictionary<string,ConeDetectionMultithreaded> coneDetectors = new Dictionary<string, ConeDetectionMultithreaded>();

    public string entities;
    public class DetectionData
    {
        public bool objInPeriph;
        public Vector3 endPoint;
        public Ray ray;
        public float radius;
        public RaycastHit[] hits;
        public HashSet<RaycastHit> objectsInView;
        public HashSet<GameObject> GameObjectsInReach;
        public bool objInView;
        public Transform transform;
        public float touchRadius;
    }
    public Dictionary<string, DetectionData> data = new Dictionary<string, DetectionData>();
    System.Random rnd = new System.Random();
    public void Start()
    {
        running = true;
        thread = Thread.CurrentThread;
        double rand = rnd.NextDouble() * 500.0;//(int)Mathf.Floor(UnityEngine.Random.value * 500f);
        Thread.Sleep(500 + (int)rand);
        Running();
    }

    void Running()
    {
        
        while (running)
        {
            
            entities = "";
            Update();
            double rand = rnd.NextDouble() * 5.0;
            Thread.Sleep(30 + (int)rand);
        }
        
    }

    public void Update()
    {

        string[] keys = coneDetectors.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            ConeDetectionMultithreaded detect = coneDetectors[key];
            ConeDetectionMultithreaded.DetectionData detectData = detect.data;
            SetDetection(detect.Conename, ref detectData.objInPeriph, ref detectData.endPoint, ref detectData.ray, 
                ref detectData.radius, ref detectData.hits, ref detectData.objectsInView, ref detectData.GameObjectsInReach, ref detectData.objInView, detectData.transform, ref detectData.touchRadius);
            if (data[detect.Conename].hits != null)
                GetDetection(detect.Conename, data[detect.Conename].objInPeriph, data[detect.Conename].endPoint, data[detect.Conename].ray, data[detect.Conename].radius,
                    data[detect.Conename].hits, data[detect.Conename].objectsInView, data[detect.Conename].GameObjectsInReach, data[detect.Conename].objInView, data[detect.Conename].transform,
                    data[detect.Conename].touchRadius);
            //Debug.Log(DetectionThreadManager.AllThreads[keys[i]]);
            double rand = rnd.NextDouble() * 2.0;
            Thread.Sleep((int)rand);
        }
        /*
        int len = coneDetectors.Count;
        for(int i = 0; i < len; i++)
        {
            //Debug.Log(coneDetectors[i].name +" " + thread.Name);
            //DetectionThreadManager.AllThreads
            
            entities += coneDetectors[i].name + " " + thread.Name + "\n";
        }
        if (DetectionThreadManager.AllThreads.ContainsKey(thread.Name))
        {
            DetectionThreadManager.AllThreads[thread.Name] = entities;
        }
        else
            DetectionThreadManager.AllThreads.Add(thread.Name, entities);
        */
    }

    public void SetDetection(string key, ref bool objInPeriphs, ref Vector3 endPoints, ref Ray rays, ref float radiuss, ref RaycastHit[] hitss,
                         ref HashSet<RaycastHit> objectsInViews, ref HashSet<GameObject> GameObjectsInReachs, ref bool objInViews, Transform transforms, ref float touchRadiuss)
    {
        if (!data.ContainsKey(key))
            data.Add(key, new DetectionData());

        data[key].objInPeriph = objInPeriphs;
        data[key].endPoint = endPoints;
        data[key].ray = rays;
        data[key].radius = radiuss;
        data[key].hits = hitss;
        data[key].objectsInView = objectsInViews;
        data[key].GameObjectsInReach = GameObjectsInReachs;
        data[key].objInView = objInViews;
        data[key].transform = transforms;
        data[key].touchRadius = touchRadiuss;
    }
    public void GetDetection(string key, bool objInPeriph,  Vector3 endPoint, Ray ray,  float radius,  RaycastHit[] hits,
                              HashSet<RaycastHit> objectsInView,  HashSet<GameObject> GameObjectsInReach,  bool objInView, Transform transform,  float touchRadius)
    {
        //Debug.Log(Thread.CurrentThread.ManagedThreadId);
        if (data[key].objectsInView == null)
            return;
        data[key].objectsInView.Clear();
        foreach (RaycastHit hit in hits)
        {

            objInPeriph = true;
            float height = Vector3.Magnitude(endPoint);

            //Projects the hit point onto the main axis of the cone.
            float axisDist = Vector3.Dot(hit.point - ray.origin, ray.direction);

            //Orthogonal distance from the axis.
            float orthoDist = Vector3.Magnitude((hit.point - ray.origin) - (axisDist * ray.direction));

            //The radius of the cone at this point.
            float current_Radius = (axisDist / height) * radius;

            if (orthoDist < current_Radius)
            {
                /*
                if (hit.transform.gameObject != transform.gameObject)
                    if (Vector3.Distance(hit.transform.position, transform.position) > touchRadius)
                        objectsInView.Add(hit);
                */
                data[key].objectsInView.Add(hit);
            }
            /*
            else if(Vector3.Distance(hit.transform.position, transform.position) < touchRadius)
            {
                if (hit.transform.gameObject != transform.gameObject)
                    GameObjectsInReach.Add(hit.transform.gameObject);
            }
            */
            else
            {
                data[key].objectsInView.Remove(hit);
            }
        }
    }

    public void LogThreadName(string name)
    {
        Debug.Log(name + " On Thread" + thread.Name);
    }

}
