using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using UnityEditor;

public class ConeDetectionMultithreaded : MonoBehaviour
{
    // Start is called before the first frame update
    [HideInInspector]
    public string Conename = "obj";
    public float distance = 7f;
    public int points = 10;
    public float radius = 1f;
    public float touchRadius = 0.25f;
    Color color;
    public Vector3 _mConeRotation = Vector3.zero;
    bool objInView; //Determines if an object exists in immediate view.
    bool objInPeriph; //Determines if object in peripheral view
    public HashSet<RaycastHit> objectsInView;
    [Header("Detection Settings")]
    [Tooltip("Determines if objects in front blocks objects behind.")]
    public bool obscructedView = true;
    [Tooltip("Determines if the rotation of the game object affects the cone's rotation.")]
    public bool rotateWithParent = true;
    public LayerMask layermask = 1;
    public Dictionary<string, IntelligentObject> GameObjectsInCone;
    public HashSet<IntelligentObject> GameObjectsInReach; //For touch radius.

    ConeDectionThread coneDetectionThread;

    private bool occuludedChecksDone = true;
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

    public DetectionData data = new DetectionData();
    void Start()
    {
        data.objectsInView = new HashSet<RaycastHit>();
        Conename = transform.name;
        coneDetectionThread = new ConeDectionThread();

        DetectionThreadManager.CreateDetectionThread(coneDetectionThread, this, Conename);
        GameObjectsInReach = new HashSet<IntelligentObject>();
        GameObjectsInCone = new Dictionary<string, IntelligentObject>();
        objectsInView = new HashSet<RaycastHit>();
        color = Color.red;
        StartCoroutine(getCollisionData());
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        

    }

    IEnumerator getCollisionData()
    {
        while (this.enabled)
        {
            float rand = UnityEngine.Random.value * 0.04f;
            yield return new WaitForSeconds(0.02f + rand);
            if (GameObjectsInReach != null)
                GameObjectsInReach.Clear();
            FindCollision();
            if (obscructedView)
            {
                if (occuludedChecksDone)
                {
                    occuludedChecksDone = false;
                    StartCoroutine(ObstructionDetection());
                    
                }
            }
                
        }
    }

