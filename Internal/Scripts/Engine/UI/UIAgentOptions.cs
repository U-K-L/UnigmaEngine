using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

[ExecuteAlways]
public class UIAgentOptions : ImmediateModeShapeDrawer
{
    // Start is called before the first frame update
    public float CellSize = 0.25f;
    public float expansionSpeed = 1f;
    public GameObject videoPlayer;
    private AgentPhysics agent;
    private Player_Controller_Agents controller;
    private AgentSkill[] skillList;
    public int particleNum = 20;
    public int selectedSkill = 0; //The skill that is currently selected.
    private int _selectedSkillAngle = 0; //Used to calculate angle from skill, this value can be negative.
    private float _selectedItemAngleOffset = 0.0f; //Offsets the wheel for the selected skill.
    public Vector3 videoOffset;
    private Vector3 originalVideoPosition;
    public GameObject iconPrefab;
    public Texture texture;
    public Vector4 iconScaleOffsets;
    private VideoPlayer video;
    public float scale = 1f;
    void Start()
    {
        videoPlayer = Instantiate(videoPlayer, gameObject.transform);
        originalVideoPosition = videoPlayer.transform.position;
        skillList = new AgentSkill[0];
        video = videoPlayer.GetComponentInChildren<VideoPlayer>();

    }
    // Update is called once per frame
    void Update()
    {
        skillList = agent.getCurrentSkills();
        Vector3 Ceuler = controller._cam.transform.rotation.eulerAngles;
        Vector3 Teuler = gameObject.transform.rotation.eulerAngles;
        Vector3 swizzle = new Vector3(Teuler.x, Ceuler.y, Ceuler.z);
        gameObject.transform.rotation = Quaternion.Euler(swizzle);
        gameObject.transform.position = agent.transform.position;
        //videoPlayer.transform.position = originalVideoPosition+ videoOffset;

        animateWheel();
        updateInputs();
    }

    public override void DrawShapes(Camera cam)
    {
        DrawMenuStable(cam);
    }

    void ExpandMenu()
    {
        StopAllCoroutines();
        gameObject.transform.localScale = Vector3.zero;
        StartCoroutine(menuExpansion());
    }
    void CompressMenu()
    {
        StopAllCoroutines();
        StartCoroutine(menuCompression());
    }
    IEnumerator menuExpansion()
    {
        while (Vector3.Distance(gameObject.transform.localScale, Vector3.one) > 0.01f)
        {
            gameObject.transform.localScale = Vector3.Lerp(gameObject.transform.localScale, Vector3.one, Time.deltaTime * 6f);
            yield return new WaitForSeconds(0.055f * Time.deltaTime);
        }
    }

    IEnumerator menuCompression()
    {
        while (Vector3.Distance(gameObject.transform.localScale, Vector3.zero) > 0.01f)
        {
            gameObject.transform.localScale = Vector3.Lerp(gameObject.transform.localScale, Vector3.zero, Time.deltaTime * 9.25f);
            yield return new WaitForSeconds(0.055f*Time.deltaTime);
        }
        gameObject.SetActive(false);
    }

    void DrawMenuStable(Camera cam)
    {
        DrawMenuArc(cam);
        DrawHexagons(cam);
        DrawSpinningParticles(cam);
        DrawIcons(cam);
        CreateVideo();
    }

