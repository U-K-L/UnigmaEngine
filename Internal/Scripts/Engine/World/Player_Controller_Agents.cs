/*
 * Controls the players controller. This is in charge of the cursor, the summoner the player controls, as well as a list of agents.
 * Handles creation of UI elements specific to a single player. Can have multiple instances per player.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Controller_Agents : MonoBehaviour
{
    private string name = "Player_{id}"; //The string used to identify which player this is.
    public Camera _cam; //The current camera for this player.
    public Agent_Summoner summoner; //The summoner the player controls.

    //Path Drawing variables
    private GameObject _line; //The variable to store the line object.
    private LineRenderer lineRenderer; //stores the line renderer object.
    public List<Vector3> points; //All points within the current line.
    private Vector3 cursorPosition; //Mouse position in 3D space.
    private GameObject _collider; //ref to collider of current line.
    private GameObject cursorPointTo; //The object the mouse is currently on.
    public GameObject pathPrefab; //The prefab for lines.
    private List<CrossingJamPath> _paths;

    //UI variables.
    public GameObject UIOptionsPrefab; //Prefab used to create the UI.
    protected GameObject UIOptions; //The reference to the UI object.
    protected UIAgentOptions uiAgentOptions;

    public Dictionary<string, AgentPhysics> selectedObjects; //All selected objects.
    public float areaOfEffect = 1.0f; //Size of selection.
    public GameObject cursorPrefab; //The cursor used.
    public GameObject cursor; //Private reference to instantiated cursor.
    private AgentPhysics _ped; //reference to currently selected pedestrain.
    private float timeButtonHeld = 0.0f; //Cool down for selection button held down.

    private int _pathsSizeLimit = 50;
    private GameObject _parent;
    private PerspectiveCameraLerp perpCam;
    public float minimumDistance = 0.05f; //minimum distance required to place a point on the line.
    private Vector3 normalHit;

    public CameraController camCon;
    public float Sensitivity = 1f;
    protected enum DrawingState { Idle, Drawing, Selecting, Menu, Following }
    protected DrawingState drawingState;

    private Vector2 ScreenPosition;
    public AgentSkill debugSkill;
    public TrajectoryPrediction trajectory;
    public GameObject map;
    public bool usingOrthoCam;
    void Start()
    {
        
        ScreenPosition = new Vector2(Screen.width, Screen.height) * 0.5f;
        cursor = Instantiate(cursorPrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
        UIOptions = Instantiate(UIOptionsPrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
        uiAgentOptions = UIOptions.GetComponent<UIAgentOptions>();
        selectedObjects = new Dictionary<string, AgentPhysics>();
        _paths = new List<CrossingJamPath>();
        perpCam = _cam.gameObject.GetComponent<PerspectiveCameraLerp>();
        drawingState = DrawingState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (usingOrthoCam)
        {
            if (perpCam.isOrtho)
            {
                updateInputs();
                updatePaths();
            }
        }
        else
        {
            updateInputs();
            updatePaths();
        }
        */
        updateInputs();
        updatePaths();

        //If it is executing path
        followAgents();
        if (Input.GetButtonDown("Debug Next"))
            trajectory.trajectoryPrediction(_ped.gameObject, map, 60);
        if (Input.GetButtonDown("Debug Previous"))
        {
            Jump();
            drawingState = DrawingState.Following;
            
            //_ped.gameObject.GetComponent<Rigidbody>().AddForce(Vector3.forward * 30f + Vector3.up * 50f, ForceMode.Impulse);
            
        }
        Cursor.lockState = CursorLockMode.Confined;

    }

    void followAgents()
    {

        if (drawingState == DrawingState.Following || drawingState == DrawingState.Selecting)
        {
            if (Input.GetButtonDown("Drag_Desktop"))
            {
                drawingState = DrawingState.Idle;
            }
            if (_ped != null)
                if(_ped.currentlyFollowed == true)
                    camCon.moveCamera(_ped.transform.position);
        }
                
    }

    void updatePaths()
    {
        if (_paths.Count == 0)
            return;
        foreach (CrossingJamPath path in _paths)
        {
            if (path != null)
            {
                bool isActivePath = path.checkPathsActive();
                if (isActivePath == false)
                {
                    if (path.path != null)
                    {
                        if (path.parent)
                            removeObject(path.parent);
                        path.path = null;
                    }

                }

            }
        }
    }

    void updateInputs()
    {
        Cursor.visible = false;

        if (camCon.rotatingState == false)
        {
            update2DCursor();
            cursor.transform.position = Vector3.Lerp(cursor.transform.position, cursorPosition, Time.deltaTime * 20f);
        }

        if (camCon._camState == CameraController.CameraState.Controlled)
            camCon.SetPivot(cursor.transform.position);



        updateDrawingPathInputs();
        updateSelectingUnitsInputs();
        updateMenuInputs();


        //if (camCon._camState == CameraController.CameraState.Controlled)

    }

    void updateDrawingPathInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CreatePath();
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 point = cursor.transform.position;
            if (!isIntersect(point))
            {
                UpdateLine(point);
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            Mesh mesh = new Mesh();
            MeshCollider meshCollider = _collider.AddComponent<MeshCollider>();
            lineRenderer.BakeMesh(mesh, true);
            meshCollider.sharedMesh = mesh;
            executePath();
        }
    }

    void updateSelectingUnitsInputs()
    {
        if (Input.GetButtonDown("Accept"))
        {
            drawingState = DrawingState.Selecting;
            timeButtonHeld = Time.time;
            if (cursorPointTo.tag == "Pedestrian")
            {
                if (_ped != null)
                    _ped.currentlyFollowed = false;
                _ped = cursorPointTo.GetComponent<AgentPhysics>();
                _ped.currentlyFollowed = true;
                if (selectedObjects.ContainsKey(_ped.ToString()))
                    removeObjectFromList(_ped);
                else
                    addObjectToList(_ped);
            }
        }
        else if (Input.GetButton("Accept") && Mathf.Abs(timeButtonHeld - Time.time) > 0.2f)
        {
            drawingState = DrawingState.Selecting;
            if (cursorPointTo.tag == "Pedestrian")
            {
                if (_ped != null)
                    _ped.currentlyFollowed = false;
                _ped = cursorPointTo.GetComponent<AgentPhysics>();
                _ped.currentlyFollowed = true;
                if (!selectedObjects.ContainsKey(_ped.ToString()))
                    addObjectToList(_ped);
            }
        }
        else if (Input.GetButtonDown("Cancel"))
        {
            removeAllObjectFromList();
        }
    }

    void updateMenuInputs()
    {
        //Deactivates menu if pressed while menu already opened.
        if (Input.GetButtonDown("MenuKeyboard"))
        {
            if (drawingState == DrawingState.Menu)
            {
                deactivateMenu();
                drawingState = DrawingState.Selecting;
            }
            else if(drawingState != DrawingState.Menu)
            {
                //if (selectedObjects.Count > 0)
                //{
                    activateMenu();
                    drawingState = DrawingState.Menu;
                //}

            }
        }
    }

    void CreatePath()
    {

        _parent = Instantiate(pathPrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
        _parent.name = "Line";
        _line = Instantiate(pathPrefab, Vector3.zero, Quaternion.identity, _parent.transform);
        _collider = Instantiate(new GameObject(), Vector3.zero, Quaternion.identity, _parent.transform);
        LineMeshCollider collider = _collider.AddComponent<LineMeshCollider>();
        _collider.layer = LayerMask.NameToLayer("Paths");
        collider.line = _line;
        Vector3 rotatedVector = new Vector3(93.0f * normalHit.y, 93.0f * normalHit.x, 93.0f * normalHit.z);
        _line.transform.Rotate(rotatedVector.x, rotatedVector.y, rotatedVector.z, Space.World);
        lineRenderer = _line.GetComponent<LineRenderer>();
        
        points.Clear();
        Vector3 point = cursorPosition;
        points.Add(point);
        points.Add(point);
        lineRenderer.SetPosition(0, points[0]);
        lineRenderer.SetPosition(1, points[1]);
    }

    void UpdateLine(Vector3 newPathPos)
    {
        drawingState = DrawingState.Drawing;
        points.Add(newPathPos);
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, newPathPos);
    }

    void update2DCursor()
    {
        float mouseHori = Input.GetAxis("Mouse X")*20f*Sensitivity;
        float mouseVert = Input.GetAxis("Mouse Y") *20f* Sensitivity;
        Vector2 mouseAxis = new Vector2(mouseHori , mouseVert);

        ScreenPosition += mouseAxis;
        ScreenPosition.x = Mathf.Min(Screen.width, ScreenPosition.x);
        ScreenPosition.x = Mathf.Max(0, ScreenPosition.x);
        ScreenPosition.y = Mathf.Min(Screen.height, ScreenPosition.y);
        ScreenPosition.y = Mathf.Max(0, ScreenPosition.y);

        getCursorPosition(ScreenPosition);
    }

    //Used to get 2D cursor into 3D.
    Vector3 getCursorPosition(Vector2 screenPoint)
    {
        Ray ray = _cam.ScreenPointToRay(screenPoint);
        int layerMask = 0;
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
            cursorPointTo = raycastHit.transform.gameObject;
            return raycastHit.point;
        }
        return Vector3.zero;
    }

    void removeObject(GameObject obj)
    {
        StartCoroutine(PlayAnimation(obj));

    }

    IEnumerator PlayAnimation(GameObject obj)
    {
        yield return new WaitForSeconds(1.2f);
        destroyObject(obj);
    }
    void destroyObject(GameObject obj)
    {
        StopCoroutine("PlayAnimation");
        Destroy(obj);
    }

    void addObjectToList(AgentPhysics ped)
    {
        selectedObjects.Add(ped.ToString(), ped);
        if (selectedObjects.ContainsKey(ped.ToString()))
        {
            GlowObject glow = ped.GetComponent<GlowObject>();
            if (glow)
            {
                glow.ActiveGlow = true;
                glow.enabled = true;
            }
        }

    }

    void removeObjectFromList(AgentPhysics ped)
    {
        selectedObjects.Remove(ped.ToString());
        if (!selectedObjects.ContainsKey(ped.ToString()))
        {
            GlowObject glow = ped.GetComponent<GlowObject>();
            if (glow)
            {
                glow.ActiveGlow = false;
                glow.enabled = true;
            }
        }

    }

    void executePath()
    {
        CrossingJamPath path = null;
        foreach (CrossingJamPath pathCJ in _paths)
        {
            if (pathCJ.path == null)
                path = pathCJ;
        }

        if (path == null)
            path = new CrossingJamPath();

        path.path = lineRenderer;
        path.parent = _parent;
        foreach (KeyValuePair<string, AgentPhysics> Entry in selectedObjects)
        {
            AgentPhysics ped = Entry.Value;
            if (ped != null)
                ped.path = path.path;
            path.addObjectToPath(ped);
            ped.setFullPath();
            ped.Seeking();
        }
        _paths.Add(path);
        removeAllObjectFromList();
        drawingState = DrawingState.Following;
        
        
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

    //Checks if line is intersecting with line already created.
    bool isIntersect(Vector3 pos)
    {
        foreach (Vector3 point in points)
        {
            if (Vector3.Distance(pos, point) < minimumDistance)
                return true;
        }
        return false;
    }

    void Jump()
    {
        foreach (KeyValuePair<string, AgentPhysics> Entry in selectedObjects)
        {
            AgentPhysics ped = Entry.Value;
            ped.setStateJumping();
        }
        
    }

    void activateMenu()
    {
        UIOptions.SetActive(true);
        uiAgentOptions.setController(this);
        if (selectedObjects.Count > 0)
        {
            uiAgentOptions.setAgent(_ped);
            camCon.moveCamera(_ped.transform.position);
        }
        else
            uiAgentOptions.setAgent(summoner);
        uiAgentOptions.activateMenu();
        
    }

    void deactivateMenu()
    {
        uiAgentOptions.setController(null);
        uiAgentOptions.deactivateMenu();
    }

    public virtual Vector3 GetCursorPosition()
    {
        return cursor.transform.position;
    }

    public virtual Vector3 GetPivot()
    {
        return cursor.transform.position;
    }

    public Vector3 GetNormalHit()
    {
        return normalHit;
    }
}
