using Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerCursor : ImmediateModeShapeDrawer
{
    // Start is called before the first frame update
    Vector3 _position;
    [Tooltip("If cursor is empty, just use mouse.")]
    public Vector3 displacement;
    private Dictionary<string, AgentPhysics> selectedObjects; //All selected objects.
    private Camera _cam;
    private enum DrawingState { Idle, Drawing, Selecting, Menu, Following }
    private DrawingState drawingState;

    //Variables for keeping track of cursor in 3D.
    Vector3 normalHit;
    Vector3 cursorPosition;
    Vector3 rayCastPoint;
    Vector3 rayCastHit;
    Ray fullRay;
    //The object cursor points to.
    GameObject cursorPointTo;
    public Texture cursorIcon;

    Vector3 _mousePos;
    void Start()
    {
        selectedObjects = new Dictionary<string, AgentPhysics>();
        drawingState = DrawingState.Idle;
        _cam = Camera.main;
    }

    void Update()
    {
        _mousePos = Input.mousePosition;
        _mousePos.z = Camera.main.nearClipPlane + displacement.z;
        _mousePos = Camera.main.ScreenToWorldPoint(_mousePos);
        gameObject.transform.rotation = Camera.main.transform.rotation;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        UpdateCursorPosition();
        DefineCursorSettings();
    }

    void UpdateCursorPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z += _cam.nearClipPlane;
        rayCastPoint = getCursorPosition(mousePos);
    }

    private void DefineCursorSettings()
    {
        Cursor.lockState = CursorLockMode.Confined;
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
    Vector3 getCursorPosition(Vector2 screenPoint)
    {
        cursorPointTo = null;
        Ray ray = _cam.ScreenPointToRay(screenPoint);
        int layerMask = 0;
        layerMask = 1 << 19;
        layerMask = 1 << 21;
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
        fullRay = ray;
        return Vector3.negativeInfinity;
    }

    public override void DrawShapes(Camera cam)
    {
        //DrawCursor(cam);
    }
    
    public void DrawCursor(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Vector3 camPos = cam.transform.position;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;
            Draw.Position += displacement;
            Draw.Position += new Vector3(_mousePos.x, _mousePos.y, _mousePos.z - displacement.z);
            Draw.ZTest = CompareFunction.Always;
            Draw.Texture(cursorIcon, new Rect(new Vector2(-0.08f, -0.22f), new Vector2(0.2f, 0.2f)));
        }
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

    public void OnDrawGizmos()
    {
        Debug.DrawRay(fullRay.origin, fullRay.direction, Color.red);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(GetRayCastedHit(), 0.1f);
    }
}
