using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SelectionNodes : ImmediateModeShapeDrawer
{

    public Vector3 offset;
    public Vector3 VVF;
    public Texture image;

    public float t = 0.0f;

    public bool currentlySelected = false;

    SelectionScreenUI _ui;

    Camera _cam;

    public Color boarderCol;
    public override void DrawShapes(Camera cam)
    {
        if (!_ui)
            _ui = GetComponentInParent<SelectionScreenUI>();
        //Debug.Log("Disc is located: " + Draw.Position);
        //DrawCircleBackground(cam, Vector3.zero);
    }

    public void DrawCircleBackground(Camera cam, Vector3 vvf)
    {
        if (!_ui)
            return;
        //t = Mathf.Lerp(t, -(vvf.x), Time.deltaTime);
        VVF = Vector3.Lerp(VVF, vvf, Time.deltaTime*2f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, vvf, Time.deltaTime*35f);
        using (Draw.Command(cam))
        {
            float radius = 1 * 1.618f * Mathf.Abs(0.115f*VVF.x) + 2;
            if (VVF.x > 0)
                radius = 1;
            
            Draw.ResetAllDrawStates();
            Draw.BlendMode = ShapesBlendMode.Transparent;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;//cam.transform.localToWorldMatrix;
            //Draw.Position += this.transform.position;
            Draw.LocalScale = this.transform.localScale;
            float redRamp = Mathf.Pow(Mathf.Abs(VVF.x*0.095f), 2) + 1;

            float greenRamp = Mathf.Pow(Mathf.Abs(VVF.y * 0.45f), 2) + 1;
            
            Color DiscColorBG = new Color(0.3f * redRamp, 0.35f * greenRamp, 0.85f, 0.85f);

            //GradientFill gradientFill = new GradientFill();
            //gradientFill.radialRadius = 1;
            //gradientFill.colorStart = DiscColorBG;
            //gradientFill.colorEnd = new Color(DiscColorBG.r, DiscColorBG.g, DiscColorBG.b, 0);
            /*
            DiscColors gradientFill = new DiscColors();
            gradientFill.outerStart = DiscColorBG;
            gradientFill.outerEnd = new Color(DiscColorBG.r, DiscColorBG.g, DiscColorBG.b, 0);
            gradientFill.innerStart = DiscColorBG;
            gradientFill.innerEnd = new Color(DiscColorBG.r, DiscColorBG.g, DiscColorBG.b, 0);
            
            Draw.Disc(Vector3.zero, radius*1.075f);
            
            Draw.Ring(Vector3.zero, radius, DiscColors.Radial(DiscColorBG, new Color(DiscColorBG.r, DiscColorBG.g, DiscColorBG.b, 0)));
            */
            //Draw.UseGradientFill = true;
            Draw.Disc(Vector3.zero, radius, DiscColorBG);
            Draw.Disc(Vector3.zero, radius * 0.825f, new Color(0,0,0,0.35f));
            Draw.UseGradientFill = true;
            Color pinkish = new Color(Mathf.Min(0.65f + redRamp * 0.2f, 1), Mathf.Min(1 * greenRamp * 0.7f, 0.89f), 0.95f, 0.95f);
            Color pinkBlue = DiscColorBG + new Color(0.25f, 0, 0.25f, 0);
            
            Draw.Ring(Vector3.zero, radius, 0.75f, DiscColors.Radial(pinkBlue, new Color(pinkBlue.r, pinkBlue.g, pinkBlue.b, 0)));

            Draw.Texture(image, new Rect(new Vector2(-1f * radius + 0.35f, -1f * radius), new Vector2(2f * radius, 2f * radius)), new Color(1, 1, 1, 0.25f));
            Draw.Texture(image, new Rect(new Vector2(-1f * radius, -1f * radius), new Vector2(2f * radius, 2f * radius)));

            //Draw.Pie(new Vector2(0, 0), radius, 0, Mathf.PI, new Color(0,0,1,0.75f));
            Draw.Ring(Vector3.zero, radius, 0.15f, pinkish);
            Draw.BlendMode = ShapesBlendMode.Transparent;

            //Vector3 pos, float radius, float angleRadStart, float angleRadEn

            //Draw.Arc(new Vector2(0, radius *(-0.125f)), radius, 0.25f*radius, 0.5f,Mathf.PI / 2 + 1.075f, new Color(0, 0, 0, 0.75f));

            //Draw text description.
            //Draw.UseGradientFill = false;
            //Draw.Rectangle(Vector3.zero, new Rect(Vector2.zero, new Vector2(2f * radius, 2f * radius)),new Color(0, 0, 1, 0.44f));

            Draw.Position += offset;

        }
    }


    public void DrawStageSelectionNode(Camera cam, Vector3 vvf)
    {
        //t = Mathf.Lerp(t, -(vvf.x), Time.deltaTime);
        VVF = Vector3.Lerp(VVF, vvf, Time.deltaTime * 2f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, vvf, Time.deltaTime * 3f);
        using (Draw.Command(cam))
        {
            float radius = 2;
            
            if(currentlySelected)
                radius = 3f;

            Draw.ResetAllDrawStates();
            Draw.BlendMode = ShapesBlendMode.Transparent;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;
            
            Draw.LocalScale = this.transform.localScale;
            float redRamp = Mathf.Pow(Mathf.Abs(VVF.x * 0.095f), 2) + 1;

            float greenRamp = Mathf.Pow(Mathf.Abs(0.45f), 2) + 1;

            Color DiscColorBG = new Color(0.3f * redRamp, 0.35f * greenRamp, 0.85f, 0.85f);
            
            Draw.Disc(Vector3.zero, radius, DiscColorBG);
            Draw.Disc(Vector3.zero, radius * 0.825f, new Color(0, 0, 0, 0.35f));
            Draw.UseGradientFill = true;
            Color pinkish = new Color(Mathf.Min(0.65f + redRamp * 0.2f, 1), Mathf.Min(1 * greenRamp * 0.7f, 0.89f), 0.95f, 0.95f);
            Color pinkBlue = DiscColorBG + new Color(0.25f, 0, 0.25f, 0);

            Draw.Ring(Vector3.zero, radius, 0.75f, DiscColors.Radial(pinkBlue, new Color(pinkBlue.r, pinkBlue.g, pinkBlue.b, 0)));

            Draw.Texture(image, new Rect(new Vector2(-1f * radius + 0.35f, -1f * radius), new Vector2(2f * radius, 2f * radius)), new Color(1, 1, 1, 0.25f));
            Draw.Texture(image, new Rect(new Vector2(-1f * radius, -1f * radius), new Vector2(2f * radius, 2f * radius)));

            Draw.Ring(Vector3.zero, radius, 0.15f, pinkish);
            Draw.BlendMode = ShapesBlendMode.Transparent;

            Draw.Position += offset;

        }
    }
}
