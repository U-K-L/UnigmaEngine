using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCursorEggGame : MonoBehaviour
{
    // Start is called before the first frame update
    Vector3 _position;
    public GameObject cursor;
    private Dictionary<string, AgentPhysics> selectedObjects; //All selected objects.
    public Camera _cam;
    private enum DrawingState { Idle, Drawing, Selecting, Menu, Following }
    private DrawingState drawingState;
    public float ScreenPlane;

    //Variables for keeping track of cursor in 3D.
    Vector3 normalHit;
    Vector3 cursorPosition;
    Vector3 rayCastPoint;
    Vector3 rayCastHit;
    //The object cursor points to.
    GameObject cursorPointTo;
    void Start()
    {
        _cam = Camera.main;
        selectedObjects = new Dictionary<string, AgentPhysics>();
        drawingState = DrawingState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCursorPosition();
        DefineCursorSettings();
    }

    void UpdateCursorPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z += _cam.nearClipPlane;
        rayCastPoint = getCursorPosition(mousePos + Camera.main.transform.position);
        mousePos = _cam.ScreenToWorldPoint(mousePos);
        cursor.transform.position = mousePos + Camera.main.transform.position;
        
    }

    private void DefineCursorSettings()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    public void addObjectToList(AgentPhysics ped)
    {
        if (selectedObjects.ContainsKey(ped.ToString()))
            return;
        selectedObjects.Add(ped.ToString(), ped);
        if (selectedObjects.ContainsKey(ped.ToString()))
        {
            GlowObject glow = ped.GetComponent<GlowObject>();
            if (glow == null)
                glow = ped.GetComponentInChildren<GlowObject>();
            if (glow)
            {
                glow.ActiveGlow = true;
                glow.enabled = true;
            }
        }

    }

    public void removeObjectFromList(AgentPhysics ped)
    {
        Debug.Log("Removing object from list");
        selectedObjects.Remove(ped.ToString());
        if (!selectedObjects.ContainsKey(ped.ToString()))
        {
            GlowObject glow = ped.GetComponent<GlowObject>();
            if (glow == null)
                glow = ped.GetComponentInChildren<GlowObject>();
            if (glow)
            {
                glow.ActiveGlow = false;
                glow.enabled = true;
            }
        }

    }

    public void removeAllObjectFromList()
    {
        Debug.Log("Removing all objects from list");
        foreach (KeyValuePair<string, AgentPhysics> Entry in selectedObjects)
        {
            AgentPhysics ped = Entry.Value;
            GlowObject glow = ped.GetComponent<GlowObject>();
            if (glow)
            {
                glow.ActiveGlow = false;
                glow.enabled = true;
            }
        }
        selectedObjects.Clear();

    }

    //Used to get 2D cursor into 3D.
    //Also obtains the game object cursor points to.
    Vector3 getCursorPosition(Vector3 screenPoint)
    {
        Vector2 offset = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.y);
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        ray.origin += Camera.main.transform.position;

        Debug.DrawRay(ray.origin+ Camera.main.transform.position, ray.direction * 10000, Color.yellow);
        int layerMask = 0;
        layerMask = 1 << 19;
        layerMask = ~layerMask;
        if (drawingState == DrawingState.Drawing)
        {
            layerMask = 1 << 10;
            layerMask = 1 << 17;
            layerMask = ~layerMask;
        }

        if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit raycastHit, 99999f, layerMask))
        {
            normalHit = raycastHit.normal;
            cursorPosition = raycastHit.point + (raycastHit.normal * 0.3f);
            rayCastHit = raycastHit.point;
            cursorPointTo = raycastHit.transform.gameObject;
            return raycastHit.point;
        }
        return Vector3.negativeInfinity;
    }

    public Dictionary<String, AgentPhysics> getSelectedObjects()
    {
        return selectedObjects;
    }

    public Vector3 GetRayCastedPoint()
    {
        return rayCastPoint;
    }

    public GameObject GetCursorPointsToObject()
    {
        return cursorPointTo;
    }

    public Vector3 GetRayCastedHit()
    {
        return rayCastHit;
    }
}
