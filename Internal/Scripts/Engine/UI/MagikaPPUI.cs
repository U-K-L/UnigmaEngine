using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;
using System;

public class MagikaPPUI : ImmediateModeShapeDrawer
{
    public int syntaxTreeCursor = 0;
    public Texture CursorImage;
    public Texture RenderText;

    public Vector4 RenderViewXYWH;
    public GradientFill RenderColor;

    public Vector4 RenderRectViewXYWH;

    public Vector4 RenderRectBorderViewXYWH;
    public Color BorderColor;

    public Vector4 RenderTextureMasterOffsets;
    
    public Vector2 nodeRectSize = new Vector2(1f, 1f);
    public Vector2 nodeRectOffset = new Vector2(0, 0);
    private float animatingRings = 0.0f;
    public PlayerCursor Cursor;
    private Vector3 _mousePos;
    public float touchRadius = 0.1f;
    private Vector2 _cursor2DPos;
    public float dragSpeed = 0.1f;
    public float scrollSpeed = 0.1f;
    public Vector2 minMaxZoom;
    public Vector2 plusIconPosition;
    public GameObject HUD;
    public GameObject SubHUD;
    private float _initialOrthoSize = 1.57f;
    private Vector3 initialScale;
    public Vector2 OrthoSizeOffsets;
    public GradientFill colorGradientSubMenu;

    public Vector4 subMenuPosition = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
    public Vector4 colRowSpacing = new Vector4(8, 3, 0.5f, 0.5f);
    public Vector4 offsetWidthHeight = new Vector4(0, 0, 0.35f, 0.35f);
    public Color cellsColor = Vector4.one;

    public Color cellBorderColor = Vector4.one;
    public Vector2 cellBorderRoundness = Vector2.one;

    public Vector4 cellNodesOffsetSpacing = new Vector4(0, 0, 0, 0);

    public CompareFunction NodesCompareFunction = CompareFunction.LessEqual;
    public CompareFunction NodesStencilCompareFunction;

    public StencilOp NodesStencilOp = StencilOp.Keep;
    public StencilOp CellsStencilOp = StencilOp.Keep;

    public enum StateMachinePPUI {main, subMenu, dragging}
    public static StateMachinePPUI stateMachine = StateMachinePPUI.main;

    
    public byte StencilTestRef = 1;
    public byte StencilTestMask = 1;

    private MagikaPP_SyntaxNode[] commands;

