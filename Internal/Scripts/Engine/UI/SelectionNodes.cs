using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SelectionNodes : ImmediateModeShapeDrawer
{

    public Vector3 offset;
    public Texture image;
    public override void DrawShapes(Camera cam)
    {
        Debug.Log("Disc is located: " + Draw.Position);
        DrawCircleBackground(cam);
    }

    public void DrawCircleBackground(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.BlendMode = ShapesBlendMode.Opaque;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;//cam.transform.localToWorldMatrix;
            //Draw.Position += this.transform.position;
            Draw.LocalScale = this.transform.localScale;

            Draw.Disc(Vector3.zero, 1, new Color(0.3f,0.35f,0.85f,0.75f));
            
            Draw.Ring(Vector3.zero, 1, Color.blue);
            Draw.BlendMode = ShapesBlendMode.Opaque;
            Draw.Texture(image, new Rect(new Vector2(-1f, -1f), new Vector2(2f, 2f)));

            Draw.Position += offset;

        }
    }
}
