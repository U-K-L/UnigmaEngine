using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;
using UnityEngine.Rendering;

public class CursorIndicatorUI : ImmediateModeShapeDrawer
{
    // Start is called before the first frame update
    private Player_Controller_Agents _controller;
    public CursorDrawImage cursor;
    void Start()
    {
        _controller = GameObject.FindGameObjectWithTag("PathDrawing").GetComponentInChildren<Player_Controller_Agents>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(_controller.GetNormalHit(), Vector3.up);
        transform.localPosition = _controller.GetNormalHit() * -0.2f;
    }

    public override void DrawShapes(Camera cam)
    {
        DrawSphereIndicator(cam);
        cursor.DrawCursor(cam);

    }

    void DrawSphereIndicator(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.Matrix = gameObject.transform.localToWorldMatrix; // draw it in the space of crosshairTransform
            /*
            Draw.ZTest = CompareFunction.LessEqual;


            Draw.Color = new Color(0.93f, 0.93f, 0.59f);

            Draw.BlendMode = ShapesBlendMode.ColorBurn;
            Draw.Radius = 1.5f;
            //Draw.Scale(new Vector3(1f, 0.2f, 1f));
            Draw.Sphere();
            */
            Draw.ZTest = CompareFunction.Always;
            Draw.DiscGeometry = DiscGeometry.Flat2D;
            //Draw.DiscGradientRadial (new Color(0, 0.73f, 1.0f, 1.0f), new Color(0.93f, 0.93f, 0.59f));
            DiscColors colors = new DiscColors();
            colors.innerStart = new Color(0, 0.73f, 1.0f, 1.0f);
            colors.outerEnd = new Color(0, 0.03f, 1.0f, 0.0f);
            Draw.BlendMode = ShapesBlendMode.Transparent;
            Draw.Radius = 2.75f;
            Draw.Thickness = 0.5f;
            //Draw.Disc(colors);


        }
    }
}
