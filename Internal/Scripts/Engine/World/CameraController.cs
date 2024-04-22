using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Start is called before the first frame update
    [HideInInspector]
    public Camera _cam;

    private Vector3 childOrigRot;
    private Vector3 parentOrigRot;
    public float _camSpeed = 1.0f;
    public float _rotationSpeed = 1.0f;
    public float followSpeed = 1.0f;
    private Vector3 _prevCamDelta;
    private Vector3 _camPosDelta;
    public Vector3 MaxPositions;
    public Vector3 MinPositions;
    public Vector3 MaxRots;
    public Vector3 MinRots;
    private float rotationDelta = 0.0f;
    [Tooltip("Ortho Max, Min Size. Perspective Max, Min FOV")]
    public Vector4 MaxMinZoom = new Vector4(0.87f, 0.6f, 90f, 45f);
    private Vector3 _pivot;
    public bool rotatingState = false;
    [HideInInspector]
    public PerspectiveCameraLerp perpCam;
    public bool originalCamIsPerp = true;
    public float AxisSlider = 1;
    public GameObject simplePivot;

    public enum CameraState
    {
        Tracking,
        Controlled,
        Resetting,
        Rotating,
        Dragged,
        Idle,
        FreeRoaming, 
        Simple
    }

    public CameraState _camState = CameraState.Tracking;

    void Start()
    {
        _cam = Camera.main;
        childOrigRot = _cam.transform.eulerAngles;
        parentOrigRot = gameObject.transform.eulerAngles;
        if (originalCamIsPerp)
            perpCam = _cam.gameObject.GetComponent<PerspectiveCameraLerp>();
        else
            perpCam = null;
        _camPosDelta = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateStates();
        updateInputMouse();
        //updateInputController();
       //UpdateChildCameraPosition();
        //UpdateCameraAlongAxis();
    }

    void UpdateStates()
    {
        if (_camState == CameraController.CameraState.Tracking)
        {
            SetPivot(_pivot);
            moveCamera(_pivot);
        }
        else if (_camState == CameraController.CameraState.Resetting)
        {
            SetPivot(_pivot);
            moveCamera(_pivot);
        }
        else if (_camState == CameraController.CameraState.Rotating)
        {
            moveCamera(_pivot);
        }
        else if (_camState == CameraController.CameraState.Idle)
        {
            moveCamera(_pivot);
        }
        else if (_camState == CameraController.CameraState.FreeRoaming)
        {
            SetPivot(_pivot);
            moveCamera(_pivot);
        }
        else if (_camState == CameraController.CameraState.Simple)
        {
            SetPivot(simplePivot.transform.position);
        }
    }

    public void UpdateChildCameraPosition()
    {

        //Set the max and min for camera position.
        _camPosDelta = new Vector3(
            Mathf.Min(MaxPositions.x, _camPosDelta.x),
            Mathf.Min(MaxPositions.y, _camPosDelta.y),
            Mathf.Min(MaxPositions.z, _camPosDelta.z));
        _camPosDelta = new Vector3(
            Mathf.Max(MinPositions.x, _camPosDelta.x),
            Mathf.Max(MinPositions.y, _camPosDelta.y),
            Mathf.Max(MinPositions.z, _camPosDelta.z));

        //Camera difference added to be ahead of previous frame.
        _camPosDelta += _prevCamDelta;
        //Moves the camera controller to next frame. Here we an control the following speed.
        Vector3 direction = _camPosDelta - _cam.transform.position;
        direction.Normalize();
        Vector3 moveAlong = Vector3.zero;
        
        //To prevent glitch where camera rolls over for perspective
        if(_cam.GetComponent<PerspectiveCameraLerp>().isOrtho)
            moveAlong = direction* AxisSlider;

        gameObject.transform.localPosition = Vector3.Lerp(gameObject.transform.localPosition, _camPosDelta + moveAlong, Time.deltaTime * _camSpeed * followSpeed * 0.1f);
        
        _prevCamDelta = Vector3.Lerp(_prevCamDelta, Vector3.zero, Time.deltaTime * _camSpeed);
        if (!Input.GetButton("Rotate_Camera_Desktop") && !Input.GetButton("Drag_Desktop") && _camState == CameraState.Resetting)
        {

            //_cam.transform.eulerAngles = Vector3.Lerp(_cam.transform.eulerAngles, origRot, Time.deltaTime * _camSpeed*0.25f);
            if (rotationDelta < 0.5f)
            {
                rotateCameraToOrigin(0.05f);
            }
            else
            {
                rotateCameraToOrigin(0.25f);
            }

            //_camState = CameraState.Tracking;

            rotationDelta += Time.deltaTime;
        }
        else
        {
            rotationDelta = 0.0f;
        }

    }

    public void UpdateCameraAlongAxis()
    {
        Vector3 direction = _camPosDelta - _cam.transform.position;
        direction.Normalize();
        transform.position = direction * AxisSlider;
    }

    void updateInputMouse()
    {
        float mouseHori = Input.GetAxis("Mouse X") * 0.09f;
        float mouseVert = Input.GetAxis("Mouse Y") * 0.05f;
        if (Input.GetButtonDown("Drag_Desktop"))
        {
            StopCoroutine(DragCameraInput());
            StartCoroutine(DragCameraInput());
        }
        else if (Input.GetButton("Rotate_Camera_Desktop"))
        {
            _camState = CameraState.Rotating;
            Vector3 rot = new Vector3(mouseVert * 5f * _rotationSpeed, mouseHori * 10f * _rotationSpeed, 0);
            rotateCameraByController(rot);
        }
        else
        {
            //if(_camState != CameraState.Resetting)
            //    _camState = CameraState.Tracking;
        }
        float deltaSize = -Input.mouseScrollDelta.y * 0.05f;
        /*
        if (perpCam)
        {
            if (perpCam.isOrtho)
            {
                _cam.orthographicSize = Mathf.Min(MaxMinZoom.x, deltaSize + _cam.orthographicSize);
                _cam.orthographicSize = Mathf.Max(MaxMinZoom.y, deltaSize + _cam.orthographicSize);
            }
            else
            {
                _cam.fieldOfView = Mathf.Min(MaxMinZoom.z, 40f * deltaSize + _cam.fieldOfView);
                _cam.fieldOfView = Mathf.Max(MaxMinZoom.w, 40f * deltaSize + _cam.fieldOfView);
            }
        }
        */

    }

    void updateInputController()
    {
        if (Input.GetButton("Drag_Controller"))
        {
            float leftStickHori = Input.GetAxis("RightStickHorizontal") * 0.095f;
            float leftStickVert = Input.GetAxis("RightStickVertical") * 0.122f;
            Vector3 mouseAxis = new Vector3(-leftStickVert, 0, -leftStickHori);
            mouseAxis = Quaternion.Euler(0, 46.9f, 0) * mouseAxis;
            _camPosDelta = mouseAxis + gameObject.transform.localPosition;
            _prevCamDelta = mouseAxis;
        }

    }

    public void Dolly()
    {
        StopCoroutine(DollyCamera());
        StartCoroutine(DollyCamera());
    }
    
    IEnumerator DollyCamera()
    {
        while (true)
        {
            //Rotate back and forth.
            Vector3 rot = new Vector3(0, 20f, 0);
            rotateCameraByController(rot);
            yield return new WaitForSeconds(1f);
        }
    }

    public void EndDolly()
    {
        StopAllCoroutines();
        _camState = CameraState.Resetting;

    }

    IEnumerator DragCameraInput()
    {
        _camState = CameraState.Dragged;
        DragCamera();
        _camState = CameraState.Controlled;
        yield return new WaitForSeconds(0.25f);
        while (Input.GetButton("Drag_Desktop"))
        {
            DragCamera();
            _camState = CameraState.Controlled;
            yield return new WaitForSeconds(0.02f);
        }
    }

    void DragCamera()
    {
        float mouseHori = Input.GetAxis("Mouse X") * 0.09f;
        float mouseVert = Input.GetAxis("Mouse Y") * 0.05f;
        Vector3 mouseAxis = new Vector3(-mouseVert, 0f, mouseHori);
        mouseAxis = Quaternion.Euler(0, _cam.transform.eulerAngles.y - 90, 0) * mouseAxis;
        //_camPosDelta = mouseAxis + gameObject.transform.localPosition;
        _prevCamDelta = mouseAxis;
        moveCamera(_pivot);
    }


    public void moveCamera(Vector3 pos)
    {
        _camPosDelta = pos;
    }

    void rotateCameraByController(Vector3 rot)
    {
        _camState = CameraState.Rotating;
        /*
        
        Vector3 childCombRot = new Vector3(_cam.transform.eulerAngles.x, _cam.transform.eulerAngles.y, _cam.transform.eulerAngles.z);
        childCombRot = new Vector3(
            Mathf.Min(MaxRots.x, childCombRot.x),
            Mathf.Min(MaxRots.y, childCombRot.y),
            Mathf.Min(MaxRots.z, childCombRot.z));
        _cam.transform.eulerAngles = new Vector3(
            Mathf.Max(MinRots.x, childCombRot.x),
            Mathf.Max(MinRots.y, childCombRot.y),
            Mathf.Max(MinRots.z, childCombRot.z));

        //Now for the parent object:
        Vector3 parentCombRot = new Vector3(gameObject.transform.eulerAngles.x, gameObject.transform.eulerAngles.y + rot.y, gameObject.transform.eulerAngles.z);
        parentCombRot = new Vector3(
            Mathf.Min(MaxRots.x, parentCombRot.x),
            Mathf.Min(MaxRots.y, parentCombRot.y),
            Mathf.Min(MaxRots.z, parentCombRot.z));
        gameObject.transform.eulerAngles = new Vector3(
            Mathf.Max(MinRots.x, parentCombRot.x),
            Mathf.Max(MinRots.y, parentCombRot.y),
            Mathf.Max(MinRots.z, parentCombRot.z));
        */

        gameObject.transform.RotateAround(_pivot, new Vector3(0, rot.y,0), Time.deltaTime * 20 * 10.25f * _rotationSpeed);
        Quaternion fromParent = Quaternion.Euler(gameObject.transform.eulerAngles);
        Quaternion toParent = Quaternion.Euler(gameObject.transform.eulerAngles.x + rot.x*15, gameObject.transform.eulerAngles.y, gameObject.transform.eulerAngles.z);
        //Vector3 parentEuler = Quaternion.Slerp(fromParent, toParent, _camSpeed * rotationDelta * 1000).eulerAngles;
        //gameObject.transform.rotation = Quaternion.Euler(toParent.eulerAngles.x, gameObject.transform.eulerAngles.y, gameObject.transform.eulerAngles.z);
    }

    void rotateCameraToOrigin(float speed)
    {
        //The parent needs to change on the x axis only.
        //The child needs to change on the y axis only (child being main cam).
        Quaternion fromChild = Quaternion.Euler(_cam.transform.eulerAngles);
        Quaternion toChild = Quaternion.Euler(childOrigRot);
        Quaternion fromParent = Quaternion.Euler(gameObject.transform.eulerAngles);
        Quaternion toParent = Quaternion.Euler(parentOrigRot);

        Vector3 childEuler = Quaternion.Slerp(fromChild, toChild, _camSpeed * Time.deltaTime * rotationDelta * speed).eulerAngles;
        Vector3 parentEuler = Quaternion.Slerp(fromParent, toParent, _camSpeed * Time.deltaTime * rotationDelta * speed).eulerAngles;

        _cam.transform.rotation = Quaternion.Euler(childEuler.x, _cam.transform.eulerAngles.y, _cam.transform.eulerAngles.z);

        gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.eulerAngles.x, parentEuler.y, gameObject.transform.eulerAngles.z);
    }

    public void SetPivot(Vector3 p)
    {
        if(_camState != CameraState.Rotating)
            _pivot = p;
    }
    //Get pivot
    public Vector3 GetPivot()
    {
        return _pivot;
    }

}
