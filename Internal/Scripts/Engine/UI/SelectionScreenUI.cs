using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Shapes;

public class SelectionScreenUI : ImmediateModeShapeDrawer
{
    // Start is called before the first frame update
    List<EggStages> ListOfStages;
    List<SelectionNodes> stagesUI;
    bool glowOff = false;
    public float timeIncrements;
    public Camera gui_cam;

    public float R1 = -1.11f;
    public float a1 = 0.11f;
    public float b1 = -1f;
    public float w1 = 1.84f;
    public float z1 = 3.5f;
    public float p1 = -2.97f;
    public float n1 = 2.73f;
    public float d1 = 10f;

    public Color borderColor = new Color(213f / 255f, 255f / 255f, 244f / 255f, 1f);
    public List<float> Times = new List<float>
    {
        21.6f,
        16.6f,
        10.9f,
        4f,
        0.5f,
        -2.8f,
    };

    void Start()
    {
        ListOfStages = new List<EggStages>();
        EggStages[] stages = Resources.LoadAll<EggStages>("EggMaps/Stages");
        ListOfStages.AddRange(stages);
        stagesUI = new List<SelectionNodes>();
        DisplayStages();


        //DebugStageList();
    }

    // Update is called once per frame
    void Update()
    {
        if (gui_cam == null)
        {
            GameObject obj = GameObject.Find("GUI_Camera");
            if (obj)
                gui_cam = obj.GetComponent<Camera>();
        }
        UpdateInputs();
        if (!glowOff && Camera.main)
        {
            GlowComposite glow = Camera.main.transform.GetComponent<GlowComposite>();
            if (glow)
            {
                glow.enabled = false;
                glowOff = true;
            }
        }
    }

    public override void DrawShapes(Camera cam)
    {
        DrawBoarders(cam);
        UpdatePositions(cam);

    }

    void UpdateInputs()
    {
        if (Input.GetButtonDown("L"))
        {
            ShiftList(stagesUI);
        }

        if (Input.GetButtonDown("R"))
        {
            PickCurrentSelection();
        }
    }

    public void PickCurrentSelection()
    {

        if (gui_cam)
        {
            gui_cam.transform.parent.gameObject.SetActive(false);
            gui_cam.enabled = false;
        }
        EggGameMaster.Instance.SetCurrentStage();
        this.gameObject.SetActive(false);
    }

    void ShiftList(List<SelectionNodes> selectionNodes)
    {
        Debug.Log("shifted");
        SelectionNodes temp = selectionNodes[0];
        //remove last node
        selectionNodes.RemoveAt(0);
        selectionNodes.Add(temp);
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
            Draw.Color = new Vector4(0,0,0,0.65f);
            Draw.Position += new Vector3(-32, 9, 0);
            Draw.Rectangle(new Rect(new Vector2(0, 0), new Vector2(55, 12)));

            
            Draw.RectangleBorder(new Rect(new Vector2(0, 0), new Vector2(55,12)), 0.21f, 100f, Color.white);

            RepeatedDrawGradient(20, borderColor, new Vector2(55, 12), new Vector2(0, 0), -1);


            Draw.Position = new Vector3(0, 0, 0);
            
            //Bottom boarder.
            Draw.Color = new Vector4(0, 0, 0, 0.65f);
            Draw.Position += new Vector3(-22, 48, 0);
            Draw.Rectangle(new Rect(new Vector2(0, 0), new Vector2(55, 12)));

            Draw.RectangleBorder(new Rect(new Vector2(0, 0), new Vector2(55, 12)), 0.21f, 100f, Color.white);

            RepeatedDrawGradient(20, borderColor, new Vector2(55, 12), new Vector2(0, 0), 1);
        }
    }

    void RepeatedDrawGradient(int n, Color c, Vector2 size, Vector2 pos, int sign)
    {
        for (int i = 1; i < n; i++)
        {
            float thickness = 0.21f;
            Color col = new Color(c.r, c.g, c.b, c.a/(0.25f*i+1));
            Draw.RectangleBorder(new Rect(pos.x, pos.y + sign*i*0.19f, size.x, size.y), thickness, 100f, col); 
        }
    }
    
    void UpdatePositions(Camera cam)
    {
        float time = 0f;
        int index = stagesUI.Count - 1;
        int timesIndex = 0;
        for (int i = stagesUI.Count -1; i >= 0; i--)
        {
            Vector3 VVF = nodePath(Times[i]);
            //node.transform.localPosition = VVF;
            //Debug.Log(Camera.main.name);
            stagesUI[i].DrawCircleBackground(cam, VVF);
            time += timeIncrements;
            timesIndex++;
        }
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
    
    public Vector3 nodePath(float t)
    {
        float term1 = Mathf.Exp(t * a1) * b1 * Mathf.Sin(w1 * t + z1);
        float term2 = p1 - n1 * Mathf.Sin(d1 * (w1 * t + Mathf.PI / 2 + z1));

        float finalPos = R1 * (term1 + term2);

        float x = -t;
        float y = finalPos;
        float z = 0.0f;
        return new Vector3(x, y, z);
    }

    public void DebugStageList()
    {
        Debug.Log("Stages count: " + ListOfStages.Count);
        foreach (EggStages stage in ListOfStages)
        {
            Debug.Log(stage.stageName);
        }
    }
}
