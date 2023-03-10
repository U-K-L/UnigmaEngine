using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MenuButtons : ImmediateModeShapeDrawer
{
    public string type;
    public string buttonText;
    public float fontSize = 1;

    public Color fontColor;

    public Vector4 RectangleXYHW;
    public GradientFill UIColor;
    public Color UIColor2;

    public Vector3 dashStart;
    public float dashCount;
    public float dashSpaces;

    public Vector3 dashEnd;
    public Vector4 RectangleXYHW2;

    public Color dashColor;


    public Vector3 arcPos;
    public Vector3 arcRRR;

    public float skew;

    public ShapesBlendMode DebugBlend;

    public Vector3 linePositionTop;
    public Vector3 linePositionTopEnd;
    public Vector3 linePositionBottom;
    public Vector3 linePositionBottomEnd;
    public Color lineColor;

    public Vector3 angleDisc;
    public Vector3 discPos;

    public GradientFill shadowColor;
    public Vector4 RectangleXYHW3;
    public ShapesBlendMode ShadowBlend;
    public float shadowRadius;
    public Vector3 shadowPos;

    public Vector3 DebugPosition;

    Vector3 _mousePos;
    Vector2 _UIPosition; // the position of the UI.
    Vector2 widthHeight;

    public Vector2 collisionMin = new Vector2(-0.45f, -0.3f);
    public Vector2 collisionMax = new Vector2(4.5f, 1);

    public bool active = false;
    // Start is called before the first frame update
    void Start()
    {
        collisionMin = new Vector2(-0.45f, -0.3f);
        collisionMax = new Vector2(4.5f, 1);
    }

    // Update is called once per frame
    void Update()
    {
        _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        CheckCollision(Camera.main);
        CheckButtonClicked();
    }

    public override void DrawShapes(Camera cam)
    {

        DrawUI(cam);
        DrawButtonText(cam);
    }

    void DrawUI(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.BlendMode = ShapesBlendMode.Opaque;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;//cam.transform.localToWorldMatrix;
            //Draw.Position += this.transform.position;
            Draw.LocalScale = this.transform.localScale;

            
            Vector2 posScreen = cam.WorldToScreenPoint(Draw.Position2D);
            
            //Vector2 worldScreen = new Vector2(posScreen.x * 0f, posScreen.y);
            //Draw.Position = cam.ScreenToWorldPoint(worldScreen);
            //Draw.Position += new Vector3(0.45f + this.transform.position.x, 0, 0);
            //Draw Shadow
            Draw.UseDashes = false;
            Draw.UseGradientFill = true;
            Draw.GradientFill = shadowColor;
            Draw.BlendMode = ShadowBlend;

            /*
            //bottom
            Vector3 a2 = new Vector3(RectangleXYHW3.x, RectangleXYHW3.y, 0);
            Vector3 b2 = new Vector3(RectangleXYHW3.x + RectangleXYHW3.w, RectangleXYHW3.y, 0);
            //top
            Vector3 c2 = new Vector3(RectangleXYHW3.x + skew, RectangleXYHW3.y + RectangleXYHW3.z, 1);
            Vector3 d2 = new Vector3(RectangleXYHW3.x + skew + RectangleXYHW3.w, RectangleXYHW3.y + RectangleXYHW3.z, 1);
            Draw.Quad(a2, c2, d2, b2);
            */
            Draw.Radius = shadowRadius;
            Draw.Rectangle(shadowPos, new Rect(new Vector2(RectangleXYHW3.x, RectangleXYHW3.y), new Vector2(RectangleXYHW3.z, RectangleXYHW3.w)), shadowRadius);



            Draw.GradientFill = UIColor;


            Rect rect = new Rect(RectangleXYHW.x, RectangleXYHW.y, RectangleXYHW.z, RectangleXYHW.w);
            //Get rect's width in pixels
            
            
            widthHeight.y = rect.height;
            widthHeight.x = rect.width;
            Draw.Rectangle(new Vector3(0, 0, 1), rect);


            Draw.UseGradientFill = false;
            //draw the second rectangle
            Draw.Color = UIColor2;
            //bottom
            Vector3 a = new Vector3(RectangleXYHW2.x, RectangleXYHW2.y, 0);
            Vector3 b = new Vector3(RectangleXYHW2.x + RectangleXYHW2.w, RectangleXYHW2.y, 0);
            //top
            Vector3 c = new Vector3(RectangleXYHW2.x + skew, RectangleXYHW2.y + RectangleXYHW2.z, 1);
            Vector3 d = new Vector3(RectangleXYHW2.x + skew + RectangleXYHW2.w, RectangleXYHW2.y + RectangleXYHW2.z, 1);
            Draw.Quad(a, c, d, b);
            //Draw.Rectangle(new Vector3(0, 0, 1), new Rect(new Vector2(RectangleXYHW2.x, RectangleXYHW2.y), new Vector2(RectangleXYHW2.z, RectangleXYHW2.w)));
            Draw.Color = dashColor;
            /*
            Draw.LineEndCaps = LineEndCap.Round;
            Draw.Line(new Vector3(-1.25f, 2.76f, 1), new Vector3(1.25f, 2.76f, 1));

            Draw.Line(new Vector3(-1.25f, 3.3f, 1), new Vector3(1.25f, 3.3f, 1));
            */
            //Draw outer dashed lines
            Draw.BlendMode = ShapesBlendMode.ColorDodge;
            Draw.UseDashes = true;
            Draw.DashSize = 2.35f;
            Draw.DashSpacing = 1.5f;
            Vector3 lineStart = dashStart;
            for (int i = 0; i < dashCount; i++)
            {
                Draw.Line(lineStart, lineStart + dashEnd);
                lineStart += new Vector3(dashSpaces, 0, 0);
            }

            //Draw boarders
            Draw.BlendMode = DebugBlend;
            Draw.UseDashes = false;
            Draw.LineEndCaps = LineEndCap.Round;
            Draw.Color = lineColor;
            Draw.BlendMode = DebugBlend;
            Draw.Arc(arcPos, arcRRR.x, arcRRR.y, arcRRR.z);

            Draw.UseDashes = true;
            Draw.DashSize = 2.35f;
            Draw.DashSpacing = 1.5f;
            //Draw line using top and bottom ends
            Draw.Line(linePositionTop, linePositionTopEnd);
            Draw.Line(linePositionBottom, linePositionBottomEnd);

            //Draw a disc
            Draw.Radius = angleDisc.z;
            Draw.Pie(discPos, angleDisc.x, angleDisc.y);


            _UIPosition = Draw.Position2D;
        }
    }

    private void DrawButtonText(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;//Draw.Matrix = cam.main.transform.localToWorldMatrix;
            
            //Vector2 posScreen = cam.WorldToScreenPoint(Draw.Position2D);
            //Vector2 worldScreen = new Vector2(posScreen.x * 0f, posScreen.y);
            //Draw.Position = cam.ScreenToWorldPoint(worldScreen);
            //Draw.Position += new Vector3(0.45f + this.transform.position.x, 0, 0);
            Draw.LocalScale = this.transform.localScale;
            Draw.FontSize = fontSize;
            if (active)
            {
                Draw.Color = fontColor;
                
            }
            else
            {
                Draw.Color = new Vector4(0.66f, 0.66f, 0.66f, 1);
            }
            //Draw.Color = fontColor;
            Draw.Text(DebugPosition, buttonText);
        }

    }

    void CheckCollision(Camera cam)
    {
        Vector3 UIpos = _UIPosition;
        //UIpos = cam.WorldToScreenPoint(_UIPosition);
        Vector2 boxMin =  new Vector2(UIpos.x, UIpos.y) + collisionMin;
        Vector2 boxMax = boxMin + collisionMax;

        Debug.Log(this.name + "  " + type + " UI Pos " + UIpos);
        //boxMax = Camera.main.ScreenToWorldPoint(boxMax);
        //boxMin = Camera.main.ScreenToWorldPoint(boxMin);

        //Debug.Log("Mouse position is: " + _mousePos);
        //Debug.Log("Box min is: " + boxMin.ToString("F7"));
        //Debug.Log("Box max is: " + boxMax.ToString("F7"));
        

        if (_mousePos.x > boxMin.x && _mousePos.x < boxMax.x && _mousePos.y > boxMin.y && _mousePos.y < boxMax.y)
        {
            Active();
        }
        else
        {
            Unactive();
        }
    }

    void CheckButtonClicked()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("Mouse position is: " + _mousePos);
            if (active)
            {
                ButtonClicked();
            }
        }
    }

    void Active()
    {
        active = true;
    }

    void Unactive()
    {
        active = false;
    }

    void ButtonClicked()
    {
        if (type == "Singleplayer")
        {
            EggGameMaster.Instance.Singleplayer();
        }
        else if (type == "Multiplayer")
        {
            EggGameMaster.Instance.Multiplayer();
        }
    }
}