    //Debug Node.
    public Seek_UI_MagikaPP debugNode;
    void Start()
    {
        SavingCode();
        debugNode = new Seek_UI_MagikaPP();
        int cellCount = (int)Mathf.Floor(colRowSpacing.x);
        int rowCount = (int)Mathf.Floor(colRowSpacing.y);
        commands = new MagikaPP_SyntaxNode[cellCount * rowCount];
        for(int i = 0; i < commands.Length; i++)
        {
            if(i%2 == 0)
                commands[i] = new Seek_UI_MagikaPP();
            else
                commands[i] = new Start_UIMagika();
        }
        stateMachine = StateMachinePPUI.main;
        initialScale = HUD.transform.localScale;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            SavingCode();
        }

    }

    public override void DrawShapes(Camera cam)
    {
        _mousePos = Input.mousePosition;
        _cursor2DPos = nodeRectOffset + new Vector2(Cursor.transform.position.x, Cursor.transform.position.y);
        HUD.transform.localScale = initialScale * (Camera.main.orthographicSize / _initialOrthoSize);
        SubHUD.transform.localScale = initialScale * (Camera.main.orthographicSize / _initialOrthoSize);

        if (stateMachine == StateMachinePPUI.subMenu)
            FindNodeInMenu();
        else        
            FindNodeTouched(cam);
        
        DrawLayout(cam);
        DrawNodes(cam);
        //debugNode.DrawShape(true, cam, animatingRings, this.transform);
        DrawStateDependentMenus(cam);
        DrawTextureImage(cam);
        DrawRenderTextureImage(cam);
        UpdateControllerInput();
        animatingRings += 0.001f % 2000000f;

    }

    void DrawStateDependentMenus(Camera cam)
    {
        if (stateMachine == StateMachinePPUI.main)
        {
            
        }
        else if (stateMachine == StateMachinePPUI.subMenu)
        {
            Vector3 origPos = SubHUD.transform.localPosition;
            Vector3 newPos = new Vector3(origPos.x, 0, origPos.z);
            SubHUD.transform.localPosition = Vector3.Lerp(SubHUD.transform.localPosition, newPos, Time.deltaTime * 7);
            DrawSubMenu(cam);
        }

        if (stateMachine != StateMachinePPUI.subMenu)
        {
            Vector3 origPos = SubHUD.transform.localPosition;
            Vector3 newPos = new Vector3(origPos.x, -3.5f, origPos.z);
            SubHUD.transform.localPosition = Vector3.Lerp(SubHUD.transform.localPosition, newPos, Time.deltaTime * 5);
            DrawSubMenu(cam);
        }
    }

    void DrawLayout(Camera cam)
    {

    }

    void DrawNodes(Camera cam)
    {
        foreach (MagikaPP_SyntaxNode node in MagikaPP_Lexer._syntaxRoots)
        {
            node.DrawConnections(cam, this.transform, node.GetConnections()[0]);
        }
        foreach (MagikaPP_SyntaxNode node in MagikaPP_Lexer._syntaxRoots)
        {
            node.DrawShape(true, cam, animatingRings, this.transform);
            node.UpdateNode();
        }
    }

    void DrawTextureImage(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.Position = Cursor.transform.position;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = gameObject.transform.localToWorldMatrix;
            Draw.Texture(CursorImage, new Rect(_cursor2DPos, nodeRectSize));
        }
    }

    void DrawRenderTextureImage(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.Position = Cursor.transform.position;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = HUD.transform.localToWorldMatrix;
            Vector4 mas = RenderTextureMasterOffsets;


            Draw.UseGradientFill = true;
            Draw.GradientFill = RenderColor;
            Draw.Color = cellBorderColor;


            Draw.StencilRefID = 1;
            Draw.StencilOpPass = CellsStencilOp;
            Draw.StencilWriteMask = 1;
            Draw.StencilReadMask = 1;


            Draw.Rectangle(new Rect(RenderRectViewXYWH.x + mas.x, RenderRectViewXYWH.y +mas.y, RenderRectViewXYWH.z*mas.z, RenderRectViewXYWH.w*mas.w));
            Draw.UseGradientFill = false;

            Draw.Color = Color.white;

            Draw.StencilRefID = 1;
            Draw.StencilOpPass = CellsStencilOp;
            Draw.StencilWriteMask = 1;
            Draw.StencilReadMask = 1;

            Draw.ZTest = NodesCompareFunction;
            Draw.StencilComp = NodesStencilCompareFunction;
            Draw.StencilRefID = 1;
            Draw.StencilReadMask = 1;


            Draw.Texture(RenderText, new Rect(RenderViewXYWH.x+mas.x, RenderViewXYWH.y+mas.y, RenderViewXYWH.z*mas.z, RenderViewXYWH.w*mas.w));

            Draw.ResetAllDrawStates();
            Draw.Position = Cursor.transform.position;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = HUD.transform.localToWorldMatrix;

            Draw.RectangleBorder(Vector3.zero, new Rect(RenderRectBorderViewXYWH.x + mas.x, RenderRectBorderViewXYWH.y + mas.y,
RenderRectBorderViewXYWH.z * mas.z, RenderRectBorderViewXYWH.w * mas.w), 0.05f, 0.05f, BorderColor);
        }
    }

    void FindNodeTouched(Camera cam)
    {
        bool isAnyNodeTouched = false;
        foreach (MagikaPP_SyntaxNode node in MagikaPP_Lexer._syntaxRoots)
        {
            if (node.IsSelected())
            {
                if (Input.GetMouseButton(0))
                {
                    isAnyNodeTouched = true;
                    DragAndDrop(cam, node);
                    break;
                }
                else
                {
                    node.SetSelected(false);
                }
            }
        }
        
        if (!isAnyNodeTouched)
        {
            foreach (MagikaPP_SyntaxNode node in MagikaPP_Lexer._syntaxRoots)
            {

                Vector2 center = node.GetPosition() - Draw.Position2D;
                center = new Vector2(node.GetPosition().x - nodeRectSize.x / 2, node.GetPosition().y - nodeRectSize.y / 2);

                if (Vector2.Distance(center, _cursor2DPos) < touchRadius)
                {
                    node.SetSelected(true);
                    break;
                }
            }
        }


    }

    private void FindNodeInMenu()
    {
        if (Input.GetMouseButton(0))
        {
            for (int i = 0; i < commands.Length; i++)
            {
                MagikaPP_SyntaxNode node = commands[i];
                Vector2 center = Camera.main.WorldToViewportPoint(node.Get3DPosition());
                Vector2 subMenuCursor = Camera.main.ScreenToViewportPoint(Input.mousePosition);
                if (Vector2.Distance(center, subMenuCursor) < 0.05f)
                {
                    node.SetInstantiation(true);
                    MagikaPP_Lexer._syntaxRoots.Add(node);
                }
            }
        }
    }

    void DragAndDrop(Camera cam, MagikaPP_SyntaxNode node)
    {
        
        float hori = Input.GetAxis("Mouse X");
        float vert = Input.GetAxis("Mouse Y");
        Vector2 center = node.GetPosition() - Draw.Position2D;
        center = new Vector2(node.GetPosition().x - nodeRectSize.x / 2, node.GetPosition().y - nodeRectSize.y / 2);        
        Vector2 axis = new Vector2(hori, vert);
        Vector2 newNodePos = node.GetPosition() + _cursor2DPos - center;
        Vector2 lerpedResult = Vector2.Lerp(node.GetPosition(), newNodePos, Time.deltaTime * dragSpeed);

        MagikaPP_Edge connection = node.GetConnection();
        if (Input.GetButton("Drag_Desktop"))
        {
            stateMachine = StateMachinePPUI.dragging;
            connection.SetPosition(_cursor2DPos - node.GetPosition());

            foreach (MagikaPP_SyntaxNode endnode in MagikaPP_Lexer._syntaxRoots)
            {

                if (node.GetPosition() == endnode.GetPosition())
                {
                    continue;
                }
                else
                {

                    Vector2 centerEnd = endnode.GetPosition() - Draw.Position2D;
                    centerEnd = new Vector2(endnode.GetPosition().x - nodeRectSize.x / 2, endnode.GetPosition().y - nodeRectSize.y / 2);

                    if (Vector2.Distance(centerEnd, _cursor2DPos) < touchRadius)
                    {
                        connection.edge.source = node;
                        connection.edge.target = endnode;
                        connection.targetConnected = true;
                        node.SetSelected(false);
                        node.currentConnection = 1;                    

                    }
                }
            }            

        }
        else
        {
            stateMachine = StateMachinePPUI.main;
            node.SetPosition(lerpedResult);
        }
    }

    void UpdateControllerInput()
    {
        UpdateStateControls();
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Camera.main.orthographicSize = Camera.main.orthographicSize+ scroll * scrollSpeed;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minMaxZoom.x, minMaxZoom.y);
    }

    void UpdateStateControls()
    {
        if (Input.GetMouseButton(0))
        {
            SwitchStateToMainMenu();
        }
    }

    void DrawSubMenu(Camera cam)
    {
        
        using (Draw.Command(cam))
        {
            //Set the settings for this node.
            Draw.ResetAllDrawStates();
            Draw.Position = Vector3.zero;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = SubHUD.transform.localToWorldMatrix;
            //Draw.Position2D += node.GetPosition();
            Draw.UseGradientFill = true;
            Draw.GradientFill = colorGradientSubMenu;
            Draw.Rectangle(new Rect(subMenuPosition.x, subMenuPosition.y, subMenuPosition.z, subMenuPosition.w));

        }

        DrawCells(cam);
    }

    void DrawCells(Camera cam)
    {
        
        int cellCount = (int)Mathf.Floor(colRowSpacing.x);
        int rowCount = (int)Mathf.Floor(colRowSpacing.y);
        int index = 0;
        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < cellCount-i; j++)
            {
                MagikaPP_SyntaxNode node = commands[index];
                Vector2 CellPos = new Vector2(j* colRowSpacing.z + (i*0.5f* colRowSpacing.z), -i* colRowSpacing.w);
                DrawACell(cam, CellPos, new Vector2(j,i), node);

                index += 1;
            }
        }
        
    }

    void DrawACell(Camera cam, Vector2 pos, Vector2 index, MagikaPP_SyntaxNode node)
    {
        float animatedHeight = 0.01f * Mathf.Sin(Time.time*6f) + offsetWidthHeight.w;
        Vector2 cellPos = new Vector2(pos.x + offsetWidthHeight.x, pos.y + offsetWidthHeight.y);
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            
            Draw.StencilRefID = 1;
            Draw.StencilOpPass = CellsStencilOp;
            Draw.StencilWriteMask = 1;
            Draw.StencilReadMask = 1;
            
            Draw.Position = Vector3.zero;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = SubHUD.transform.localToWorldMatrix;
            Draw.Color = cellsColor;
            Draw.Rectangle(Vector3.zero, new Rect(cellPos.x, cellPos.y, offsetWidthHeight.z, animatedHeight), 0.095f);

            float scale = 0.35f;
            index.x = cellNodesOffsetSpacing.w * index.x;
            index.y = cellNodesOffsetSpacing.z * index.y;
            cellPos.x += index.x;
            cellPos.y += index.y;
            
            Draw.StencilRefID = 1;
            Draw.StencilOpPass = CellsStencilOp;
            Draw.StencilWriteMask = 1;
            Draw.StencilReadMask = 1;

            Draw.ZTest = NodesCompareFunction;
            Draw.StencilComp = NodesStencilCompareFunction;
            Draw.StencilRefID = 1;
            Draw.StencilReadMask = 1;

            node.DrawCell(false, cam, animatingRings, cellPos, cellNodesOffsetSpacing, scale);

            Draw.Color = cellBorderColor;
            Draw.RectangleBorder(Vector3.zero, new Rect(cellPos.x, cellPos.y, offsetWidthHeight.z, animatedHeight), cellBorderRoundness.x, cellBorderRoundness.y);
        }
    }

    public static void SwitchStateToSubMenu()
    {
        stateMachine = StateMachinePPUI.subMenu;
    }

    public static void SwitchStateToMainMenu()
    {
        stateMachine = StateMachinePPUI.main;
    }

    //Saves the code into a text file.
    public void SavingCode()
    {
        Debug.Log("Begin saving");
        MagikaPP_Lexer.SaveCode(MagikaPP_Lexer._syntaxRoots);
    }
}
