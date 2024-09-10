using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicalOcculusion : MonoBehaviour
{
    //List of objects within this buffer to be tested for occulusion.
    public List<GameObject> wallObjects = new List<GameObject>();
    public List<GameObject> objectsToView = new List<GameObject>(); //Objects we are trying to see. Player is added in this by default, but could add more.
    public void Start()
    {
        SetUpWallsAndObjects();
    }

    public void Update()
    {
        CheckWallOcculudeObjects();
    }

    void SetUpWallsAndObjects()
    {
        //Add any object with a wall tag to _graphicalOcculusion.wallObjects list.
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        foreach (GameObject wall in walls)
        {
            wallObjects.Add(wall);
        }
        //Do the same for players
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            objectsToView.Add(player);
        }
        //Do the same for pivots
        GameObject[] pivots = GameObject.FindGameObjectsWithTag("PivotPoint");
        foreach (GameObject pivot in pivots)
        {
            objectsToView.Add(pivot);
        }
        GameObject[] ceilings = GameObject.FindGameObjectsWithTag("Ceiling");
        foreach (GameObject ceiling in ceilings)
        {
            //Get _StencilRef from isometricDepth.
            int stencilValue = ceiling.GetComponent<Renderer>().material.GetInt("_StencilRef");
            ceiling.GetComponent<IsometricDepthNormalObject>().material.SetInt("_StencilRef", stencilValue);
            ceiling.GetComponent<OutlineColor>()._originalMaterial.SetInt("_StencilRef", stencilValue);
            //ceiling.GetComponent<IsometricDepthNormalObject>().materials["FluidPositions"].SetInt("_StencilRef", stencilValue);
        }

    }

    public void CheckWallOcculudeObjects()
    {
        foreach (GameObject wall in wallObjects)
        {
            //Clear all.
            ChangeStencilBuffer(0, wall);
        }
        //For each object in view, check if a wall is in front.
        foreach (GameObject obj in objectsToView)
        {
            IsWallInFront(obj);
        }
    }

    private void IsWallInFront(GameObject obj)
    {
        //Get player to camera direction.
        Vector3 direction = Vector3.Normalize(obj.transform.position - Camera.main.transform.position);
        

        //Do a large sphere cast to get all possible objects.
        RaycastHit[] hits = Physics.SphereCastAll(Camera.main.transform.position, 1.0f, gameObject.transform.forward, 100);

        float distancePlayerToCamera = Vector3.Distance(obj.transform.position, Camera.main.transform.position);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.tag == "PivotPoint" || hit.collider.gameObject.tag == "Player")
            {
                distancePlayerToCamera = hit.distance;
            }
        }


        //Check if any of the objects are a wall.
        foreach (RaycastHit hit in hits)
        {
            //Check tag to see if it is a Wall.
            if (hit.collider.gameObject.tag == "Wall")
            {
                //Checks to see if distance from this wall to camera is less than the object to camera.
                //float distanceWallToCamera = Vector3.Distance(hit.point, Camera.main.transform.position);
                if (hit.distance < distancePlayerToCamera)
                    ChangeStencilBuffer(1, hit.collider.gameObject);
            }
        }
    }

    public void ChangeStencilBuffer(int value, GameObject wall)
    {
        wall.GetComponent<MeshRenderer>().material.SetInt("_StencilRef", value);
        IsometricDepthNormalObject isometricDepthNormals = wall.GetComponent<IsometricDepthNormalObject>();
        if (isometricDepthNormals != null)
        {
            isometricDepthNormals.material.SetInt("_StencilRef", value);
            //isometricDepthNormals.materials["FluidPositions"].SetInt("_StencilRef", value);
        }

        OutlineColor outlineColors = wall.GetComponent<OutlineColor>();
        if (outlineColors != null)
        {
            outlineColors._originalMaterial.SetInt("_StencilRef", value);
        }
    }
}