    void FindCollision()
    {
        //Create the axis of the ray.
        Vector3 rotation = _mConeRotation;
        if (rotateWithParent)
            rotation += Quaternion.Euler(transform.rotation.eulerAngles) * Vector3.forward;
        Ray ray = new Ray(transform.position, rotation);
        Vector3 endPoint = ray.direction * distance + (ray.direction * (radius * 2));

        //We want to go half a sphere ahead so that it doesn't detect behind.
        Vector3 origin = ray.origin - (ray.direction * (radius));
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, ray.direction, distance * radius, layermask);
        if (hits.Length > 1)
        {
            data.objInPeriph = objInPeriph;
            data.endPoint = endPoint;
            data.ray = ray;
            data.radius = radius;
            data.hits = hits;
            //data.objectsInView = objectsInView;
            objectsInView = data.objectsInView;
            //data.GameObjectsInReach = GameObjectsInReach;
            data.objInView = objInView;
            data.transform = transform;
            data.touchRadius = touchRadius;
            


            //coneDetectionThread.SetDetection(Conename, ref objInPeriph, ref endPoint, ref ray, ref radius, ref hits, ref objectsInView, ref GameObjectsInReach, ref objInView, gameObject.transform, ref touchRadius);
            /*
            foreach (RaycastHit hit in hits)
            {

                objInPeriph = true;
                //point.transform.position = hit.point;
                float height = Vector3.Magnitude(endPoint);

                //Projects the hit point onto the main axis of the cone.
                float axisDist = Vector3.Dot(hit.point - ray.origin, ray.direction);

                //Orthogonal distance from the axis.
                float orthoDist = Vector3.Magnitude((hit.point - ray.origin) - (axisDist * ray.direction));

                //The radius of the cone at this point.
                float current_Radius = (axisDist / height) * radius;

                if (orthoDist < current_Radius)
                {
                    objInView = true;
                    if (hit.transform.gameObject != transform.gameObject)
                        if (Vector3.Distance(hit.transform.position, transform.position) > touchRadius)
                            objectsInView.Add(hit);

                }
                else if(Vector3.Distance(hit.transform.position, transform.position) < touchRadius)
                {
                    if (hit.transform.gameObject != transform.gameObject)
                        GameObjectsInReach.Add(hit.transform.gameObject);
                }
                else
                {
                    objectsInView.Remove(hit);
                }
            }
        */
        }
        if (objectsInView != null)
        {
            if (objectsInView.Count <= 0)
            {
                objInView = false;

            }else
                objInView = true;
        }

    }


    Vector3 ExpandFromCenter(Vector3 p, Vector3 c, float dist)
    {
        return p + (p - c).normalized * dist;
    }

    void checkConeRaycast(Vector3 goThroughPoint, RaycastHit hit)
    {
        float dist = 100000f;
        Ray ray = new Ray(transform.position, (goThroughPoint - transform.position).normalized);
        RaycastHit hit2;

        Vector3 rotation = _mConeRotation;
        if (rotateWithParent)
            rotation += Quaternion.Euler(transform.rotation.eulerAngles) * Vector3.forward;
        Ray original_ray = new Ray(transform.position, rotation);

        if (Physics.Raycast(ray, out hit2, dist, layermask))
        {

            Vector3 endPoint = original_ray.direction * distance + (original_ray.direction * (radius * 2));

            //point.transform.position = hit.point;
            float height = Vector3.Magnitude(endPoint);

            //Projects the hit point onto the main axis of the cone.
            float axisDist = Vector3.Dot(hit2.point - original_ray.origin, original_ray.direction);

            //Orthogonal distance from the axis.
            float orthoDist = Vector3.Magnitude((hit2.point - original_ray.origin) - (axisDist * original_ray.direction));

            //The radius of the cone at this point.
            float current_Radius = (axisDist / height) * radius;

            if (orthoDist < current_Radius)
            {
                

                objInView = true;
                if (!GameObjectsInCone.ContainsKey(hit2.transform.gameObject.name))
                {
                    if (hit2.transform.gameObject != transform.gameObject)
                    {
                        IntelligentObject IObj = hit2.transform.gameObject.GetComponent<IntelligentObject>();
                        if (IObj != null)
                        {
                            lock (GameObjectsInCone)
                                GameObjectsInCone.Add(IObj.Iname, IObj);
                        }
                    }
                }

            }
        }
    }


   IEnumerator ObstructionDetection()
    {
        float rand = UnityEngine.Random.value * 0.01f;
        HashSet<RaycastHit> objsSetCopy = new HashSet<RaycastHit>(objectsInView);
        lock (GameObjectsInCone)
            GameObjectsInCone.Clear();
        if (objectsInView != null)
        {

            foreach (RaycastHit hit in objsSetCopy)
            {
                
                Vector3 center = hit.collider.bounds.center;
                Vector3 max = hit.collider.bounds.max;
                Vector3 min = hit.collider.bounds.min;

                Vector3 Edge1 = new Vector3(max.x, max.y, min.z);
                Vector3 Edge2 = new Vector3(max.x, min.y, min.z);
                Vector3 Edge3 = new Vector3(max.x, min.y, max.z);

                Vector3 Edge4 = new Vector3(min.x, max.y, max.z);
                Vector3 Edge5 = new Vector3(min.x, min.y, max.z);
                Vector3 Edge6 = new Vector3(min.x, max.y, min.z);

                Vector3 Plane1 = new Vector3(center.x, min.y, center.z);
                Vector3 Plane2 = new Vector3(center.x, max.y, center.z);
                Vector3 Plane3 = new Vector3(min.x, center.y, center.z);
                Vector3 Plane4 = new Vector3(max.x, center.y, center.z);

                Vector3 check1 = ExpandFromCenter(max, center, 0.2f);
                Vector3 check2 = ExpandFromCenter(Edge1, center, 0.2f);
                Vector3 check3 = ExpandFromCenter(Edge2, center, 0.2f);
                Vector3 check4 = ExpandFromCenter(Edge3, center, 0.2f);
                
                Vector3 check5 = ExpandFromCenter(Edge4, center, 0.2f);
                Vector3 check6 = ExpandFromCenter(Edge5, center, 0.2f);
                Vector3 check7 = ExpandFromCenter(Edge6, center, 0.2f);
                Vector3 check8 = ExpandFromCenter(min, center, 0.2f);
                Vector3 check9 = ExpandFromCenter(Plane1, center, 0.2f);
                Vector3 check10 = ExpandFromCenter(Plane2, center, 0.2f);
                Vector3 check11 = ExpandFromCenter(Plane3, center, 0.2f);
                Vector3 check12 = ExpandFromCenter(Plane4, center, 0.2f);


                //Add objects from raycasting the corners.
                checkConeRaycast(center, hit);
                checkConeRaycast(check1, hit);
                checkConeRaycast(check2, hit);
                yield return new WaitForSeconds(0.02f + rand);
                checkConeRaycast(check3, hit);
                checkConeRaycast(check4, hit);
                checkConeRaycast(check5, hit);
                yield return new WaitForSeconds(0.01f + rand);
                checkConeRaycast(check6, hit);
                checkConeRaycast(check7, hit);
                checkConeRaycast(check8, hit);
                yield return new WaitForSeconds(0.02f + rand);
                checkConeRaycast(check9, hit);
                checkConeRaycast(check10, hit);
                checkConeRaycast(check11, hit);
                checkConeRaycast(check12, hit);

                
            }
            
        }
        occuludedChecksDone = true;
    }

    void OnApplicationQuit()
    {
        Debug.Log("Application ending after " + Time.time + " seconds");
        DetectionThreadManager.EndThreads();
    }


    private void OnDrawGizmos()
    {
        if (!this.enabled)
            return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, touchRadius);
        if (objInPeriph)
            color = Color.blue;
        if (objInView)
            color = Color.green;
        Gizmos.color = color;

        Vector3 rotation = _mConeRotation;
        if (rotateWithParent)
            rotation += Quaternion.Euler(transform.rotation.eulerAngles) * Vector3.forward;
        Ray ray = new Ray(transform.position, rotation);
        var origin = ray.origin;
        var endPoint = ray.direction * distance + (ray.direction * (radius * 2));
        //...............
        //We only want direction of the end point. Normalized gets us that.
        Vector3 normal = endPoint.normalized;

        //We now want a vector orthogonal to the endpoint.
        //We can do that by swapping around the components, "binormal" for now.
        Vector3 biNormal = new Vector3(normal.z, normal.x, normal.y);

        //We get the tangent, a vector that is orthogonal to the ray by taking the cross product.
        Vector3 tangent = Vector3.Cross(normal, biNormal);

        //We get the other orthogonal vector similarly.
        Vector3 biTangent = Vector3.Cross(normal, tangent);
        //And now we have round are two vectors to create our cone ANYWHERE.
        var slice = 2 * Mathf.PI / points;
        for (var i = 0; i < points; i++)
        {
            var angle = slice * i;
            //We replace change this with our tangent.
            //Our newly created vectors will be empty in all axis except those orthogonal, so we can do this now:
            Vector3 p = origin + endPoint + (tangent * Mathf.Cos(angle) * radius) + (biTangent * Mathf.Sin(angle) * radius);
            Gizmos.color = color;
            Gizmos.DrawLine(ray.origin, p);

        }

        color = Color.red;
        Gizmos.color = color;

        Vector3 ps = origin + endPoint;
        float vDist = Vector3.Distance(origin, ps);
        float radiusSize = vDist;
        Gizmos.DrawWireSphere(transform.position, radiusSize);

        if (objectsInView != null)
        {
            HashSet<RaycastHit> objsSetCopy = new HashSet<RaycastHit>(objectsInView);
            foreach (RaycastHit hit in objsSetCopy)
            {
                Vector3 center = hit.collider.bounds.center;
                if (obscructedView)
                {
                    Vector3 max = hit.collider.bounds.max;
                    Vector3 min = hit.collider.bounds.min;

                    Vector3 Edge1 = new Vector3(max.x, max.y, min.z);
                    Vector3 Edge2 = new Vector3(max.x, min.y, min.z);
                    Vector3 Edge3 = new Vector3(max.x, min.y, max.z);

                    Vector3 Edge4 = new Vector3(min.x, max.y, max.z);
                    Vector3 Edge5 = new Vector3(min.x, min.y, max.z);
                    Vector3 Edge6 = new Vector3(min.x, max.y, min.z);

                    Vector3 Plane1 = new Vector3(center.x, min.y, center.z);
                    Vector3 Plane2 = new Vector3(center.x, max.y, center.z);
                    Vector3 Plane3 = new Vector3(min.x, center.y, center.z);
                    Vector3 Plane4 = new Vector3(max.x, center.y, center.z);

                    Vector3 check1 = ExpandFromCenter(max, center, 0.2f);
                    Vector3 check2 = ExpandFromCenter(Edge1, center, 0.2f);
                    Vector3 check3 = ExpandFromCenter(Edge2, center, 0.2f);
                    Vector3 check4 = ExpandFromCenter(Edge3, center, 0.2f);
                    Vector3 check5 = ExpandFromCenter(Edge4, center, 0.2f);
                    Vector3 check6 = ExpandFromCenter(Edge5, center, 0.2f);
                    Vector3 check7 = ExpandFromCenter(Edge6, center, 0.2f);
                    Vector3 check8 = ExpandFromCenter(min, center, 0.2f);

                    Vector3 check9 = ExpandFromCenter(Plane1, center, 0.2f);
                    Vector3 check10 = ExpandFromCenter(Plane2, center, 0.2f);
                    Vector3 check11 = ExpandFromCenter(Plane3, center, 0.2f);
                    Vector3 check12 = ExpandFromCenter(Plane4, center, 0.2f);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(hit.point, 0.1f);
                    Gizmos.DrawRay(origin, hit.point - origin);

                    Gizmos.color = Color.magenta;

                    Gizmos.DrawWireSphere(center, 0.1f);
                    Gizmos.DrawWireSphere(check1, 0.1f);
                    Gizmos.DrawWireSphere(check2, 0.1f);
                    Gizmos.DrawWireSphere(check3, 0.1f);
                    Gizmos.DrawWireSphere(check4, 0.1f);
                    Gizmos.DrawWireSphere(check5, 0.1f);
                    Gizmos.DrawWireSphere(check6, 0.1f);
                    Gizmos.DrawWireSphere(check7, 0.1f);
                    Gizmos.DrawWireSphere(check8, 0.1f);

                    Gizmos.DrawWireSphere(check9, 0.1f);
                    Gizmos.DrawWireSphere(check10, 0.1f);
                    Gizmos.DrawWireSphere(check11, 0.1f);
                    Gizmos.DrawWireSphere(check12, 0.1f);
                }
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(hit.point, 0.1f);
                Gizmos.DrawRay(origin, (center - transform.position).normalized * endPoint.magnitude);

            }
        }

    }

    public void DebugVision()
    {
        //Debugs each object in objectInView
        foreach (KeyValuePair<string, IntelligentObject> obj in GameObjectsInCone)
        {
            Debug.Log(gameObject.name + " " + obj.Value.Iname + " In View");
        }
    }
}

