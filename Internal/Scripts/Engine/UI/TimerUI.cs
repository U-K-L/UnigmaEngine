using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TimerUI : ImmediateModeShapeDrawer
{
    public int fontSize = 5;
    private string CurrentStringTime = "";
    private float currentTime;

    public Vector3 DebugPosition;

    public bool TimerOn = false;
    public bool TimerUpdating = false;
    public ShapesBlendMode BlendType;

    public Color fontColor;

    public Vector4 RectangleXYHW;
    public GradientFill UIColor;

    private void Start()
    {
        CurrentStringTime = "";
        currentTime = 0;
        TimerOn = false;
    }
    void Update()
    {
        if (TimerUpdating)
            UpdateTime();
    }
    public override void DrawShapes(Camera cam)
    {
        if (TimerOn)
        {
            DrawUI(cam);
            DrawTime(cam);
        }
    }
    private void DrawUI(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.BlendMode = BlendType;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = Camera.main.transform.localToWorldMatrix;

            Draw.UseGradientFill = true;
            Draw.GradientFill = UIColor;

            Draw.Rectangle(new Vector3(0,0,1), new Rect(new Vector2(RectangleXYHW.x, RectangleXYHW.y), new Vector2(RectangleXYHW.z, RectangleXYHW.w)));

            Draw.UseGradientFill = false;
            Draw.Color = Color.white;
            Draw.LineEndCaps = LineEndCap.Round;
            Draw.Line(new Vector3(-1.25f, 2.76f, 1), new Vector3(1.25f, 2.76f, 1));

            Draw.Line(new Vector3(-1.25f, 3.3f, 1), new Vector3(1.25f, 3.3f, 1));
        }

    }

    private void DrawTime(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.BlendMode = BlendType;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = Camera.main.transform.localToWorldMatrix;
            Draw.FontSize = fontSize;
            Draw.Color = fontColor;
            Draw.Text(DebugPosition, CurrentStringTime);
        }

    }

    public void StartTimer()
    {
        TimerUpdating = true;
        TimerOn = true;
        currentTime = 0;
        CurrentStringTime = GetTimeFromFloat(0);

    }

    public void StopTimer()
    {
        TimerOn = true;
        TimerUpdating = false;

    }

    string GetTimeFromFloat(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int miliseconds = Mathf.FloorToInt( (time * 100) % 100);
        return minutes.ToString("D2") + ":" + seconds.ToString("D2") + "." + miliseconds.ToString("D2");
    }

    public void UpdateTime()
    {
        currentTime += Time.deltaTime;
        CurrentStringTime = GetTimeFromFloat(currentTime);
    }
}