    void DrawMenuArc(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ZTest = CompareFunction.Always; // to make sure it draws on top of everything like a HUD
            Draw.Matrix = gameObject.transform.localToWorldMatrix; // draw it in the space of crosshairTransform

            //Draw.GradientFill = GradientFill.Radial(new Vector2(0, 0), 10f, Color.black, Color.black);
            //Draw.UseGradientFill = true;
            Draw.Color = new Color(0.19f, 0.19f, 0.4f, 0.3f);
            Draw.BlendMode = ShapesBlendMode.Transparent;
            DrawRoundedArcOutlineNOCAP(new Vector2(0, 0), 0.286f, 0.05f, 0.1f, 0.0f, Mathf.PI * 2);
            Draw.BlendMode = ShapesBlendMode.Opaque;
            Draw.Color = new Color(0.19f, 0.19f, 0.25f);
            DrawRoundedArcOutlineNOCAP(new Vector2(0, 0), 0.276f, 0.175f, 0.01f, 0.0f, Mathf.PI * 2);
            Draw.DashType = DashType.Basic;
            Draw.UseDashes = true;
            DrawRoundedArcOutlineNOCAP(new Vector2(0, 0), 0.286f, 0.175f, 0.01f, 0.0f, Mathf.PI * 2);

        }
    }

    void DrawHexagons(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;
            Draw.Radius = animateHexagon();

            //Determines the angle depending on where it exists in the arc.
            //Get the rotation towards the camera.
            Vector3 Ceuler = cam.transform.rotation.eulerAngles;
            Vector3 Teuler = gameObject.transform.rotation.eulerAngles;
            Vector3 swizzle = new Vector3(Teuler.x, Ceuler.y, Ceuler.z); //The x axis does not change.

            //Take the forward axis of the rotated hexagon towards the camera.
            Quaternion origRot = Draw.Rotation;
            //Draw.Rotation = Quaternion.AngleAxis(90, -gameObject.transform.forward) * Quaternion.Euler(swizzle);
            Color col = new Color(0.12f, 0.12f, 0.19f);
            Color colBright = new Color(0.32f, 0.32f, 0.39f);
            Draw.UseGradientFill = true;
            float expand = 2.9f * scale;

            for (int i = 0; i < skillList.Length; i++)
            {
                //determine the color:
                if (i == (skillList.Length-selectedSkill) % skillList.Length)
                {
                    Draw.GradientFill = GradientFill.Radial(new Vector2(0, 0), 0.165f, col, Color.white);
                }
                else
                    Draw.GradientFill = GradientFill.Radial(new Vector2(0, 0), 0.145f, col, colBright);

                //Rotating hexagon.
                float angle = ((i+_selectedItemAngleOffset) * ((Mathf.PI * 2) / (skillList.Length)) ) % 360;
                Vector3 center = gameObject.transform.position;
                //Vector3 rotPos = (center + (expand * Mathf.Sin(Time.time + angle) * gameObject.transform.right) + (expand * Mathf.Cos(Time.time + angle) * gameObject.transform.up));
                Vector3 rotPos = (center + (expand * Mathf.Sin(angle) * gameObject.transform.right) + (expand * Mathf.Cos(angle) * gameObject.transform.up));
                Draw.Position = rotPos;
                Draw.Rotation = origRot;
                Draw.Rotation = Quaternion.AngleAxis(57.2958f * Mathf.Atan2(rotPos.y - center.y, rotPos.x - center.x), -gameObject.transform.forward) * Quaternion.Euler(swizzle);
                Draw.RegularPolygon();
                
                //Draw.RegularPolygonBorder();
            }
            /*
            //The left
            Draw.Position = new Vector3(centerPos.x - 0.2f, centerPos.y - 0.135f, centerPos.z);
            Draw.RegularPolygon();
            */
        }
    }

    void DrawIcons(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.Position = Vector3.zero;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;

            for (int i = 0; i < skillList.Length; i++)
            {
                //Expands them to the desired vector radius.
                float angle = ((i + _selectedItemAngleOffset) * ((Mathf.PI * 2) / (skillList.Length))) % 360;
                Vector3 center = gameObject.transform.position + new Vector3(iconScaleOffsets.z, iconScaleOffsets.w, 0.0f);
                Vector3 rotPos = (center + (iconScaleOffsets.x * Mathf.Sin(angle) * gameObject.transform.right) + (iconScaleOffsets.y * Mathf.Cos(angle) * gameObject.transform.up));
                Draw.Position = rotPos;
                Draw.Texture(skillList[i].texture, new Rect(new Vector2(-0.045f, -0.045f), new Vector2(0.09f, 0.09f)));
            }
        }
    }

    void DrawSpinningParticles(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;
            Draw.Radius = 0.1f*animateHexagon();
            Vector3 Ceuler = cam.transform.rotation.eulerAngles;
            Vector3 Teuler = gameObject.transform.rotation.eulerAngles;
            Vector3 swizzle = new Vector3(Teuler.x, Ceuler.y, Ceuler.z);
            Quaternion origRot = Draw.Rotation;
            Color col = new Color(0.12f, 0.12f, 0.19f);
            Color colBright = new Color(0.32f, 0.32f, 0.39f);
            Draw.UseGradientFill = true;
            Draw.DashType = DashType.Basic;
            Draw.Thickness = 0.004f;
            float expand = 4f* scale;
            for (int i = 0; i < particleNum; i++)
            {
                float t = i / (float)particleNum;
                float t2 = (i+1) / (float)particleNum;
                Color color = Color.HSVToRGB(t, 1, 1);
                Color color2 = Color.HSVToRGB(t2, 1, 1);
                Draw.GradientFill = GradientFill.Radial(new Vector2(0, 0), 0.5f, color, color);
                float angle = (i) * ((Mathf.PI * 2) / (particleNum));
                angle += Mathf.Cos(angle * (0.2f * Mathf.Abs(Mathf.Sin(Time.time))) + Time.time * Mathf.Sin(Time.time) *0.05f*Time.deltaTime);


                Vector3 center = gameObject.transform.position;
                Vector3 rotPos = (center + (expand * Mathf.Sin(Time.time + angle) * gameObject.transform.right) + (expand * Mathf.Cos(Time.time + angle) * gameObject.transform.up));
                Draw.Position = rotPos;
                Draw.Rotation = origRot;
                Draw.Rotation = Quaternion.AngleAxis(57.2958f * Mathf.Atan2(rotPos.y - center.y, rotPos.x - center.x), -gameObject.transform.forward) * Quaternion.Euler(swizzle);
                
                Draw.Ring();
            }
        }
    }

    public float animateHexagon()
    {
        float expanding = CellSize + Mathf.Abs( (CellSize*0.125f) * Mathf.Sin(Time.time * expansionSpeed));
        return expanding;
    }

    public void setAgent(AgentPhysics ag)
    {
        agent = ag;

    }

    public AgentPhysics getAgent()
    {
        return agent;
    }

    public void setController(Player_Controller_Agents pa)
    {
        controller = pa;
    }

    public Player_Controller_Agents getController()
    {
        return controller;
    }

    public static void DrawRoundedArcOutlineNOCAP(Vector2 origin, float radius, float thickness, float outlineThickness, float angStart, float angEnd)
    {
        // inner / outer
        float innerRadius = radius - thickness / 2;
        float outerRadius = radius + thickness / 2;
        const float aaMargin = 0.01f;
        Draw.Arc(origin, innerRadius, outlineThickness, angStart - aaMargin, angEnd + aaMargin);
        Draw.Arc(origin, outerRadius, outlineThickness, angStart - aaMargin, angEnd + aaMargin);

    }
    public static void DrawRoundedArcOutline(Vector2 origin, float radius, float thickness, float outlineThickness, float angStart, float angEnd)
    {
        // inner / outer
        float innerRadius = radius - thickness / 2;
        float outerRadius = radius + thickness / 2;
        const float aaMargin = 0.01f;
        Draw.Arc(origin, innerRadius, outlineThickness, angStart - aaMargin, angEnd + aaMargin);
        Draw.Arc(origin, outerRadius, outlineThickness, angStart - aaMargin, angEnd + aaMargin);

        // rounded caps
        Vector2 originBottom = origin + ShapesMath.AngToDir(angStart) * radius;
        Vector2 originTop = origin + ShapesMath.AngToDir(angEnd) * radius;
        Draw.Arc(originBottom, thickness / 2, outlineThickness, angStart, angStart - ShapesMath.TAU / 2);
        Draw.Arc(originTop, thickness / 2, outlineThickness, angEnd, angEnd + ShapesMath.TAU / 2);
    }



    public void activateMenu()
    {
        ExpandMenu();
    }

    public void deactivateMenu()
    {
        CompressMenu();
    }

    void animateWheel()
    {
        _selectedItemAngleOffset = Mathf.Lerp(_selectedItemAngleOffset, (float)_selectedSkillAngle, Time.deltaTime*2.5f);
    }

    void updateInputs()
    {
        if (Input.GetButtonDown("HorizontalButton"))
        {
            float hori = Input.GetAxis("Horizontal");
            if (hori > 0.0f)
                _selectedSkillAngle += 1;
            else if (hori < 0.0f)
                _selectedSkillAngle -= 1;
            
        }

        Debug.Log(skillList);
        selectedSkill = mod(_selectedSkillAngle, skillList.Length);

        if (Input.GetButtonDown("Accept"))
        {
            skillList[selectedSkill].execute(controller, agent);
            deactivateMenu();
        }
    }

    void CreateVideo()
    {
        if(selectedSkill < skillList.Length)
            video.clip = skillList[selectedSkill].video;
    }

    int mod(int x, int m)
    {
        return (x % m + m) % m;
    }
}
