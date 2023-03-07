using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;
using UnityEngine.Rendering;

public class CursorDrawImage : ImmediateModeShapeDrawer
{
    // Start is called before the first frame update
    private Player_Controller_Agents _controller;
    public Texture texture;
    void Start()
    {
        _controller = GameObject.FindGameObjectWithTag("PathDrawing").GetComponentInChildren<Player_Controller_Agents>();
    }


    // Update is called once per frame
    void Update()
    {
        Vector3 Ceuler = _controller._cam.transform.rotation.eulerAngles;
        Vector3 Teuler = gameObject.transform.rotation.eulerAngles;
        Vector3 swizzle = new Vector3(Teuler.x, Ceuler.y, Ceuler.z);
        gameObject.transform.rotation = Quaternion.Euler(swizzle);
    }

    public void DrawCursor(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.Position = Vector3.zero;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;
            Draw.Texture(texture, new Rect(new Vector2(-0.08f,-0.22f), new Vector2(0.2f, 0.2f)));
        }
    }

}
