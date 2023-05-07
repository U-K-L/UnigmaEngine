using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class StageSelectionScreenUI : ImmediateModeShapeDrawer
{
    public Color borderColor = new Color(213f / 255f, 255f / 255f, 244f / 255f, 1f);
    List<SelectionNodes> stagesUI;
    List<EggStages> ListOfStages;

    void Start()
    {
        ListOfStages = new List<EggStages>();
        EggStages[] stages = Resources.LoadAll<EggStages>("EggMaps/Stages");
        ListOfStages.AddRange(stages);
        stagesUI = new List<SelectionNodes>();
        DisplayStages();
    }

    void DisplayStages()
    {
        foreach (EggStages stage in ListOfStages)
        {
            GameObject obj = new GameObject(stage.stageName);
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            SelectionNodes node = obj.AddComponent<SelectionNodes>();
            node.image = stage.stageIcon;
            stagesUI.Add(node);
        }
    }

    public override void DrawShapes(Camera cam)
    {
        DrawBoarders(cam);
        UpdateStagePositions(cam);
    }

    void UpdateStagePositions(Camera cam)
    {
        for (int i = 0; i < stagesUI.Count; i++)
        {
            stagesUI[i].DrawStageSelectionNode(cam, new Vector3(-22, 1.185f,0) + new Vector3(i*8, 0,0));
        }
    }
    void DrawBoarders(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.BlendMode = ShapesBlendMode.Transparent;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;
            Draw.LocalScale = this.transform.localScale;

            //Top boarder.
            Draw.Color = new Vector4(0, 0, 0, 0.65f);
            Draw.Position += new Vector3(-32, 9, 0);
            Draw.Rectangle(new Rect(new Vector2(0, 0), new Vector2(55, 12)));


            Draw.RectangleBorder(new Rect(new Vector2(0, 0), new Vector2(55, 12)), 0.21f, 100f, Color.white);

            RepeatedDrawGradient(20, borderColor, new Vector2(55, 12), new Vector2(0, 0), -1);


            Draw.Position = new Vector3(0, 0, 0);

            //Bottom boarder.
            Draw.Color = new Vector4(0, 0, 0, 0.65f);
            Draw.Position += new Vector3(-22, 48, 0);
            Draw.Rectangle(new Rect(new Vector2(0, 0), new Vector2(55, 12)));

            Draw.RectangleBorder(new Rect(new Vector2(0, 0), new Vector2(55, 12)), 0.21f, 100f, Color.white);

            RepeatedDrawGradient(20, borderColor, new Vector2(55, 12), new Vector2(0, 0), 1);

            Draw.Position = new Vector3(0, 0, 0);
            Draw.Position += new Vector3(-16, 67.5f, 0);
            Draw.Rectangle(new Rect(new Vector2(0, 0), new Vector2(40, 4)), 100);
            Draw.RectangleBorder(new Rect(new Vector2(0, 0), new Vector2(40, 4)), 0.21f, 100f, Color.white);
        }
    }

    void RepeatedDrawGradient(int n, Color c, Vector2 size, Vector2 pos, int sign)
    {
        for (int i = 1; i < n; i++)
        {
            float thickness = 0.21f;
            Color col = new Color(c.r, c.g, c.b, c.a / (0.25f * i + 1));
            Draw.RectangleBorder(new Rect(pos.x, pos.y + sign * i * 0.19f, size.x, size.y), thickness, 100f, col);
        }
    }
}
